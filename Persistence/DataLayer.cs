using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace Jubilado.Persistence;

public class DataLayer
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

    public City GetCity(string cityName)
    {
        var cityNameUpper = cityName.ToUpper();
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND SK = :sk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"CITY#{cityNameUpper}" } },
                { ":sk", new AttributeValue { S = $"CITY#{cityNameUpper}" } }
            }
        };
        var queryResponse = _client.QueryAsync(queryRequest).GetAwaiter().GetResult();
        var toDict = queryResponse.Items.First().ToDictionary<string, AttributeValue>();
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
            { "WeatherScore", new AttributeValue { S = city.WeatherScore?.ToString().Trim() ?? "" } }
        };

       await WriteItem(item);
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
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{formattedHistoryItem.CityName}" } },
            { "SK", new AttributeValue { S = $"WEATHERHISTORY#{formattedHistoryItem.Date.ToString()}" } },
            { "CityName", new AttributeValue { S = $"{formattedHistoryItem.CityName}" } },
            { "Date", new AttributeValue { S = formattedHistoryItem.Date.ToString() } },
            { "Temperature", new AttributeValue { S = formattedHistoryItem.Temperature.ToString() } },
            { "Sunshine", new AttributeValue { S = formattedHistoryItem.Sunshine.ToString() } },
            { "Humidity", new AttributeValue { S = formattedHistoryItem.Humidity.ToString() } },
        };

        await WriteItem(item);
    }

    private static WeatherHistory FormatHistoryItem(WeatherHistory historyItem)
    {
        var cityName = historyItem.CityName.ToUpper().Replace(" ", "-");
        return new WeatherHistory(cityName, historyItem.Date, historyItem.Humidity, historyItem.Temperature, historyItem.Sunshine);
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
}