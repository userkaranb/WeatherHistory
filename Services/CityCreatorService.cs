using System.Data;
using Jubilado;
using Jubilado.Persistence;

public interface ICityCreatorService
{
    Task CreateCity(List<City> cities);
    Task DeleteCity(City city);
}

public class CityCreatorService : ICityCreatorService
{
    private IDataLayer _dataLayer;
    private ICityWeatherHistoryApiCaller _weatherHistoryApiCaller;
    private const float IDEAL_TEMP = 75f;
    private const float IDEAL_SUN = 100f;
    private const float IDEAL_HUM = 50f;
    
    public CityCreatorService(IDataLayer dataLayer, ICityWeatherHistoryApiCaller weatherHistoryApiCaller)
    {
        _dataLayer = dataLayer;
        _weatherHistoryApiCaller = weatherHistoryApiCaller;
    }
    
    public async Task CreateCity(List<City> cities)
    {
        // see whats already there
        var cityKeyToCityValue = cities.Select(x => (x, _dataLayer.GetCity(x))).ToDictionary<City, City>();
        var citiesThatDontHaveValues = cityKeyToCityValue.Where(x => City.IsCityEmpty(x.Value)).Select(x => x.Key).ToList();
        
        // download and persist weather history items for whatevers not there
        var downloadedWeatherHistoryItems = await _weatherHistoryApiCaller.DownloadWeatherHistoryItems(citiesThatDontHaveValues);
        _dataLayer.CreateWeatherHistoryItems(downloadedWeatherHistoryItems);
        
        //Amend Weather info to city object, and persist
        var cityToCityStatMappings = GetWeatherStatsForCities(citiesThatDontHaveValues);
        foreach(var city in cityToCityStatMappings.Keys)
        {
            city.CityStats = cityToCityStatMappings[city];
            _dataLayer.CreateCity(city);
        }
    }

    public async Task DeleteCity(City city)
    {
        _dataLayer.DeleteCity(city);
    }

    private Dictionary<City, CityStatWrapper> GetWeatherStatsForCities(List<City> cities)
    {
        Dictionary<City, CityStatWrapper> results = new Dictionary<City, CityStatWrapper>();
        foreach(var city in cities)
        {
            var cityName = city.CityName;
            var weatehrHistory = _dataLayer.GetWeatherHistoryForCity(cityName);
            var weatherStats = CalculateWeatherScore(cityName, weatehrHistory);
            results[city] = weatherStats;
        }
        return results;
    }

    private CityStatWrapper CalculateWeatherScore(string cityName, List<WeatherHistory> histories)
    {
        // for now, just calculate MAD score for temperature
        var temps = histories.Select(x => (float)x.Temperature).ToList();
        var sunshines = histories.Select(x => (float)x.Sunshine).ToList();
        
        var tempScore = CalculateIdealMetric(temps, IDEAL_TEMP);
        var sunshineScore = CalculateIdealMetric(sunshines, IDEAL_SUN);
        var humidityScore = CalculateIdealMetric(histories.Select(x => (float)x.Humidity).ToList(), IDEAL_HUM);
        var weatherScore = tempScore + sunshineScore + humidityScore;
        var idealTempDays = temps.Where(temp => temp > 68 && temp < 83);
        var idealSunshineDays = sunshines.Where(sun => sun > 50);
        
        return new CityStatWrapper(cityName, tempScore, sunshineScore, humidityScore, weatherScore, idealTempDays.Count(), idealSunshineDays.Count()); 
    }

    internal float CalculateIdealMetric(List<float> metrics, float ideal)
    {
        return (float)metrics.Select(metric => Math.Abs(ideal - metric)).Average();
    }

}