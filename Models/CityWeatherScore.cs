using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityWeatherScore : SortableCityWeatherAttribute
{
    public float WeatherScore { private set; get;}

    public CityWeatherScore(string cityName, float weatherScore) : base(cityName)
    {
        WeatherScore = (float)Math.Round(weatherScore, 2);
    }

    public override string GetFormattedNumber()
    {
        return WeatherScore.ToString("F2").PadLeft(10, '0');
    }

    public override string ToString()
    {
        return $"{CityName}-{WeatherScore.ToString()}";
    }

}
