// using Xunit;
// using Moq;
// using Amazon.DynamoDBv2;
// using Amazon.DynamoDBv2.Model;
// using Amazon.SecretsManager;
// using Jubilado.Persistence;

// public class DataLayerTests
// {
//     private readonly Mock<IAmazonDynamoDB> _mockDynamoDbClient;
//     private readonly Mock<IAmazonSecretsManager> _mockSecretsManagerClient;
//     private readonly DataLayer _dataLayer;

//     public DataLayerTests()
//     {
//         _mockDynamoDbClient = new Mock<IAmazonDynamoDB>();
//         _mockSecretsManagerClient = new Mock<IAmazonSecretsManager>();
//         _dataLayer = new DataLayer(_mockDynamoDbClient.Object, _mockSecretsManagerClient.Object);
//     }

//     [Fact]
//     public async Task GetCity_ShouldReturnCity()
//     {
//         // Arrange
//         var cityName = "New York";
//         var city = new City { CityName = cityName };
//         var queryResponse = new QueryResponse
//         {
//             Items = new List<Dictionary<string, AttributeValue>>
//             {
//                 new Dictionary<string, AttributeValue>
//                 {
//                     { "PK", new AttributeValue { S = $"CITY#{cityName}" } },
//                     { "SK", new AttributeValue { S = $"CITY#{cityName}" } }
//                 }
//             }
//         };
//         _mockDynamoDbClient
//             .Setup(client => client.QueryAsync(It.IsAny<QueryRequest>(), default))
//             .ReturnsAsync(queryResponse);

//         // Act
//         var result = _dataLayer.GetCity(city);

//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(cityName, result.CityName);
//     }

//     [Fact]
//     public async Task CreateCity_ShouldCallPutItem()
//     {
//         // Arrange
//         var city = new City { CityName = "New York", CityStats = new CityStatWrapper(75, 50, 100, 75, 10, 10) };
//         _mockDynamoDbClient
//             .Setup(client => client.PutItemAsync(It.IsAny<PutItemRequest>(), default))
//             .Returns(Task.FromResult(new PutItemResponse()));

//         // Act
//         _dataLayer.CreateCity(city);

//         // Assert
//         _mockDynamoDbClient.Verify(client => client.PutItemAsync(It.IsAny<PutItemRequest>(), default), Times.Once);
//     }

//     // Additional tests for other methods...
// }