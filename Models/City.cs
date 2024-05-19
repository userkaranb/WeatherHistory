using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class City : Item
{

    public string CityName {private set; get;}

    public CityStatWrapper? CityStats { set; get;}

    public City(string cityName, CityStatWrapper? cityStats = null)
    {
        CityName = cityName.ToUpper().Replace(" ", "-");
        CityStats = cityStats;
    }

}
