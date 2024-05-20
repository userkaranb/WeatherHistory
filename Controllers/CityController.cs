using Amazon.DynamoDBv2.Model;
using Jubilado.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jubilado.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly ICityCreatorService _cityCreatorService;
        private readonly IDataLayer _dataLayer;
        public CityController(ICityCreatorService cityCreatorService, IDataLayer dataLayer)
        {
            _cityCreatorService = cityCreatorService;
            _dataLayer = dataLayer;
        }

        [HttpGet("{cityName}")]
        public JsonResult Get(string cityName)
        {
            return new JsonResult(_dataLayer.GetCity(new City(cityName)));
        }

        [HttpPut()]
        public async Task<IActionResult> CreatCity([FromBody] List<CreateCityRequest> request)
        {
            var cityList = request.Select(city => new City(city.CityName)).ToList();
            // DI me
            await _cityCreatorService.CreateCity(cityList);
            return Ok("done");
        }

        [HttpPut("weatherhistory")]
        public IActionResult CreateWeatherHistory([FromBody] WeatherHistory historyItem)
        {
            if (ValidateWeatherHistoryObject(historyItem))
            {
                 _dataLayer.CreateWeatherHistoryItem(historyItem);
                return Ok("created.");       
            }
            else
            {
                return BadRequest($"Date for History Item {historyItem.Date} is not formatted as YYYY-MM-DD");
            }
        }

        [HttpGet("{cityName}/weatherhistory")]
        public JsonResult GetWeatherHistory(string cityName)
        {
            return new JsonResult(_dataLayer.GetWeatherHistoryForCity(cityName));
        }

        private bool ValidateWeatherHistoryObject(WeatherHistory historyItem)
        {
            string dateString = historyItem.Date; // Example date string in YYYY-MM-DD format

            // Define the expected date format
            string[] formats = { "yyyy-MM-dd" };

            // Try parsing the date string using the specified format
            if (DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                Console.WriteLine("Date string is in the correct format.");
                Console.WriteLine("Parsed date: " + result.ToString("yyyy-MM-dd"));
                return true;
            }
            else
            {
                Console.WriteLine("Date string is not in the correct format.");
                return false;
            }
        }
    }
}
