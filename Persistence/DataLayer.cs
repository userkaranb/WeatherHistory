using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;


namespace Jubilado.Persistence;

public interface IDataLayer
{
    public City GetCity(City city);
    public Task<List<CityWeatherScore>> GetAllCityWeatherScoreCombos();
    public Task<List<CityIdealSunDays>> GetAllIdealSunDaysCombos();
    public List<WeatherHistory> GetWeatherHistoryForCity(string cityName);
    void CreateCity(City city);
    void DeleteCity(City city);
    void CreateWeatherScoreInfo(CityStatWrapper cityStat);
    void CreateWeatherHistoryItem(WeatherHistory historyItem);
    void CreateWeatherHistoryItems(List<WeatherHistory> historyItems);
    void CreateWeatherScoreKey(List<CityWeatherScore> cityScores);
    void CreateIdealSunDaysScoreKey(List<CityIdealSunDays> cityScores);
    void DeleteWeatherSortKey(string cityName);

}

public class DataLayer : IDataLayer
{
    private Table _table;
    private AmazonDynamoDBClient _client;
    const String TableName = "Jubilado";
    const String PK = "PK";
    const String SK = "SK";
    const String GSI1PK = "GSI1PK";
    const String GSISK = "GSI1SK";
    public DataLayer()
    {
        // this is bad
        var awsCredentials = new BasicAWSCredentials("AKIAVRUVWNX44E25QLC7", "2FA72iPuUXiFaFLMyqIAGMLL13MNdDFfVpVqL+wk");
        var dynamoDBConfig = new AmazonDynamoDBConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast2 // Replace YOUR_REGION with your AWS region
        };

