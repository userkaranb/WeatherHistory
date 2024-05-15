using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;

namespace Jubilado;

public class ItemFactory
{
    public static City ToCity(Dictionary<string, AttributeValue> values)
    {
        string cityName = "";
        float? weatherScore = null;
        foreach(var kvp in values)
        {
            switch(kvp.Key)
            {
                case "CityName":
                  cityName = values["CityName"].S;
                  break;
                case "WeatherScore":
                  weatherScore = values["WeatherScore"].S == "" ? null : float.Parse(values["WeatherScore"].S);
                  break;
                case null:
                    continue;
            }
        }
        return new City(cityName, weatherScore);
    }
    public static WeatherHistory ToWeatherHistory(Dictionary<string, AttributeValue> values)
    {
        string cityName = "";
        double humidity = -1;
        double temperature = -1;
        double sunshine = -1;
        string date = "";
        foreach(var kvp in values)
        {
            switch(kvp.Key)
            {
                case "CityName":
                  cityName = values["CityName"].S;
                  break;
                case "Humidity":
                  humidity = double.Parse(values["Humidity"].S);
                  break;
                case "Sunshine":
                  sunshine = double.Parse(values["Sunshine"].S);
                  break;
                case "Temperature":
                  temperature = double.Parse(values["Temperature"].S);
                  break;
                case "Date":
                  date = values["Date"].S;
                  break;
                case null:
                    continue;
            }
        }
        return new WeatherHistory(cityName, date, humidity, temperature, sunshine);
    }

}