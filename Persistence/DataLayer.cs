using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Diagnostics;

namespace Jubilado.Persistence;

public interface IDataLayer
{
    public City GetCity(City city);
    Task<List<T>> GetExistingWeatherScoreAttributeByScan<T>(string attributesToScan) where T : SortableCityWeatherAttribute;
    Task<List<T>> GetSortedWeatherStat<T>(string sortKeyIdentifier, bool isOrderDescending = false) where T : SortableCityWeatherAttribute;
    public List<WeatherHistory> GetWeatherHistoryForCity(string cityName);
    void CreateCity(City city);
    void DeleteCity(City city);
    void CreateWeatherHistoryItems(List<WeatherHistory> historyItems);
    void CreateSortableWeatherSortKey<T>(List<T> scores) where T : SortableCityWeatherAttribute;
}

public class DataLayer : IDataLayer
{
    private Table _table;
    const string TableName = "Jubilado";
    private readonly IAmazonDynamoDB _dynamoDbClient;

    public DataLayer(IAmazonDynamoDB dynamoDbClient, ITableLoader tableLoader)
    {
        _dynamoDbClient = dynamoDbClient;
        _table = tableLoader.LoadTable(dynamoDbClient, TableName);
    }
    public async Task<List<T>> GetExistingWeatherScoreAttributeByScan<T>(string attributesToScan) where T : SortableCityWeatherAttribute
    {
        var scanResults = await PerformScan(attributesToScan);
        return scanResults.Items.Select(item => ItemFactory.ToSortableCityWeatherScoreItem<T>(item)).ToList();
    }

    public async void CreateSortableWeatherSortKey<T>(List<T> scores) where T : SortableCityWeatherAttribute
    {
        var dictToWrite = scores.Select(score => FromSortableWeatherScoreItem<T>(score)).ToList();
        await BatchWriteItem(dictToWrite);
    }

    public async void DeleteCity(City city)
    {
        var cityName = city.CityName;
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
        var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);
        var transactItems = extraItemsToDelete.Select(x => new Amazon.DynamoDBv2.Model.TransactWriteItem { Delete = x }).ToList();

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
                await _dynamoDbClient.TransactWriteItemsAsync(transactWriteItemsRequest);
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

        var extraQueryResponse = await _dynamoDbClient.QueryAsync(extraQueryRequest);
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

