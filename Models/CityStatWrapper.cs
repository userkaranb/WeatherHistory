public class CityStatWrapper
{
    public float TemperatureScore {private set; get;}
    public float SunshineScore {private set; get;}
    public float HumidityScore {private set; get;}
    public float WeatherScore {private set; get;}
    public int IdealTempRangeDays {private set; get;}
    public int IdealSunshineDays {private set; get;}
    public CityStatWrapper(float tempeScore, float sunScore, float humScore, float weatherScore, int idealTempDays, int idealSunDays)
    {
        TemperatureScore = tempeScore;
        SunshineScore = sunScore;
        HumidityScore = humScore;
        WeatherScore = weatherScore;
        IdealTempRangeDays = idealTempDays;
        IdealSunshineDays = idealSunDays;

    }
}