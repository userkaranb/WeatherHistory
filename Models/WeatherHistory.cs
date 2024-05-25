namespace Jubilado;


[Serializable()]
public class WeatherHistory : Item
{
    public string CityName { set; get;} 
    public string Date {set; get;}
    public double Temperature { set; get;}
    public double Humidity { set; get;}
    public double Sunshine { set; get;}

    public WeatherHistory(string cityName, string date, double humidity, double temperature, double sunshine)
    {
        CityName = cityName;
        Date = date.ToString();
        Humidity = humidity;
        Temperature = temperature;
        Sunshine = 100.0 - sunshine;
    }
}
