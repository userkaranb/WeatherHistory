using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using NodaTime;

public static class Backfiller
{
    const bool SHOULD_RUN = true;
    const bool SHOULD_DRY_RUN = false;
    const string KEY = "YLQ4H9DL7KCFMPEA6PDFU2W59";
    const string URI = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";


    public static void Execute()
    {
        if(SHOULD_RUN)
        {
            var citiesList = SHOULD_DRY_RUN ? GetDryRunCitiesOfInterest() : GetCitiesOfInterest();
            var (start, end) = GetDateRanges();
            foreach(var city in citiesList)
            {
                var url = GetFormattedUrl(city, start, end);
                Console.WriteLine(url);
            }
            Console.WriteLine("Executing.");
        }
    }

    public static List<string> GetDryRunCitiesOfInterest()
    {
        return new List<string>()
        {
            "Male"
        };
    }

    public static Tuple<string, string> GetDateRanges()
    {
        return new Tuple<string, string>(new DateOnly(2023, 1, 1).ToString("yyyy-MM-dd"), 
        new DateOnly(2023, 1, 5).ToString("yyyy-MM-dd"));
    }

    public static string GetFormattedUrl(string cityName, string startDate, string endDate)
    {
        var formattedCity = cityName.Replace(" ", "%20");
        return $"{URI}{formattedCity}/{startDate.ToString()}/{endDate.ToString()}?key={KEY}";
    }
    
    public static List<string> GetCitiesOfInterest()
    {
        return new List<string>()
        {
            "Buenos Aires",
            "New York City",
            "San Diego",
            "San Juan",
            "Montreal",
            "Toronto",
            "Santiago",
            "Mexico City",
            "Puerto Vallerta",
            "Oaxaca",
            "Montevideo",
            "Tenerife",
            "Bogota",
            "Lima",
            "Cusco",
            "Madrid",
            "Barcelona",
            "Palma de Mallorca",
            "La Paz",
            "Rio De Janeiro",
            "Sao Paulo",
            "Praia",
            "Marrekech",
            "Malaga",
            "Sevilla",
            "Lisbon",
            "Stockholm",
            "Copenhagen",
            "Helsinki",
            "Berlin",
            "Dubai",
            "Kuwait City",
            "Bangkok",
            "Quito",
            "Bali",
            "Tokyo",
            "Mykonos",
            "Athens",
            "Valencia",
            "San Sebastian",
            "Panama City",
            "Cozumel",
            "San Miguel de Allende",
            "Porto",
            "Mendoza",
            "San Jose",
            "Cartagena"
        };
    }


}