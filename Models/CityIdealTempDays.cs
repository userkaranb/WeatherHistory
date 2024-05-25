using System.Text.Json.Serialization;

namespace Jubilado;

[Serializable()]
public class CityIdealTempDays : SortableCityWeatherAttribute
{

    public int IdealTempDays { private set; get;}

    public CityIdealTempDays(string cityName, int idealTempDays) : base(cityName)
    {
        IdealTempDays = idealTempDays;
    }
    public override string GetFormattedNumber()
    {
        return IdealTempDays.ToString("D4");
    }

}
