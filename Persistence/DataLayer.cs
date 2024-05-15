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


    public async void CreateCity(City city)
    {
         var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { "SK", new AttributeValue { S = $"CITY#{city.CityName}" } },
            { "CityName", new AttributeValue { S = $"{city.CityName}" } },
            { "WeatherScore", new AttributeValue { S = city.WeatherScore?.ToString().Trim() ?? "" } }
        };

        // Create PutItem request
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
}