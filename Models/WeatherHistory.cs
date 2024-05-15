namespace Jubilado;


[Serializable()]
public class WeatherHistory : Item
{
    public string CityName {private set; get;} 
    public DateOnly Date {private set; get;}
    public float Temperature {private set; get;}
    public float Humidity {private set; get;}
    public float Sunshine {private set; get;}

    public WeatherHistory(string cityName, DateOnly date, float humidity, float temperature, float sunshine)
    {
        CityName = cityName;
        Date = date;
        Humidity = humidity;
        Temperature = temperature;
        Sunshine = sunshine;
    }

    public string GetWeatherHistoryDateString()
    {
        return Date.ToShortDateString();
    }
}
