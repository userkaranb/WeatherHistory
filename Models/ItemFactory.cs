using System.ComponentModel;
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
                  weatherScore = GetValue<float>(values["WeatherScore"]);
                  break;
                case "TempScore":
                  tempScore = GetValue<float>(values["TempScore"]);
                  break;
                case "SunScore":
                  sunScore = GetValue<float>(values["SunScore"]);
                  break;
                case "HumScore":
                  humScore = GetValue<float>(values["HumScore"]);
                  break;
                case "IdealTempDays":
                  idealTempDays = GetValue<int>(values["IdealTempDays"]);
                  break;
                case "IdealSunDays":
                  idealSunDays = GetValue<int>(values["IdealSunDays"]);
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
                  humidity = GetValue<double>(values["Humidity"]);
                  break;
                case "Sunshine":
                  sunshine = GetValue<double>(values["Sunshine"]);
                  break;
                case "Temperature":
                  temperature = GetValue<double>(values["Temperature"]);
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

    public static List<T> ToSortableCityScoreBulk<T>(List<Dictionary<string, AttributeValue>> values)
      where T : SortableCityWeatherAttribute
    {
      List<T> retVals = new List<T>();
      if (typeof(T) == typeof(CityWeatherScore))
      {
        foreach(var val in values)
        {
          retVals.Add(new CityWeatherScore(val["CityName"].S, GetSortableWeatherAttributeFromSortKey<float>(val)) as T);
        }
      }
      if (typeof(T) == typeof(CityIdealSunDays))
      {
        foreach(var val in values)
        {
          retVals.Add(new CityIdealSunDays(val["CityName"].S, GetSortableWeatherAttributeFromSortKey<int>(val)) as T);
        }
      }
      if (typeof(T) == typeof(CityIdealTempDays))
      {
        foreach(var val in values)
        {
          retVals.Add(new CityIdealTempDays(val["CityName"].S, GetSortableWeatherAttributeFromSortKey<int>(val)) as T);
        }
      }

      return retVals;

    }

    public static T1 ToSortableCityWeatherScoreItem<T1>(Dictionary<string, AttributeValue> values) 
      where T1 : SortableCityWeatherAttribute
    {
      var cityName = values["CityName"].S;
      if (typeof(T1) == typeof(CityWeatherScore))
      {
        return new CityWeatherScore(cityName, GetValue<float>(values["WeatherScore"])) as T1;
      }
      if (typeof(T1) == typeof(CityIdealSunDays))
      {
          return new CityIdealSunDays(cityName, GetValue<int>(values["IdealSunDays"])) as T1;
      }
      if (typeof(T1) == typeof(CityIdealTempDays))
      {
          return new CityIdealTempDays(cityName, GetValue<int>(values["IdealTempDays"])) as T1;
      }

      throw new InvalidOperationException("Unsupported type");
    }

  private static T GetValue<T>(AttributeValue val)
  {
    string stringValue = val.S ?? val.N; // Assuming either S or N is not null.
    return (T)Convert.ChangeType(stringValue, typeof(T));
  }

  private static T GetSortableWeatherAttributeFromSortKey<T>(Dictionary<string, AttributeValue> val)
  {
    string stringValue = val["SK"].S.Split("#")[1];
    return (T)Convert.ChangeType(stringValue, typeof(T));
  }

}