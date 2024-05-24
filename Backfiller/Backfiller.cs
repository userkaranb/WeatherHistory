using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Net.Http;
using System.Threading.Tasks;
using NodaTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.DynamoDBv2.Model.Internal.MarshallTransformations;
using Amazon.DynamoDBv2.Model;
using System.ComponentModel;
using Jubilado;
using Jubilado.Persistence;

public interface IBackfiller
{
    Task Execute(List<string>? cityList);

    Task CreateWeatherScorePK();
}

public class Backfiller : IBackfiller
{
    const bool SHOULD_RUN = true;
    const bool SHOULD_DRY_RUN = false;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";
    private IDataLayer _dataLayer;
    private ICityCreatorService _cityCreatorService;

    public Backfiller(IDataLayer dataLayer, ICityCreatorService cityCreatorService)
    {
        _dataLayer = dataLayer;
        _cityCreatorService = cityCreatorService;
    }

    public async Task CreateWeatherScorePK()
    {
        // Get all city names and weather scores
        // call data layer to insert a bunch of items (new keys)
        // update the CreateCity write to also write the new key in a transaction.
        // Create the getters. One that uses the new key, the other that does the scan and compares them.
        var combos = await _dataLayer.GetAllCityWeatherScoreCombos();
        combos.ForEach(x => Console.WriteLine(x.ToString()));
    }

    public async Task Execute(List<string>? cityList = null)
    {

        var citiesList = cityList == null ? 
        (SHOULD_DRY_RUN ? GetDryRunCitiesOfInterest() : CityListHelper.GetCitiesOfInterest()) : 
        cityList.Select(cityString => new City(cityString)).ToList();
        if(SHOULD_RUN)
        {
            await _cityCreatorService.CreateCity(citiesList);
        }
    }

    private async Task FetchFromApi(HttpClient client, string city, string url)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
            var converted = await ConvertJsonToWeatherHistory(dict, city);
            foreach(var historyItem in converted)
            {
                _dataLayer.CreateWeatherHistoryItem(historyItem);
                // Console.WriteLine($"Item: {historyItem.CityName}-{historyItem.Date}-{historyItem.Humidity}-{historyItem.Sunshine}-{historyItem.Temperature}");
            }
        }
        catch (HttpRequestException e)
        {
            // Handle any errors that occurred during the request
            Console.WriteLine("City {0} didn't work Message :{1} ", city, e.Message);
        }
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

    public static List<City> GetDryRunCitiesOfInterest()
    {
        return new List<City>()
        {
            new City("Buenos Aires")
        };
    }

    public static Tuple<string, string> GetDateRanges()
    {
        return new Tuple<string, string>(new DateOnly(2023, 1, 1).ToString("yyyy-MM-dd"), 
        new DateOnly(2023, 12, 31).ToString("yyyy-MM-dd"));
    }

    public static string GetFormattedUrl(string cityName, string startDate, string endDate)
    {
        var formattedCity = cityName.Replace(" ", "%20");
        return $"{URI}{formattedCity}/{startDate.ToString()}/{endDate.ToString()}?key={KEY}";
    }
}