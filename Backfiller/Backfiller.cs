using Jubilado;
using Jubilado.Persistence;

public interface IBackfiller
{
    Task BackfillCityCreation(List<string>? cityList);

    Task CreateGenericCityWeatherScoreSK();

    Task BackfillGSIForWeatherHistory();
}

public class Backfiller : IBackfiller
{
    const bool SHOULD_RUN = true;
    const bool SHOULD_DRY_RUN = false;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";
    private IDataLayer _dataLayer;
    private ICityCreatorService _cityCreatorService;

    public Backfiller(ICityCreatorService cityCreatorService, IDataLayer dataLayer)
    {
        _dataLayer = dataLayer;
        _cityCreatorService = cityCreatorService;
    }

    public async Task BackfillGSIForWeatherHistory()
    {
        _dataLayer.BackfillGSIForWeatherHistory();
    }

    public async Task CreateGenericCityWeatherScoreSK()
    {
        // Go get all stats pertaining to the given key from the city object (need a datalayer function genericized)
        var scores = await _dataLayer.GetExistingWeatherScoreAttributeByScan<CityIdealTempDays>("CityName, IdealTempDays");
        _dataLayer.CreateSortableWeatherSortKey<CityIdealTempDays>(scores);
        // Genercized Create new SK

    }

    public async Task BackfillCityCreation(List<string> cityList)
    {
        var citiesList = cityList.Select(cityString => new City(cityString)).ToList();
        if (SHOULD_RUN)
        {
            await _cityCreatorService.CreateCity(citiesList);
        }
    }
}
