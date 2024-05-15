namespace Jubilado;


[Serializable()]
public class WeatherHistory : Item
{
    public string CityName {private set; get;} 
    public string Date {private set; get;}
    public double Temperature {private set; get;}
    public double Humidity {private set; get;}
    public double Sunshine {private set; get;}

    public WeatherHistory(string cityName, string date, double humidity, double temperature, double sunshine)
    {
        CityName = cityName;
        Date = date.ToString();
        Humidity = humidity;
        Temperature = temperature;
        Sunshine = 100.0 - sunshine;
    }
}
