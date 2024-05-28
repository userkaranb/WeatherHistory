using System.Reflection.Metadata;

public static class DataStringConstants
{
    public const string PK = "PK";
    public const string SK = "SK";
    public static class SortableCityStatsSKValues
    {
        public const string IdealSunDays = "IDEALSUNDAYS";
        public const string WeatherScore = "WEATHERSCORE";
        public const string IdealTempDays = "IDEALTEMPDAYS";
        public const string CityWeatherScorePK = "CITY-WEATHER-SCORE";
    }

    public static class CityDataObject
    {
        public const string CityName = "CityName";
        public const string WeatherScore = "WeatherScore";
        public const string TempScore = "TempScore";
        public const string SunScore = "SunScore";
        public const string HumScore = "HumScore";
        public const string IdealTempDays = "IdealTempDays";
        public const string IdealSunDays = "IdealSunDays";
        public const string WeatherHistoryKey = "WEATHERHISTORY";
        public const string CityKey = "CITY";
    }
    public static class WeatherHistoryDataObject
    {
        public const string Humidity = "Humidity";
        public const string Sunshine = "Sunshine";
        public const string Temperature = "Temperature";
        public const string Date = "Date";
    }

}
