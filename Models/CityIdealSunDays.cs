using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityIdealSunDays : SortableCityWeatherAttribute
{

    public int IdealSunDays { private set; get;}

    public CityIdealSunDays(string cityName, int idealSunDays) : base(cityName)
    {
        IdealSunDays = idealSunDays;
    }

    public override string GetFormattedNumber()
    {
        return IdealSunDays.ToString("D4");
    }

}
