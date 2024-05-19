using System.Data;
using Jubilado;
using Jubilado.Persistence;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.TagHelpers;

public class WeatherScoreCalculator
{
    private DataLayer _dataLayer;
    private const float IDEAL_TEMP = 75f;
    private const float IDEAL_SUN = 100f;
    private const float IDEAL_HUM = 50f;
    public WeatherScoreCalculator()
    {
        _dataLayer = new DataLayer();
    }
    public Dictionary<City, CityStatWrapper> PersistWeatherForCities(List<City> cities = null)
    {
        var cityList = cities;
        Dictionary<City, CityStatWrapper> results = new Dictionary<City, CityStatWrapper>();
        if(cityList == null)
        {
            cityList = CityListHelper.GetCitiesOfInterest();
        }
        foreach(var city in cityList)
        {
            var cityName = city.CityName;
            var weatehrHistory = _dataLayer.GetWeatherHistoryForCity(cityName);
            var weatherScore = CalculateWeatherScore(cityName, weatehrHistory);
            results[city] = weatherScore;
            // insert
            // refactor for DI
        }
        // var sortedByValue = results.OrderBy(pair => pair.Value.IdealSunshineDays);
        // foreach(var sortedVal in sortedByValue)
        // {
        //     Console.WriteLine($"{sortedVal.Key.CityName}: {sortedVal.Value.IdealSunshineDays}");
        // }
        return results;
    }

    private CityStatWrapper CalculateWeatherScore(string cityName, List<WeatherHistory> histories)
    {
        // for now, just calculate MAD score for temperature
        var temps = histories.Select(x => (float)x.Temperature).ToList();
        var sunshines = histories.Select(x => (float)x.Sunshine).ToList();
        
        var tempScore = CalculateIdealMetric(temps, IDEAL_TEMP);
        var sunshineScore = CalculateIdealMetric(sunshines, IDEAL_SUN);
        var humidityScore = CalculateIdealMetric(histories.Select(x => (float)x.Humidity).ToList(), IDEAL_HUM);
        var weatherScore = tempScore + sunshineScore + humidityScore;
        var idealTempDays = temps.Where(temp => temp > 68 && temp < 83);
        var idealSunshineDays = sunshines.Where(sun => sun > 50);
        
        return new CityStatWrapper(cityName, tempScore, sunshineScore, humidityScore, weatherScore, idealTempDays.Count(), idealSunshineDays.Count()); 
    }

    private float CalculateIdealMetric(List<float> metrics, float ideal)
    {
        return (float)metrics.Select(metric => Math.Abs(ideal - metric)).Average();
    }
}