        _client = new AmazonDynamoDBClient(awsCredentials, dynamoDBConfig);
        _table = Table.LoadTable(_client, TableName);

    }

    public async Task<List<CityWeatherScore>> GetAllCityWeatherScoreCombos()
    {
        var useNewCode = true;
        if(!useNewCode)
        {
            var scannedResponse = await GetAllCityWeatherScoreCombosUsingScan("CityName, WeatherScore");
            return scannedResponse.OrderBy(x => x.WeatherScore).ToList();
        }

        return await GetAllCityWeatherScoreCombosUsingNewKey();
    }

    public async Task<List<CityIdealSunDays>> GetAllIdealSunDaysCombos()
    {
        var useNewCode = true;
        if(!useNewCode)
        {
            var scannedResponse = await GetAllCityIdealSunDaysCombosUsingScan("CityName, IdealSunDays");
            return scannedResponse.OrderBy(x => x.IdealSunDays).ToList();
        }

        return await GetAllIdealSunScoresUsingNewKey();
    }

    public async void CreateWeatherScoreKey(List<CityWeatherScore> cityWeatherScores)
    {
        var weatherScores = cityWeatherScores.Select(x => FormatCityWeatherScore(x)).ToList();
        await BatchWriteItem(weatherScores);
    }

    public async void CreateIdealSunDaysScoreKey(List<CityIdealSunDays> citySunScores)
    {
        var sunScores = citySunScores.Select(x => FormatCityIdealSunDays(x)).ToList();
        await BatchWriteItem(sunScores);

    }

    public async void DeleteCity(City city)
    {
        await DeletePartitionAndItemAsync(city.CityName);
    }

    public async void DeleteWeatherSortKey(string cityName)
    {
        var rowsToDelete = await GetWeatherScoreKeysToDelete(cityName);
        var deleteItemRequests = rowsToDelete.Select(del => new DeleteItemRequest {TableName = TableName, Key = del.Key});
        foreach (var request in deleteItemRequests)
        {
            await _client.DeleteItemAsync(request);
        }
    }

    public async Task DeletePartitionAndItemAsync(string cityName)
    {
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = GetCityPK(cityName) } }
            }
        };
        var extraItemsToDelete = await GetWeatherScoreKeysToDelete(cityName);
        var queryResponse = await _client.QueryAsync(queryRequest);
        var transactItems  = extraItemsToDelete.Select(x => new Amazon.DynamoDBv2.Model.TransactWriteItem { Delete = x}).ToList();

        foreach (var item in queryResponse.Items)
        {
            var deleteRequest = new Delete
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", item["PK"] },
                    { "SK", item["SK"] }
                }
            };
            transactItems.Add(new TransactWriteItem { Delete = deleteRequest });
        }

        foreach (var chunk in transactItems.Chunk(25))
        {
            var transactWriteItemsRequest = new TransactWriteItemsRequest
            {
                TransactItems = chunk.ToList()
            };

            try
            {
                await _client.TransactWriteItemsAsync(transactWriteItemsRequest);
                Console.WriteLine("Transaction succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transaction failed: {ex.Message}");
            }
        }

    }

    private async Task<List<Delete>> GetWeatherScoreKeysToDelete(string cityName)
    {
        var extraQueryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            FilterExpression = "contains(CityName, :str)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = GetCityWeatherScorePK() } },
                { ":str", new AttributeValue { S = $"{cityName}" } }
            }
        };

        var extraQueryResponse = await _client.QueryAsync(extraQueryRequest);
        return extraQueryResponse.Items.Select(item => new Delete
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", item["PK"] },
                    { "SK", item["SK"] }
                }
            }).ToList();
    }

    private async Task<List<CityWeatherScore>> GetAllCityWeatherScoreCombosUsingScan(string projectionExpressionFields)
    {
        var stopwatch = new Stopwatch();
        List<CityWeatherScore> cityScores = new List<CityWeatherScore>();
        stopwatch.Start();
        ScanResponse scanResponse = await PerformScan(projectionExpressionFields);
        Console.WriteLine(scanResponse.Count);
        foreach (var item in scanResponse.Items)
        {
            cityScores.Add(ItemFactory.ToCityWeatherScore(item));
        }
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time scanning: {stopwatch.ElapsedMilliseconds} ms");
        return cityScores;
    }

    private async Task<List<CityIdealSunDays>> GetAllCityIdealSunDaysCombosUsingScan(string projectionExpressionFields)
    {
        var stopwatch = new Stopwatch();
        List<CityIdealSunDays> cityScores = new List<CityIdealSunDays>();
        stopwatch.Start();
        ScanResponse scanResponse = await PerformScan(projectionExpressionFields);
        Console.WriteLine(scanResponse.Count);
        foreach (var item in scanResponse.Items)
        {
            cityScores.Add(ItemFactory.ToCityIdealSunDays(item));
        }
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time scanning: {stopwatch.ElapsedMilliseconds} ms");
        return cityScores;
    }

    private async Task<ScanResponse> PerformScan(string projectionExpressionFields)
    {
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":skPrefix", new AttributeValue { S = "CITY#" } }
            },
            ProjectionExpression = projectionExpressionFields
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        return scanResponse;
    }

    private async Task<List<CityWeatherScore>> GetAllCityWeatherScoreCombosUsingNewKey()
    {
        var stopwatch = new Stopwatch();
        List<CityWeatherScore> cityScores = new List<CityWeatherScore>();
        stopwatch.Start();
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY-WEATHER-SCORE" } },
                { ":sk", new AttributeValue { S = $"WEATHERSCORE" } },
            }
        };
        var queryResponse = _client.QueryAsync(queryRequest).GetAwaiter().GetResult();
        var toDict = queryResponse.Items;
        return ItemFactory.ToCityWeatherScores(toDict);
    }

     private async Task<List<CityIdealSunDays>> GetAllIdealSunScoresUsingNewKey()
    {
        var stopwatch = new Stopwatch();
        List<CityIdealSunDays> cityScores = new List<CityIdealSunDays>();
        stopwatch.Start();
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ScanIndexForward = false,
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY-WEATHER-SCORE" } },
                { ":sk", new AttributeValue { S = $"IDEALSUNDAYS" } },
            }
        };
        var queryResponse = _client.QueryAsync(queryRequest).GetAwaiter().GetResult();
        var toDict = queryResponse.Items;
        return ItemFactory.ToCityIdealSunDaysBulk(toDict);
    }

    public City GetCity(City city)
    {
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND SK = :sk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY#{city.CityName}" } },
                { ":sk", new AttributeValue { S = $"CITY#{city.CityName}" } }
            }
        };
        var queryResponse = _client.QueryAsync(queryRequest).GetAwaiter().GetResult();
        Console.WriteLine(city.CityName);
        var toDict = queryResponse.Items.FirstOrDefault(new Dictionary<string, AttributeValue>()).ToDictionary<string, AttributeValue>();
        return ItemFactory.ToCity(toDict);
    }

    public List<WeatherHistory> GetWeatherHistoryForCity(string cityName)
    {
        var cityNameUpper = cityName.ToUpper();
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY#{cityNameUpper}" } },
                { ":sk", new AttributeValue { S = $"WEATHERHISTORY#" } }
            }
        };
        var queryResponse = _client.QueryAsync(queryRequest).GetAwaiter().GetResult();
        return queryResponse.Items.Select(x => ItemFactory.ToWeatherHistory(x)).ToList();
    }


    public async void CreateCity(City city)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { "SK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { "CityName", new AttributeValue { S = $"{city.CityName}" } },
            { "TempScore", new AttributeValue { S = city.CityStats?.TemperatureScore.ToString().Trim() } },
            { "HumScore", new AttributeValue { S = city.CityStats?.HumidityScore.ToString().Trim() } },
            { "IdealSunDays", new AttributeValue { S = city.CityStats?.IdealSunshineDays.ToString().Trim() } },
            { "IdealTempDays", new AttributeValue { S = city.CityStats?.IdealTempRangeDays.ToString().Trim() } },
            { "SunScore", new AttributeValue { S = city.CityStats?.SunshineScore.ToString().Trim() } },
            { "WeatherScore", new AttributeValue { S = city.CityStats?.WeatherScore.ToString().Trim() } },
        };

       var weatherScoreItem = FormatCityWeatherScore(new CityWeatherScore(city.CityName, city.CityStats.WeatherScore));
       var sunDaysItem = FormatCityIdealSunDays(new CityIdealSunDays(city.CityName, city.CityStats.IdealSunshineDays));
       await TransactWriteItem(new List<Dictionary<string, AttributeValue>>{item, weatherScoreItem, sunDaysItem});
    }

    public async void CreateWeatherScoreInfo(CityStatWrapper cityStat)
    {
        var request = new UpdateItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> {
                { "PK", new AttributeValue { S = $"CITY#{cityStat.CityName}" } },
                { "SK", new AttributeValue { S = $"CITY#{cityStat.CityName}" } },
            },
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                {"#TS", "TempScore"},
                {"#SS", "SunScore"},
                {"#HS", "HumScore"},
                {"#WS", "WeatherScore"},
                {"#ITD", "IdealTempDays"},
                {"#ISD", "IdealSunDays"}
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":tempScore", new AttributeValue { N = cityStat.TemperatureScore.ToString() }},
                {":sunScore", new AttributeValue { N = cityStat.SunshineScore.ToString() }},
                {":humScore", new AttributeValue { N = cityStat.HumidityScore.ToString() }},
                {":weatherScore", new AttributeValue { N = cityStat.WeatherScore.ToString() }},
                {":idealTempDays", new AttributeValue { N = cityStat.IdealTempRangeDays.ToString() }},
                {":idealSunDays", new AttributeValue { N = cityStat.IdealSunshineDays.ToString() }}
            },
            // Set the update expression for updating Price and Name
            UpdateExpression = "SET #TS = :tempScore, #SS = :sunScore, #HS = :humScore, #WS = :weatherScore, #ITD = :idealTempDays, #ISD = :idealSunDays",
            ReturnValues = "UPDATED_NEW" // Specifies that you want to get the new values of the updated attributes
        };
        try
        {
            // Send the update item request
            var response = _client.UpdateItemAsync(request).Result;

            // Output the updated attributes
            Console.WriteLine("Updated item:");
            foreach (var attribute in response.Attributes)
            {
                Console.WriteLine($"{attribute.Key}: {attribute.Value.S ?? attribute.Value.N}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating item in DynamoDB: {ex.Message}");
        }
    }

    public async void CreateWeatherHistoryItem(WeatherHistory historyItem)
    {
        var formattedHistoryItem = FormatHistoryItem(historyItem);
        await WriteItem(formattedHistoryItem);
    }

    public async void CreateWeatherHistoryItems(List<WeatherHistory> historyItems)
    {
        var historyItemsToWrite = historyItems.Select(x => FormatHistoryItem(x)).ToList();
        await BatchWriteItem(historyItemsToWrite);
    }

    private static Dictionary<string, AttributeValue> FormatCityWeatherScore(CityWeatherScore weatherScore)
    {
        var cityName = weatherScore.CityName.ToUpper().Replace(" ", "-");
        return new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"{GetCityWeatherScorePK()}" } },
            { "SK", new AttributeValue { S = $"WEATHERSCORE#{weatherScore.GetStringKeyForWeatherScore().PadLeft(10, '0')}#CITY#{cityName}" } },
            { "CityName", new AttributeValue { S = $"{cityName}" } },
        };
    }

    private static Dictionary<string, AttributeValue> FormatCityIdealSunDays(CityIdealSunDays cityIdealSunDays)
    {
        return new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"{GetCityWeatherScorePK()}" } },
            { "SK", new AttributeValue { S = $"IDEALSUNDAYS#{cityIdealSunDays.IdealSunDays.ToString("D4")}#CITY#{cityIdealSunDays.CityName}" } },
            { "CityName", new AttributeValue { S = $"{cityIdealSunDays.CityName}" } },
        };
    }
    
    private static Dictionary<string, AttributeValue> FormatHistoryItem(WeatherHistory historyItem)
    {
        var cityName = historyItem.CityName.ToUpper().Replace(" ", "-");
        return new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{historyItem.CityName}" } },
            { "SK", new AttributeValue { S = $"WEATHERHISTORY#{historyItem.Date.ToString()}" } },
            { "CityName", new AttributeValue { S = $"{cityName}" } },
            { "Date", new AttributeValue { S = historyItem.Date.ToString() } },
            { "Temperature", new AttributeValue { S = historyItem.Temperature.ToString() } },
            { "Sunshine", new AttributeValue { S = historyItem.Sunshine.ToString() } },
            { "Humidity", new AttributeValue { S = historyItem.Humidity.ToString() } },
        };
    }

    private async Task WriteItem(Dictionary<string, AttributeValue> item)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = item
        };

        // Execute PutItem request
        try
        {
            var resp = await _client.PutItemAsync(request);
            Console.WriteLine("Event inserted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting event: {ex.Message}");
        }
    }

    private async Task TransactWriteItem(List<Dictionary<string, AttributeValue>> items)
    {
        var putRequests = items.Select(item => new Put{Item = item, TableName = TableName}).ToList();
        var transactItems = putRequests.Select(item => new TransactWriteItem{Put = item}).ToList();
        var request = new TransactWriteItemsRequest
        {
            TransactItems = transactItems
        };

        try
        {
            var response = await _client.TransactWriteItemsAsync(request);
            Console.WriteLine("Transaction succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transaction failed: {ex.Message}");
        }
    }

    private async Task BatchWriteItem(List<Dictionary<string, AttributeValue>> items)
    {
        var writeItems = items.Select(kvp => new WriteRequest{
            PutRequest = new PutRequest(kvp)
        }).ToList();
        var chunkedItems = writeItems.Chunk(25).ToList();
        foreach (var chunk in chunkedItems)
        {
            Console.WriteLine("Processing Chunk");
            var requestItems = new Dictionary<string, List<WriteRequest>>()
            {
                {_table.TableName, chunk.ToList()}
            };
            BatchWriteItemRequest request = new BatchWriteItemRequest(){
                RequestItems = requestItems
            };
             try
            {
                var resp = await _client.BatchWriteItemAsync(request);
                Console.WriteLine("Event inserted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting event: {ex.Message}");
            }
        }
    }

    private async Task UpdateItem(Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> item)
    {
        var request = new UpdateItemRequest
        {
            TableName = TableName,
            Key = key,
            AttributeUpdates = item
        };

        // Execute PutItem request
        try
        {
            var resp = await _client.UpdateItemAsync(request);
            Console.WriteLine("Event inserted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting event: {ex.Message}");
        }
    }

    private string GetCityPK(string cityName)
    {
        return $"CITY#{cityName}";
    }

    private static string GetCityWeatherScorePK()
    {
        return $"CITY-WEATHER-SCORE";
    }

}