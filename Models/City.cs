using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class City : Item
{

    private const string EMPTY_CITY_NAME = "EMPTY";
    public string CityName {private set; get;}

    public CityStatWrapper? CityStats { set; get;}

    public City(string cityName, CityStatWrapper? cityStats = null)
    {
        CityName = cityName.ToUpper().Replace(" ", "-");
        CityStats = cityStats;
    }

    public static City EmptyCity()
    {
        return new City(EMPTY_CITY_NAME, null);
    }

    public static bool IsCityEmpty(City city)
    {
        return city.CityName == EMPTY_CITY_NAME;
    }

}
