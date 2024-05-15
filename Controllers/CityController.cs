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
            return new JsonResult("Foo");
        }

        [HttpPut()]
        public IActionResult CreatCity([FromBody] City city)
        {
            return new JsonResult("Foo");
        }
    }
}