        var scanResponse = await _dynamoDbClient.ScanAsync(scanRequest);
        return scanResponse;
    }

    public async Task<List<T>> GetSortedWeatherStat<T>(string sortKeyIdentifier, bool isOrderDescending = false)
       where T : SortableCityWeatherAttribute
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY-WEATHER-SCORE" } },
                { ":sk", new AttributeValue { S = sortKeyIdentifier } },
            }
        };

        if (isOrderDescending)
        {
            queryRequest.ScanIndexForward = false;
        }
        var queryResponse = _dynamoDbClient.QueryAsync(queryRequest).GetAwaiter().GetResult();
        var toDict = queryResponse.Items;
        return ItemFactory.ToSortableCityScoreBulk<T>(toDict);
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
        var queryResponse = _dynamoDbClient.QueryAsync(queryRequest).GetAwaiter().GetResult();
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
        var queryResponse = _dynamoDbClient.QueryAsync(queryRequest).GetAwaiter().GetResult();
        return queryResponse.Items.Select(x => ItemFactory.ToWeatherHistory(x)).ToList();
    }


    public async void CreateCity(City city)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { "SK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { DataStringConstants.CityDataObject.CityName, new AttributeValue { S = $"{city.CityName}" } },
            { DataStringConstants.CityDataObject.TempScore, new AttributeValue { S = city.CityStats?.TemperatureScore.ToString().Trim() } },
            { DataStringConstants.CityDataObject.HumScore, new AttributeValue { S = city.CityStats?.HumidityScore.ToString().Trim() } },
            { DataStringConstants.CityDataObject.IdealSunDays, new AttributeValue { S = city.CityStats?.IdealSunshineDays.ToString().Trim() } },
            { DataStringConstants.CityDataObject.IdealTempDays, new AttributeValue { S = city.CityStats?.IdealTempRangeDays.ToString().Trim() } },
            { DataStringConstants.CityDataObject.SunScore, new AttributeValue { S = city.CityStats?.SunshineScore.ToString().Trim() } },
            { DataStringConstants.CityDataObject.WeatherScore, new AttributeValue { S = city.CityStats?.WeatherScore.ToString().Trim() } },
        };

        var weatherScoreItem = FromSortableWeatherScoreItem<CityWeatherScore>(new CityWeatherScore(city.CityName, city.CityStats.WeatherScore));
        var sunDaysItem = FromSortableWeatherScoreItem<CityIdealSunDays>(new CityIdealSunDays(city.CityName, city.CityStats.IdealSunshineDays));
        var idealTempDaysItem = FromSortableWeatherScoreItem<CityIdealTempDays>(new CityIdealTempDays(city.CityName, city.CityStats.IdealTempRangeDays));
        await TransactWriteItem(new List<Dictionary<string, AttributeValue>> { item, weatherScoreItem, sunDaysItem, idealTempDaysItem });
    }

    public async void CreateWeatherHistoryItems(List<WeatherHistory> historyItems)
    {
        var historyItemsToWrite = historyItems.Select(x => FormatHistoryItem(x)).ToList();
        await BatchWriteItem(historyItemsToWrite);
    }

    private static Dictionary<string, AttributeValue> FromSortableWeatherScoreItem<T>(T item) where T : SortableCityWeatherAttribute
    {
        var startingDict = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"{GetCityWeatherScorePK()}" } },
            { DataStringConstants.CityDataObject.CityName, new AttributeValue { S = $"{item.CityName}" } },
        };
        var skSuffix = $"#{item.GetFormattedNumber()}#CITY#{item.CityName}";
        if (typeof(T) == typeof(CityWeatherScore))
        {
            startingDict["SK"] = new AttributeValue { S = $"WEATHERSCORE{skSuffix}" };
            return startingDict;
        }

        if (typeof(T) == typeof(CityIdealSunDays))
        {
            startingDict["SK"] = new AttributeValue { S = $"IDEALSUNDAYS{skSuffix}" };
            return startingDict;
        }
        if (typeof(T) == typeof(CityIdealTempDays))
        {
            startingDict["SK"] = new AttributeValue { S = $"IDEALTEMPDAYS{skSuffix}" };
            return startingDict;
        }
        throw new InvalidOperationException("Unsupported type");
    }

    private static Dictionary<string, AttributeValue> FormatHistoryItem(WeatherHistory historyItem)
    {
        var cityName = historyItem.CityName.ToUpper().Replace(" ", "-");
        return new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{historyItem.CityName}" } },
            { "SK", new AttributeValue { S = $"WEATHERHISTORY#{historyItem.Date.ToString()}" } },
            { DataStringConstants.CityDataObject.CityName, new AttributeValue { S = $"{cityName}" } },
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
            var resp = await _dynamoDbClient.PutItemAsync(request);
            Console.WriteLine("Event inserted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting event: {ex.Message}");
        }
    }

    private async Task TransactWriteItem(List<Dictionary<string, AttributeValue>> items)
    {
        var putRequests = items.Select(item => new Put { Item = item, TableName = TableName }).ToList();
        var transactItems = putRequests.Select(item => new TransactWriteItem { Put = item }).ToList();
        var request = new TransactWriteItemsRequest
        {
            TransactItems = transactItems
        };

        try
        {
            var response = await _dynamoDbClient.TransactWriteItemsAsync(request);
            Console.WriteLine("Transaction succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transaction failed: {ex.Message}");
        }
    }

    private async Task BatchWriteItem(List<Dictionary<string, AttributeValue>> items)
    {
        var writeItems = items.Select(kvp => new WriteRequest
        {
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
            BatchWriteItemRequest request = new BatchWriteItemRequest()
            {
                RequestItems = requestItems
            };
            try
            {
                var resp = await _dynamoDbClient.BatchWriteItemAsync(request);
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
            var resp = await _dynamoDbClient.UpdateItemAsync(request);
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
