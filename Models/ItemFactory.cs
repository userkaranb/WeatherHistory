using Amazon.DynamoDBv2.Model;

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
}