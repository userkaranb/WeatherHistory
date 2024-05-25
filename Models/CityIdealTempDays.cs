using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityIdealTempDays : Item
{

    public string CityName {private set; get;}

    public int IdealTempDays { private set; get;}

    public CityIdealTempDays(string cityName, int idealTempDays)
    {
        CityName = cityName;
        IdealTempDays = idealTempDays;
    }

}
