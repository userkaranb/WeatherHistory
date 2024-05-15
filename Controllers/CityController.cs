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

        [HttpPut()]
        public IActionResult CreateWeatherHistory([FromBody] WeatherHistory historyItem)
        {
            new DataLayer().CreateWeatherHistoryItem(historyItem);
            return Ok("created.");
        }
    }
}
