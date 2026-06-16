namespace HikingLog.Infrastructure.Data;

using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;

/// <summary>Provides static seed data for the HikingLog database.</summary>
public static class SeedData
{
    /// <summary>Returns the routes with their stages for initial seeding.</summary>
    /// <returns>A list of routes, each with their stages pre-populated.</returns>
    public static IReadOnlyList<Route> GetRoutes() =>
    [
        new Route
        {
            Name        = "Pieterpad",
            Code        = "LAW 9",
            Country     = "Nederland",
            TotalDistanceKm = 497m,
            Description = "De bekendste langeafstandswandelroute van Nederland, van Pieterburen in Groningen tot de Sint-Pietersberg bij Maastricht.",
            Stages =
            [
                // Groningen / Drenthe — vlak terrein
                new Stage { Number =  1, Name = "Pieterburen – Winsum",              StartPoint = "Pieterburen",           EndPoint = "Winsum",               DistanceKm = 12.0m, ElevationDifferenceM =  15m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  2, Name = "Winsum – Groningen",                StartPoint = "Winsum",                EndPoint = "Groningen",            DistanceKm = 22.0m, ElevationDifferenceM =  20m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  3, Name = "Groningen – Zuidlaren",             StartPoint = "Groningen",             EndPoint = "Zuidlaren",            DistanceKm = 21.0m, ElevationDifferenceM =  30m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  4, Name = "Zuidlaren – Rolde",                 StartPoint = "Zuidlaren",             EndPoint = "Rolde",                DistanceKm = 17.0m, ElevationDifferenceM =  40m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  5, Name = "Rolde – Schoonloo",                 StartPoint = "Rolde",                 EndPoint = "Schoonloo",            DistanceKm = 18.0m, ElevationDifferenceM =  35m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  6, Name = "Schoonloo – Sleen",                 StartPoint = "Schoonloo",             EndPoint = "Sleen",                DistanceKm = 24.0m, ElevationDifferenceM =  45m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  7, Name = "Sleen – Coevorden",                 StartPoint = "Sleen",                 EndPoint = "Coevorden",            DistanceKm = 21.0m, ElevationDifferenceM =  30m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  8, Name = "Coevorden – Hardenberg",            StartPoint = "Coevorden",             EndPoint = "Hardenberg",           DistanceKm = 19.0m, ElevationDifferenceM =  25m, Difficulty = Difficulty.Easy     },
                // Overijssel
                new Stage { Number =  9, Name = "Hardenberg – Ommen",               StartPoint = "Hardenberg",            EndPoint = "Ommen",                DistanceKm = 21.0m, ElevationDifferenceM =  40m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 10, Name = "Ommen – Hellendoorn",              StartPoint = "Ommen",                 EndPoint = "Hellendoorn",          DistanceKm = 21.0m, ElevationDifferenceM =  90m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 11, Name = "Hellendoorn – Holten",             StartPoint = "Hellendoorn",           EndPoint = "Holten",               DistanceKm = 16.0m, ElevationDifferenceM = 120m, Difficulty = Difficulty.Moderate },
                // Achterhoek
                new Stage { Number = 12, Name = "Holten – Laren",                   StartPoint = "Holten",                EndPoint = "Laren",                DistanceKm = 15.0m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 13, Name = "Laren – Vorden",                   StartPoint = "Laren",                 EndPoint = "Vorden",               DistanceKm = 14.0m, ElevationDifferenceM =  60m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 14, Name = "Vorden – Zelhem",                  StartPoint = "Vorden",                EndPoint = "Zelhem",               DistanceKm = 17.0m, ElevationDifferenceM =  70m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 15, Name = "Zelhem – Braamt",                  StartPoint = "Zelhem",                EndPoint = "Braamt",               DistanceKm = 17.0m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 16, Name = "Braamt – Millingen aan de Rijn",   StartPoint = "Braamt",                EndPoint = "Millingen aan de Rijn", DistanceKm = 25.0m, ElevationDifferenceM =  50m, Difficulty = Difficulty.Moderate },
                // Limburg
                new Stage { Number = 17, Name = "Millingen aan de Rijn – Groesbeek", StartPoint = "Millingen aan de Rijn", EndPoint = "Groesbeek",           DistanceKm = 20.0m, ElevationDifferenceM = 110m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 18, Name = "Groesbeek – Gennep",              StartPoint = "Groesbeek",             EndPoint = "Gennep",               DistanceKm = 15.0m, ElevationDifferenceM =  70m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 19, Name = "Gennep – Vierlingsbeek",           StartPoint = "Gennep",                EndPoint = "Vierlingsbeek",        DistanceKm = 19.0m, ElevationDifferenceM =  60m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 20, Name = "Vierlingsbeek – Swolgen",          StartPoint = "Vierlingsbeek",         EndPoint = "Swolgen",              DistanceKm = 24.0m, ElevationDifferenceM =  55m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 21, Name = "Swolgen – Venlo",                  StartPoint = "Swolgen",               EndPoint = "Venlo",                DistanceKm = 21.0m, ElevationDifferenceM =  45m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 22, Name = "Venlo – Swalmen",                  StartPoint = "Venlo",                 EndPoint = "Swalmen",              DistanceKm = 23.0m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 23, Name = "Swalmen – Montfort",               StartPoint = "Swalmen",               EndPoint = "Montfort",             DistanceKm = 21.0m, ElevationDifferenceM = 120m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 24, Name = "Montfort – Sittard",               StartPoint = "Montfort",              EndPoint = "Sittard",              DistanceKm = 24.0m, ElevationDifferenceM = 150m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 25, Name = "Sittard – Strabeek",               StartPoint = "Sittard",               EndPoint = "Strabeek",             DistanceKm = 22.0m, ElevationDifferenceM = 200m, Difficulty = Difficulty.Hard     },
                new Stage { Number = 26, Name = "Strabeek – Sint-Pietersberg",      StartPoint = "Strabeek",              EndPoint = "Sint-Pietersberg",     DistanceKm = 17.0m, ElevationDifferenceM = 250m, Difficulty = Difficulty.Hard     },
            ]
        },

        new Route
        {
            Name        = "Trekvogelpad",
            Code        = "LAW 2",
            Country     = "Nederland",
            TotalDistanceKm = 414m,
            Description = "Het langste natuurpad van Nederland, van Bergen aan Zee aan de Noordzee dwars door Midden-Nederland naar Enschede in Twente.",
            Stages =
            [
                // Kustduinen / Noord-Holland
                new Stage { Number =  1, Name = "Bergen aan Zee – Alkmaar",          StartPoint = "Bergen aan Zee",    EndPoint = "Alkmaar",            DistanceKm = 22.0m, ElevationDifferenceM =  65m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  2, Name = "Alkmaar – Graft",                   StartPoint = "Alkmaar",           EndPoint = "Graft",              DistanceKm = 20.0m, ElevationDifferenceM =  15m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  3, Name = "Graft – Wormer",                    StartPoint = "Graft",             EndPoint = "Wormer",             DistanceKm = 15.0m, ElevationDifferenceM =  10m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  4, Name = "Wormer – Den Ilp",                  StartPoint = "Wormer",            EndPoint = "Den Ilp",            DistanceKm = 17.0m, ElevationDifferenceM =  10m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  5, Name = "Den Ilp – Broek in Waterland",      StartPoint = "Den Ilp",           EndPoint = "Broek in Waterland", DistanceKm = 17.0m, ElevationDifferenceM =   8m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  6, Name = "Broek in Waterland – Schellingwoude", StartPoint = "Broek in Waterland", EndPoint = "Schellingwoude",  DistanceKm = 15.0m, ElevationDifferenceM =   5m, Difficulty = Difficulty.Easy     },
                new Stage { Number =  7, Name = "Schellingwoude – Weesp",            StartPoint = "Schellingwoude",    EndPoint = "Weesp",              DistanceKm = 24.0m, ElevationDifferenceM =  10m, Difficulty = Difficulty.Moderate },
                // Gooi / Utrechtse Heuvelrug
                new Stage { Number =  8, Name = "Weesp – Bussum",                   StartPoint = "Weesp",             EndPoint = "Bussum",             DistanceKm = 17.1m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Moderate },
                new Stage { Number =  9, Name = "Bussum – Baarn",                    StartPoint = "Bussum",            EndPoint = "Baarn",              DistanceKm = 15.0m, ElevationDifferenceM =  90m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 10, Name = "Baarn – Soesterberg",               StartPoint = "Baarn",             EndPoint = "Soesterberg",        DistanceKm = 20.0m, ElevationDifferenceM = 110m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 11, Name = "Soesterberg – Maarn",               StartPoint = "Soesterberg",       EndPoint = "Maarn",              DistanceKm = 18.0m, ElevationDifferenceM = 130m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 12, Name = "Maarn – Langbroek",                 StartPoint = "Maarn",             EndPoint = "Langbroek",          DistanceKm = 11.2m, ElevationDifferenceM =  70m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 13, Name = "Langbroek – Amerongen",             StartPoint = "Langbroek",         EndPoint = "Amerongen",          DistanceKm = 16.8m, ElevationDifferenceM =  85m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 14, Name = "Amerongen – Rhenen",                StartPoint = "Amerongen",         EndPoint = "Rhenen",             DistanceKm = 16.0m, ElevationDifferenceM = 100m, Difficulty = Difficulty.Moderate },
                // Veluwe
                new Stage { Number = 15, Name = "Rhenen – Ede",                      StartPoint = "Rhenen",            EndPoint = "Ede",                DistanceKm = 16.2m, ElevationDifferenceM = 120m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 16, Name = "Ede – Otterlo",                     StartPoint = "Ede",               EndPoint = "Otterlo",            DistanceKm = 20.7m, ElevationDifferenceM = 160m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 17, Name = "Otterlo – Hoenderloo",              StartPoint = "Otterlo",           EndPoint = "Hoenderloo",         DistanceKm = 15.0m, ElevationDifferenceM = 180m, Difficulty = Difficulty.Hard     },
                new Stage { Number = 18, Name = "Hoenderloo – Loenen",               StartPoint = "Hoenderloo",        EndPoint = "Loenen",             DistanceKm = 20.0m, ElevationDifferenceM = 150m, Difficulty = Difficulty.Moderate },
                // Achterhoek / Twente
                new Stage { Number = 19, Name = "Loenen – Brummen",                  StartPoint = "Loenen",            EndPoint = "Brummen",            DistanceKm = 13.0m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 20, Name = "Brummen – Vorden",                  StartPoint = "Brummen",           EndPoint = "Vorden",             DistanceKm = 19.0m, ElevationDifferenceM = 100m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 21, Name = "Vorden – Ruurlo",                   StartPoint = "Vorden",            EndPoint = "Ruurlo",             DistanceKm = 11.0m, ElevationDifferenceM =  60m, Difficulty = Difficulty.Easy     },
                new Stage { Number = 22, Name = "Ruurlo – Eibergen",                 StartPoint = "Ruurlo",            EndPoint = "Eibergen",           DistanceKm = 22.0m, ElevationDifferenceM =  90m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 23, Name = "Eibergen – Koekoeksbrug",           StartPoint = "Eibergen",          EndPoint = "Koekoeksbrug",       DistanceKm = 21.5m, ElevationDifferenceM =  80m, Difficulty = Difficulty.Moderate },
                new Stage { Number = 24, Name = "Koekoeksbrug – Enschede",           StartPoint = "Koekoeksbrug",      EndPoint = "Enschede",           DistanceKm = 17.0m, ElevationDifferenceM =  70m, Difficulty = Difficulty.Easy     },
            ]
        },
    ];
}
