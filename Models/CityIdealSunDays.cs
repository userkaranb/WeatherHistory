using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityIdealSunDays : Item
{

    public string CityName {private set; get;}

    public int IdealSunDays { private set; get;}

    public CityIdealSunDays(string cityName, int idealSunDays)
    {
        CityName = cityName;
        IdealSunDays = idealSunDays;
    }

}
