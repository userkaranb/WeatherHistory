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
        [HttpGet("{cityName}")]
        public JsonResult Get(string cityName)
        {
            return new JsonResult(new DataLayer().GetCity(cityName));
        }

        [HttpPut()]
        public IActionResult CreatCity([FromBody] City city)
        {
            new DataLayer().CreateCity(city);
            return Ok("done");
        }

        [HttpPut("weatherhistory")]
        public IActionResult CreateWeatherHistory([FromBody] WeatherHistory historyItem)
        {
            if (ValidateWeatherHistoryObject(historyItem))
            {
                 new DataLayer().CreateWeatherHistoryItem(historyItem);
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
            return new JsonResult(new DataLayer().GetWeatherHistoryForCity(cityName));
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
