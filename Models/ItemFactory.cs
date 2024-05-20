using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;

namespace Jubilado;

public class ItemFactory
{
    public static City ToCity(Dictionary<string, AttributeValue> values)
    {
        if(!values.ContainsKey("CityName"))
        {
          return City.EmptyCity();
        }
        string cityName = "";
        float weatherScore = -1f;
        float tempScore = -1f;
        float sunScore = -1f;
        float humScore = -1f;
        int idealTempDays = -1;
        int idealSunDays = -1;
        foreach(var kvp in values)
        {
            switch(kvp.Key)
            {
                case "CityName":
                  cityName = values["CityName"].S;
                  break;
                case "WeatherScore":
                  weatherScore = float.Parse(values["WeatherScore"].S);
                  break;
                case "TempScore":
                  tempScore = float.Parse(values["TempScore"].S);
                  break;
                case "SunScore":
                  sunScore = float.Parse(values["SunScore"].S);
                  break;
                case "HumScore":
                  humScore = float.Parse(values["HumScore"].S);
                  break;
                case "IdealTempDays":
                  idealTempDays = int.Parse(values["IdealTempDays"].S);
                  break;
                case "IdealSunDays":
                  idealSunDays = int.Parse(values["IdealSunDays"].S);
                  break;
                case null:
                    continue;
            }
        }
        var wrapper = new CityStatWrapper(cityName, tempScore, 
        sunScore, humScore, weatherScore, idealTempDays, idealSunDays);
        return new City(cityName, wrapper);
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