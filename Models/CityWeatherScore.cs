using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityWeatherScore : Item
{

    public string CityName {private set; get;}

    public float WeatherScore { private set; get;}

    public CityWeatherScore(string cityName, float weatherScore)
    {
        CityName = cityName;
        WeatherScore = weatherScore;
    }

    public override string ToString()
    {
        return $"{CityName}-{WeatherScore.ToString()}";
    }

}
