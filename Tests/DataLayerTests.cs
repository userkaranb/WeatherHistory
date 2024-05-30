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
                    { DataStringConstants.PK, new AttributeValue { S = $"CITY#{city.CityName}" } },
                    { DataStringConstants.SK, new AttributeValue { S = $"CITY#{city.CityName}" } },
                    { DataStringConstants.CityDataObject.CityName, new AttributeValue { S = $"{city.CityName}" } }
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
        Assert.Equal("NEW-YORK", result.CityName);
    }

    [Fact]
    public async Task GetCity_ShouldPassInTheRightQuery()
    {
        var cityName = "New York";
        var city = new City(cityName);
        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
           .ReturnsAsync(new QueryResponse { Items = new List<Dictionary<string, AttributeValue>>() });

        var desiredQueryRequest = new QueryRequest
        {
            TableName = "Jubilado",
            KeyConditionExpression = "PK = :pk AND SK = :sk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = "CITY#NEW-YORK" } },
                { ":sk", new AttributeValue { S = "CITY#NEW-YORK" } }
            }
        };
        var result = _dataLayer.GetCity(city);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
       req.TableName == "Jubilado" &&
       req.KeyConditionExpression == "PK = :pk AND SK = :sk" &&
       req.ExpressionAttributeValues[":pk"].S == "CITY#NEW-YORK" &&
       req.ExpressionAttributeValues[":sk"].S == "CITY#NEW-YORK"), default), Times.Once);

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
    public async Task CreateCity_ShouldInsertTheRightPayload()
    {
        // Arrange
        float tempScore = 75f;
        float sunScore = 50f;
        float humScore = 100f;
        float weatherScore = 75f;
        int idealTempDays = 10;
        int idealSunDays = 10;
        var city = new City("New York", new CityStatWrapper("NEW-YORK", tempScore, sunScore, humScore,
        weatherScore, idealTempDays, idealSunDays));
        _mockDynamoDbClient
            .Setup(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), default))
            .Returns(Task.FromResult(new TransactWriteItemsResponse()));

        // Act
        _dataLayer.CreateCity(city);

        // Assert
        _mockDynamoDbClient.Verify(client => client.TransactWriteItemsAsync(It.Is<TransactWriteItemsRequest>(req =>
      req.TransactItems.First().Put.Item["PK"].S == "CITY#NEW-YORK" &&
      req.TransactItems.First().Put.Item["SK"].S == "CITY#NEW-YORK" &&
      req.TransactItems.First().Put.Item["CityName"].S == "NEW-YORK" &&
      req.TransactItems.First().Put.Item["HumScore"].S == humScore.ToString() &&
      req.TransactItems.First().Put.Item["TempScore"].S == tempScore.ToString() &&
      req.TransactItems.First().Put.Item["SunScore"].S == sunScore.ToString() &&
      req.TransactItems.First().Put.Item["IdealTempDays"].S == idealTempDays.ToString() &&
      req.TransactItems.First().Put.Item["IdealSunDays"].S == idealSunDays.ToString()), default), Times.Once);

    }

    [Fact]
    public async Task GetWeatherHistoryForCity_ShouldPassInTheRightQuery()
    {
        var city = new City("New York");
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>()
        };

        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        var result = _dataLayer.GetWeatherHistoryForCity(city);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
            req.TableName == "Jubilado" &&
            req.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
            req.ExpressionAttributeValues[":pk"].S == "CITY#NEW-YORK" &&
            req.ExpressionAttributeValues[":sk"].S == "WEATHERHISTORY#"
        ), default), Times.Once);
    }

    [Fact]
    public async Task GetWeatherHistoryForCity_ShouldReturnCorrectWeatherHistory()
    {
        var city = new City("NEW YORK");
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    { "CityName", new AttributeValue { S = "NEW-YORK" } },
                    { "Date", new AttributeValue { S = "2023-01-01" } },
                    { "Temperature", new AttributeValue { N = "30" } },
                    { "Sunshine", new AttributeValue { N = "5" } },
                    { "Humidity", new AttributeValue { N = "60" } }
                }
            }
        };

        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        var result = _dataLayer.GetWeatherHistoryForCity(city);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
            req.TableName == "Jubilado" &&
            req.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
            req.ExpressionAttributeValues[":pk"].S == "CITY#NEW-YORK" &&
            req.ExpressionAttributeValues[":sk"].S == "WEATHERHISTORY#"
        ), default), Times.Once);

        Assert.NotNull(result);
        Assert.Single(result);

        var weatherHistory = result.First();
        Assert.Equal("NEW-YORK", weatherHistory.CityName);
        Assert.Equal("2023-01-01", weatherHistory.Date);
        Assert.Equal(30, weatherHistory.Temperature);
        Assert.Equal(95, weatherHistory.Sunshine);
        Assert.Equal(60, weatherHistory.Humidity);
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
                { DataStringConstants.PK, new AttributeValue { S = "CITY#NEW-YORK" } },
                { DataStringConstants.SK, new AttributeValue { S = "WEATHERSCORE#75#CITY#NEW-YORK" } },
                { DataStringConstants.CityDataObject.CityName, new AttributeValue { S = "NEW-YORK" } },
                { DataStringConstants.CityDataObject.WeatherScore, new AttributeValue { S = "75" } }
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
                { DataStringConstants.PK, new AttributeValue { S = "CITY#NEW-YORK" } },
                { DataStringConstants.SK, new AttributeValue { S = "CITY#NEW-YORK" } }
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

    [Fact]
    public async Task GetSortedWeatherStat_CityIdealSunDays_ShouldPassInTheRightQueryAndReturnCorrectResult()
    {
        var sortKeyIdentifier = "IDEALSUNDAYS";
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    { "SK", new AttributeValue { S = "IDEALSUNDAYS#10#CITY#NEW-YORK" } },
                    { "CityName", new AttributeValue { S = "NEW-YORK" } },
                }
            }
        };

        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        var result = await _dataLayer.GetSortedWeatherStat<CityIdealSunDays>(sortKeyIdentifier);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
            req.TableName == "Jubilado" &&
            req.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
            req.ExpressionAttributeValues[":pk"].S == DataStringConstants.SortableCityStatsSKValues.CityWeatherScorePK &&
            req.ExpressionAttributeValues[":sk"].S == sortKeyIdentifier
        ), default), Times.Once);

        Assert.NotNull(result);
        Assert.Single(result);

        var weatherStat = result.First();
        Assert.Equal("NEW-YORK", weatherStat.CityName);
        Assert.Equal(10, weatherStat.IdealSunDays);
    }

    [Fact]
    public async Task GetSortedWeatherStat_CityIdealTempDays_ShouldPassInTheRightQueryAndReturnCorrectResult()
    {
        var sortKeyIdentifier = "IDEALTEMPDAYS";
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    { "SK", new AttributeValue { S = "IDEALTEMPDAYS#8#CITY#NEW-YORK" } },
                    { "CityName", new AttributeValue { S = "NEW-YORK" } }
                }
            }
        };

        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        var result = await _dataLayer.GetSortedWeatherStat<CityIdealTempDays>(sortKeyIdentifier);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
            req.TableName == "Jubilado" &&
            req.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
            req.ExpressionAttributeValues[":pk"].S == DataStringConstants.SortableCityStatsSKValues.CityWeatherScorePK &&
            req.ExpressionAttributeValues[":sk"].S == sortKeyIdentifier
        ), default), Times.Once);

        Assert.NotNull(result);
        Assert.Single(result);

        var weatherStat = result.First();
        Assert.Equal("NEW-YORK", weatherStat.CityName);
        Assert.Equal(8, weatherStat.IdealTempDays);
    }

    [Fact]
    public async Task GetSortedWeatherStat_CityWeatherScore_ShouldPassInTheRightQueryAndReturnCorrectResult()
    {
        var sortKeyIdentifier = "WEATHERSCORE";
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    { "SK", new AttributeValue { S = "WEATHERSCORE#89#CITY#NEW-YORK" } },
                    { "CityName", new AttributeValue { S = "NEW-YORK" } }
                }
            }
        };

        _mockDynamoDbClient
            .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(queryResponse);

        var result = await _dataLayer.GetSortedWeatherStat<CityWeatherScore>(sortKeyIdentifier);

        _mockDynamoDbClient.Verify(client => client.QueryAsync(It.Is<QueryRequest>(req =>
            req.TableName == "Jubilado" &&
            req.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
            req.ExpressionAttributeValues[":pk"].S == "CITY-WEATHER-SCORE" &&
            req.ExpressionAttributeValues[":sk"].S == "WEATHERSCORE"
        ), default), Times.Once);

        Assert.NotNull(result);
        Assert.Single(result);

        var weatherStat = result.First();
        Assert.Equal("NEW-YORK", weatherStat.CityName);
        Assert.Equal(89f, weatherStat.WeatherScore);
    }

    [Fact]
    public async Task CreateWeatherHistoryItems_ShouldFormatItemsCorrectlyAndBatchWrite()
    {
        // Arrange
        var historyItems = new List<WeatherHistory>
        {
            new WeatherHistory
            (
                "NEW-YORK",
                "2023-01-01",
                30,
                5,
                60
            ),
            new WeatherHistory
            (
                "LOS-ANGELES",
                "2023-01-01",
                75,
                10,
                50
            )
        };

        _mockDynamoDbClient
            .Setup(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default))
            .ReturnsAsync(new BatchWriteItemResponse());

        // Act
        _dataLayer.CreateWeatherHistoryItems(historyItems);

        // Assert
        _mockDynamoDbClient.Verify(client => client.BatchWriteItemAsync(It.Is<BatchWriteItemRequest>(req =>
            req.RequestItems["Jubilado"].Count == historyItems.Count &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY#NEW-YORK" &&
                item.PutRequest.Item[DataStringConstants.SK].S == "WEATHERHISTORY#2023-01-01" &&
                item.PutRequest.Item[DataStringConstants.CityDataObject.CityName].S == "NEW-YORK" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Date].S == "2023-01-01" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Temperature].S == "5" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Sunshine].S == "40" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Humidity].S == "30"
            ) &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY#LOS-ANGELES" &&
                item.PutRequest.Item[DataStringConstants.SK].S == "WEATHERHISTORY#2023-01-01" &&
                item.PutRequest.Item[DataStringConstants.CityDataObject.CityName].S == "LOS-ANGELES" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Date].S == "2023-01-01" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Temperature].S == "10" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Sunshine].S == "50" &&
                item.PutRequest.Item[DataStringConstants.WeatherHistoryDataObject.Humidity].S == "75"
            )
        ), default), Times.Once);
    }

    [Fact]
    public async Task CreateWeatherHistoryItems_ShouldHandleEmptyList()
    {
        // Arrange
        var historyItems = new List<WeatherHistory>();

        _mockDynamoDbClient
            .Setup(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default))
            .ReturnsAsync(new BatchWriteItemResponse());

        // Act
        _dataLayer.CreateWeatherHistoryItems(historyItems);

        // Assert
        _mockDynamoDbClient.Verify(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateSortableWeatherSortKey_ShouldFormatAndBatchWrite_CityIdealSunDays()
    {
        var scores = new List<CityIdealSunDays>
        {
            new CityIdealSunDays("NEW-YORK", 10),
            new CityIdealSunDays("LOS-ANGELES", 20)
        };

        _mockDynamoDbClient
            .Setup(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default))
            .ReturnsAsync(new BatchWriteItemResponse());

        _dataLayer.CreateSortableWeatherSortKey(scores);

        _mockDynamoDbClient.Verify(client => client.BatchWriteItemAsync(It.Is<BatchWriteItemRequest>(req =>
            req.RequestItems["Jubilado"].Count == scores.Count &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"IDEALSUNDAYS#0010#CITY#NEW-YORK"
            ) &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"IDEALSUNDAYS#0020#CITY#LOS-ANGELES"
            )
        ), default), Times.Once);
    }

    [Fact]
    public async Task CreateSortableWeatherSortKey_ShouldFormatAndBatchWrite_CityIdealTempDays()
    {
        var scores = new List<CityIdealTempDays>
        {
            new CityIdealTempDays("NEW-YORK", 10),
            new CityIdealTempDays("LOS-ANGELES", 20)
        };

        _mockDynamoDbClient
            .Setup(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default))
            .ReturnsAsync(new BatchWriteItemResponse());

        _dataLayer.CreateSortableWeatherSortKey(scores);

        _mockDynamoDbClient.Verify(client => client.BatchWriteItemAsync(It.Is<BatchWriteItemRequest>(req =>
            req.RequestItems["Jubilado"].Count == scores.Count &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"{DataStringConstants.SortableCityStatsSKValues.IdealTempDays}#0010#CITY#NEW-YORK"
            ) &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"{DataStringConstants.SortableCityStatsSKValues.IdealTempDays}#0020#CITY#LOS-ANGELES"
            )
        ), default), Times.Once);
    }

    [Fact]
    public async Task CreateSortableWeatherSortKey_ShouldFormatAndBatchWrite_CityWeatherScore()
    {
        var scores = new List<CityWeatherScore>
        {
            new CityWeatherScore("NEW-YORK", 75.5f),
            new CityWeatherScore("LOS-ANGELES", 185.3f)
        };

        _mockDynamoDbClient
            .Setup(client => client.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), default))
            .ReturnsAsync(new BatchWriteItemResponse());

        _dataLayer.CreateSortableWeatherSortKey(scores);

        _mockDynamoDbClient.Verify(client => client.BatchWriteItemAsync(It.Is<BatchWriteItemRequest>(req =>
            req.RequestItems["Jubilado"].Count == scores.Count &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"{DataStringConstants.SortableCityStatsSKValues.WeatherScore}#0000075.50#CITY#NEW-YORK"
            ) &&
            req.RequestItems["Jubilado"].Any(item =>
                item.PutRequest.Item[DataStringConstants.PK].S == "CITY-WEATHER-SCORE" &&
                item.PutRequest.Item[DataStringConstants.SK].S == $"{DataStringConstants.SortableCityStatsSKValues.WeatherScore}#0000185.30#CITY#LOS-ANGELES"
            )
        ), default), Times.Once);
    }

    // Additional tests for other methods...
}
