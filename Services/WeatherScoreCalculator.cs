using System.Data;
using Jubilado;
using Jubilado.Persistence;

public class WeatherScoreCalculator
{
    private DataLayer _dataLayer;
    public WeatherScoreCalculator()
    {
        _dataLayer = new DataLayer();
    }
    public Dictionary<City, float> PersistWeatherForCities(List<string> cities = null)
    {
        var cityList = cities;
        if(cityList == null)
        {
            cityList = CityListHelper.GetCitiesOfInterest();
        }
        foreach(var city in cityList)
        {
            Console.WriteLine(city);
            var weatehrHistory = _dataLayer.GetWeatherHistoryForCity(city);
            // calculate score
            // insert
        }
        return new Dictionary<City, float>();
    }
}