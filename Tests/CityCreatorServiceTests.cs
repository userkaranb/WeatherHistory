namespace JubiladoUnitTests;

using Xunit;
using Jubilado.Persistence;
using Jubilado;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CityCreatorServiceTests
{
    private readonly Mock<IDataLayer> _mockDataLayer;
    private readonly Mock<ICityWeatherHistoryApiCaller> _mockWeatherHistoryApiCaller;
    private readonly CityCreatorService _service;

    public CityCreatorServiceTests()
    {
        _mockDataLayer = new Mock<IDataLayer>();
        _mockWeatherHistoryApiCaller = new Mock<ICityWeatherHistoryApiCaller>();
        _service = new CityCreatorService(_mockDataLayer.Object, _mockWeatherHistoryApiCaller.Object);
    }

    [Fact]
    public async Task CreateCity_ShouldCallWeatherHistoryApiForMissingCities()
    {
        // Arrange
        var cities = new List<City> { new City("New York"), new City("Los Angeles") };
        _mockDataLayer.Setup(d => d.GetCity(It.IsAny<City>())).Returns<City>(city => city);

        // Act
        await _service.CreateCity(cities);

        // Assert
        _mockWeatherHistoryApiCaller.Verify(c => c.DownloadWeatherHistoryItems(It.IsAny<List<City>>()), Times.Once);
    }

    [Fact]
    public async Task CreateCity_ShouldCreateWeatherHistoryItems()
    {
        // Arrange
        var nyc = new City("New York");
        var cities = new List<City> { nyc };
        var weatherHistory = new List<WeatherHistory> { new WeatherHistory("New York", "2024-5-1", 2.0, 70, 3.0) };
        _mockDataLayer.Setup(d => d.GetCity(It.IsAny<City>())).Returns((City)null);
        _mockWeatherHistoryApiCaller.Setup(c => c.DownloadWeatherHistoryItems(It.IsAny<List<City>>())).ReturnsAsync(weatherHistory);
        _mockDataLayer.Setup(d => d.GetCity(It.IsAny<City>())).Returns(nyc);

        // Act
        await _service.CreateCity(cities);

        // Assert
        _mockDataLayer.Verify(d => d.CreateWeatherHistoryItems(weatherHistory), Times.Once);
    }

    [Fact]
    public async Task DeleteCity_ShouldCallDataLayerDelete()
    {
        // Arrange
        var city = new City("New York");

        // Act
        await _service.DeleteCity(city);

        // Assert
        _mockDataLayer.Verify(d => d.DeleteCity(city), Times.Once);
    }

    // [Fact]
    // public void CalculateIdealMetric_ShouldReturnCorrectAverage()
    // {
    //     // Arrange
    //     var metrics = new List<float> { 70f, 80f, 75f };
    //     var ideal = 75f;

    //     // Act
    //     var result = _service.CalculateIdealMetric(metrics, ideal);

    //     // Assert
    //     Assert.Equal(5f, result);
    // }
}