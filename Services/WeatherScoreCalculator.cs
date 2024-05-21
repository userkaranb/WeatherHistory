using System.Data;
using Jubilado;
using Jubilado.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Collections;
using Amazon.DynamoDBv2.Model.Internal.MarshallTransformations;
using Amazon.DynamoDBv2.Model;


public interface ICityCreatorService
{
    Task CreateCity(List<City> cities);
}

public class CityCreatorService : ICityCreatorService
{
    private IDataLayer _dataLayer;
    private const float IDEAL_TEMP = 75f;
    private const float IDEAL_SUN = 100f;
    private const float IDEAL_HUM = 50f;
    private HttpClient _client;

    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";

    public CityCreatorService(IDataLayer dataLayer)
    {
        _dataLayer = dataLayer;
        _client = new HttpClient();
    }
    
    public async Task CreateCity(List<City> cities)
    {
        var cityKeyToCityValue = cities.Select(x => (x, _dataLayer.GetCity(x))).ToDictionary<City, City>();
        var citiesThatDontHaveValues = cityKeyToCityValue.Where(x => City.IsCityEmpty(x.Value)).Select(x => x.Key).ToList();
        await DownloadAndPersistWeatherHistoryItems(citiesThatDontHaveValues);
        var cityToCityStatMappings = GetWeatherStatsForCities(citiesThatDontHaveValues);
        foreach(var city in cityToCityStatMappings.Keys)
        {
            city.CityStats = cityToCityStatMappings[city];
            _dataLayer.CreateCity(city);
        }
    }

    private async Task DownloadAndPersistWeatherHistoryItems(List<City> citiesList)
    {
        var (start, end) = GetDateRanges();
        foreach(var city in citiesList)
        {
            var url = GetFormattedUrl(city.CityName, start, end);
            var historyItems = await FetchFromApi(city.CityName, url);
            _dataLayer.CreateWeatherHistoryItems(historyItems);
        }
    }

    private static Tuple<string, string> GetDateRanges()
    {
        return new Tuple<string, string>(new DateOnly(2023, 1, 1).ToString("yyyy-MM-dd"), 
        new DateOnly(2023, 12, 31).ToString("yyyy-MM-dd"));
    }

    private static string GetFormattedUrl(string cityName, string startDate, string endDate)
    {
        var formattedCity = cityName.Replace(" ", "%20");
        return $"{URI}{formattedCity}/{startDate.ToString()}/{endDate.ToString()}?key={KEY}";
    }


    private Dictionary<City, CityStatWrapper> GetWeatherStatsForCities(List<City> cities)
    {
        Dictionary<City, CityStatWrapper> results = new Dictionary<City, CityStatWrapper>();
        foreach(var city in cities)
        {
            var cityName = city.CityName;
            var weatehrHistory = _dataLayer.GetWeatherHistoryForCity(cityName);
            var weatherStats = CalculateWeatherScore(cityName, weatehrHistory);
            results[city] = weatherStats;
        }
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

    private static async Task<List<WeatherHistory>> ConvertJsonToWeatherHistory(Dictionary<string, object> jsonBlob, string cityName)
    {
        List<WeatherHistory> weatherHistoryItems = new List<WeatherHistory>();
        var dailyWeatherHistory = jsonBlob["days"] as List<object>;
        JArray children = (JArray)jsonBlob["days"];

        foreach (JObject child in children)
        {
            string datetime = (string)child["datetime"];
            double temp = (double)child["temp"];
            double humidity = (double)child["humidity"];
            double sunshine = (double)child["cloudcover"];
            weatherHistoryItems.Add(new WeatherHistory(cityName, datetime, humidity, temp, sunshine));
        }
        return weatherHistoryItems;
    }

    private async Task<List<WeatherHistory>> FetchFromApi(string city, string url)
    {
        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
            return await ConvertJsonToWeatherHistory(dict, city);
        }
        catch (HttpRequestException e)
        {
            // Handle any errors that occurred during the request
            var msg = $"City {city} didn't work Message :{e.Message} ";
            Console.WriteLine(msg);
            return null;
        }
    }
}