using Jubilado;

public static class CityListHelper
{
    public static List<City> GetCitiesOfInterest()
    {
        var cityListName = new List<string>()
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
            "Denpasar",
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
        return cityListName.Select(x => new City(x)).ToList();
    }
}
