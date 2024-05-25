using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Net.Http;
using System.Threading.Tasks;
using NodaTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.DynamoDBv2.Model.Internal.MarshallTransformations;
using Amazon.DynamoDBv2.Model;
using System.ComponentModel;
using Jubilado;
using Jubilado.Persistence;

public interface IBackfiller
{
    Task BackfillCityCreation(List<string>? cityList);

    Task CreateWeatherScorePK();
    Task CreateIdealTempAndSunDaysSk();
}

public class Backfiller : IBackfiller
{
    const bool SHOULD_RUN = true;
    const bool SHOULD_DRY_RUN = false;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";
    private IDataLayer _dataLayer;
    private ICityCreatorService _cityCreatorService;

    public Backfiller(IDataLayer dataLayer, ICityCreatorService cityCreatorService)
    {
        _dataLayer = dataLayer;
        _cityCreatorService = cityCreatorService;
    }


    private async Task CreateGenericCityWeatherScoreSK()
    {
        // Go get all stats pertaining to the given key from the city object (need a datalayer function genericized)
        // Optional Delete existing SK, also generecized (parameterized)
        // Genercized Create new SK

    }
    public async Task CreateWeatherScorePK()
    {
        var combos = await _dataLayer.GetExistingWeatherScoreAttribute<CityWeatherScore>("CityName, WeatherScore");
        await DeleteExistingWeatherScorePK(combos.Select(x => x.CityName).ToList());
        _dataLayer.CreateWeatherScoreKey(combos);
    }

    public async Task CreateIdealTempAndSunDaysSk()
    {
        // get all ideal sun days and temp days combos
        var combos = await _dataLayer.GetExistingWeatherScoreAttribute<CityIdealSunDays>("CityName, IdealSunDays");
        _dataLayer.CreateIdealSunDaysScoreKey(combos);
        // create an endpoint to read from it
        // clean up the datalayer
    }

    private async Task DeleteExistingWeatherScorePK(List<string> cityNames)
    {
        foreach(var cityName in cityNames)
        {
            _dataLayer.DeleteWeatherSortKey(cityName);
        }
    }

    public async Task BackfillCityCreation(List<string> cityList)
    {
        var citiesList = cityList.Select(cityString => new City(cityString)).ToList();
        if(SHOULD_RUN)
        {
            await _cityCreatorService.CreateCity(citiesList);
        }
    }
}