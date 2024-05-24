
using Jubilado;
using Jubilado.Persistence; 
using System.Diagnostics;

public interface ICityGetterService
{
    Task<List<CityWeatherScore>> GetTopWeatherScoreCities();
}

public class CityGetterService : ICityGetterService
{
    private readonly IDataLayer _dataLayer;

    public CityGetterService(IDataLayer dataLayer)
    {
        _dataLayer = dataLayer;
    }
    public async Task<List<CityWeatherScore>> GetTopWeatherScoreCities()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var results = await _dataLayer.GetAllCityWeatherScoreCombos();
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        return results;
    }

}