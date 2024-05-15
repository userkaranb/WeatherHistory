using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class City : Item
{

    public string CityName {private set; get;}
    public float? WeatherScore {private set; get;}

    public City(string cityName, float? weatherScore)
    {
        CityName = cityName;
        WeatherScore = weatherScore;
    }

}
