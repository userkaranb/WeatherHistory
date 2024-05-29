using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jubilado.Controllers;
using Jubilado.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Jubilado;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace JubiladoE2ETests
{

    public class CityControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        public WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Mock<ICityCreatorService> _mockCityCreatorService;
        private readonly Mock<IDataLayer> _mockDataLayer;
        private readonly Mock<ICityGetterService> _mockCityGetterService;

        public CityControllerTests(WebApplicationFactory<Program> factory)
        {
            _mockCityCreatorService = new Mock<ICityCreatorService>();
            _mockDataLayer = new Mock<IDataLayer>();
            _mockCityGetterService = new Mock<ICityGetterService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_mockCityCreatorService.Object);
                    services.AddSingleton(_mockDataLayer.Object);
                    services.AddSingleton(_mockCityGetterService.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Get_ShouldReturnCity()
        {
            var cityName = "NewYork";
            var city = new City(cityName);
            _mockDataLayer.Setup(d => d.GetCity(It.IsAny<City>())).Returns(city);

            var response = await _client.GetAsync($"/api/city/{cityName}");

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain(cityName);
        }

        [Fact]
        public async Task CreateCity_ShouldReturnOk()
        {
            var cityRequests = new List<CreateCityRequest> { new CreateCityRequest { CityName = "NewYork" } };
            var content = new StringContent(JsonConvert.SerializeObject(cityRequests), Encoding.UTF8, "application/json");

            var response = await _client.PutAsync("/api/city", content);

            response.EnsureSuccessStatusCode();
            _mockCityCreatorService.Verify(s => s.CreateCity(It.IsAny<List<City>>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCity_ShouldReturnOk()
        {
            var cityName = "NewYork";

            var response = await _client.DeleteAsync($"/api/city/{cityName}");

            response.EnsureSuccessStatusCode();
            _mockCityCreatorService.Verify(s => s.DeleteCity(It.IsAny<City>()), Times.Once);
        }

        [Fact]
        public async Task GetWeatherHistory_ShouldReturnHistory()
        {
            var cityName = "NewYork";
            var history = new List<WeatherHistory> { new WeatherHistory(cityName, "2024-05-29", 75, 0, 0) };
            _mockDataLayer.Setup(d => d.GetWeatherHistoryForCity(It.IsAny<City>())).Returns(history);

            var response = await _client.GetAsync($"/api/city/{cityName}/weatherhistory");

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain(cityName);
        }

        [Fact]
        public async Task GetTopWeatherScores_ShouldReturnScores()
        {
            var topScores = new List<CityWeatherScore> { new CityWeatherScore("NewYork", 75) };
            _mockCityGetterService.Setup(s => s.GetTopWeatherScoreCities()).ReturnsAsync(topScores);

            var response = await _client.GetAsync("/api/city/topweatherscores");

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("NewYork");
        }

        [Fact]
        public async Task GetTopSunDays_ShouldReturnSunDays()
        {
            var topSunDays = new List<CityIdealSunDays> { new CityIdealSunDays("NewYork", 200) };
            _mockCityGetterService.Setup(s => s.GetTopIdealSunDays()).ReturnsAsync(topSunDays);

            var response = await _client.GetAsync("/api/city/topsundays");

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("NewYork");
        }
    }
}
