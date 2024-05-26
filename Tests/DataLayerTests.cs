using Xunit;
using Moq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Jubilado;
using Jubilado.Persistence;

namespace JubiladoUnitTests;

public class DataLayerTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDbClient;
    private readonly Mock<ITableLoader> _mockTableLoader;

    private readonly DataLayer _dataLayer;

    public DataLayerTests()
    {
        _mockDynamoDbClient = new Mock<IAmazonDynamoDB>();
        _mockTableLoader = new Mock<ITableLoader>();
        // var mockTable = new Mock<Table>(_mockDynamoDbClient.Object, "Jubilado");
        _mockTableLoader.Setup(loader => loader.LoadTable(It.IsAny<IAmazonDynamoDB>(), It.IsAny<string>()))
                        .Returns(It.IsAny<Table>);

        _dataLayer = new DataLayer(_mockDynamoDbClient.Object, _mockTableLoader.Object);
    }

    [Fact]
    public async Task GetCity_ShouldReturnCity()
    {
        // Arrange
        var cityName = "New York";
        var city = new City(cityName);
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"CITY#{city.CityName}" } },
                    { "SK", new AttributeValue { S = $"CITY#{city.CityName}" } },
                    { "CityName", new AttributeValue { S = $"{city.CityName}" } }
                }
            }
        };
        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        // Act
        var result = _dataLayer.GetCity(city);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(city.CityName, result.CityName);
    }

    [Fact]
    public async Task CreateCity_ShouldCallPutItem()
    {
        // Arrange
        var city = new City("New York", new CityStatWrapper("New York", 75f, 50f, 100f, 75f, 10, 10));
        _mockDynamoDbClient
            .Setup(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), default))
            .Returns(Task.FromResult(new TransactWriteItemsResponse()));

        // Act
        _dataLayer.CreateCity(city);

        // Assert
        _mockDynamoDbClient.Verify(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task GetExistingWeatherScoreAttributeByScan_ShouldReturnAttributes()
    {
        // Arrange
        var scanResponse = new ScanResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
        {
            new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = "CITY#NEW-YORK" } },
                { "SK", new AttributeValue { S = "WEATHERSCORE#75#CITY#NEW-YORK" } },
                { "CityName", new AttributeValue { S = "NEW-YORK" } },
                { "WeatherScore", new AttributeValue { S = "75" } }
            }
        }
        };
        _mockDynamoDbClient
            .Setup(client => client.ScanAsync(It.IsAny<ScanRequest>(), default))
            .ReturnsAsync(scanResponse);

        // Act
        var result = await _dataLayer.GetExistingWeatherScoreAttributeByScan<CityWeatherScore>("CityName, WeatherScore");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task DeleteCity_ShouldCallTransactWriteItemsAsync()
    {
        // Arrange
        var city = new City("New York");
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
        {
            new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = "CITY#New York" } },
                { "SK", new AttributeValue { S = "CITY#New York" } }
            }
        }
        };
        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);
        _mockDynamoDbClient
            .Setup(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), default))
            .ReturnsAsync(new TransactWriteItemsResponse());

        // Act
        _dataLayer.DeleteCity(city);

        // Assert
        _mockDynamoDbClient.Verify(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), default), Times.AtLeastOnce);
    }

    // Additional tests for other methods...
}
