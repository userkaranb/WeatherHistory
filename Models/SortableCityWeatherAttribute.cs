using System.Reflection.Metadata;

public abstract class SortableCityWeatherAttribute
{
    public string CityName { get; set; }
    protected SortableCityWeatherAttribute(string cityName)
    {
        CityName = cityName;
    }

    public abstract string GetFormattedNumber();
    
}