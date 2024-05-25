using Jubilado;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public interface ICityWeatherHistoryApiCaller
{
    Task<List<WeatherHistory>> DownloadWeatherHistoryItems(List<City> citiesList);
}
public class CityWeatherHistoryApiCaller : ICityWeatherHistoryApiCaller
{
    private readonly HttpClient _client;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";

    public CityWeatherHistoryApiCaller()
    {
        _client = new HttpClient();
    }
   
   #region PublicFunctions
    public async Task<List<WeatherHistory>> DownloadWeatherHistoryItems(List<City> citiesList)
    {
        List<WeatherHistory> allHistoryItems = new List<WeatherHistory>();
        var (start, end) = GetDateRanges();
        foreach(var city in citiesList)
        {
            var url = GetFormattedUrl(city.CityName, start, end);
            var historyItems = await FetchFromApi(city.CityName, url);
            allHistoryItems.AddRange(historyItems);
        }
        return allHistoryItems;
    }
    
    #endregion
   #region Privates
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

    private static string GetFormattedUrl(string cityName, string startDate, string endDate)
    {
        var formattedCity = cityName.Replace(" ", "%20");
        return $"{URI}{formattedCity}/{startDate.ToString()}/{endDate.ToString()}?key={KEY}";
    }

    private static Tuple<string, string> GetDateRanges()
    {
        return new Tuple<string, string>(new DateOnly(2023, 1, 1).ToString("yyyy-MM-dd"), 
        new DateOnly(2023, 12, 31).ToString("yyyy-MM-dd"));
    }
    #endregion
}