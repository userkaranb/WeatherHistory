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

public static class Backfiller
{
    const bool SHOULD_RUN = false;
    const bool SHOULD_DRY_RUN = false;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";

    private static readonly DataLayer dataLayer = new DataLayer();

    public static async void Execute()
    {
        var citiesList = SHOULD_DRY_RUN ? GetDryRunCitiesOfInterest() : GetCitiesOfInterest();
        if(SHOULD_RUN)
        {
            var (start, end) = GetDateRanges();
            HttpClient client = new HttpClient();
            foreach(var city in citiesList)
            {
                var url = GetFormattedUrl(city, start, end);
                await FetchFromApi(client, city, url);
            }
            Console.WriteLine("Executing.");
        }
        foreach (var city in citiesList)
        {
            dataLayer.CreateCity(new City(city));
        }
    }

    private static async Task FetchFromApi(HttpClient client, string city, string url)
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
                dataLayer.CreateWeatherHistoryItem(historyItem);
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

    public static List<string> GetDryRunCitiesOfInterest()
    {
        return new List<string>()
        {
            "Buenos Aires"
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
    
    public static List<string> GetCitiesOfInterest()
    {
        return new List<string>()
        {
            "Buenos Aires",
            "New York City",
            "San Diego",
            "San Juan",
            "Montreal",
            "Toronto",
            "Santiago",
            "Mexico City",
            "Puerto Vallerta",
            "Oaxaca",
            "Montevideo",
            "Tenerife",
            "Bogota",
            "Lima",
            "Cusco",
            "Madrid",
            "Barcelona",
            "Palma de Mallorca",
            "La Paz",
            "Rio De Janeiro",
            "Sao Paulo",
            "Praia",
            "Marrekech",
            "Malaga",
            "Sevilla",
            "Lisbon",
            "Stockholm",
            "Copenhagen",
            "Helsinki",
            "Berlin",
            "Dubai",
            "Kuwait City",
            "Bangkok",
            "Quito",
            "Denpasar",
            "Tokyo",
            "Mykonos",
            "Athens",
            "Valencia",
            "San Sebastian",
            "Panama City",
            "Cozumel",
            "San Miguel de Allende",
            "Porto",
            "Mendoza",
            "San Jose",
            "Cartagena"
        };
    }


}