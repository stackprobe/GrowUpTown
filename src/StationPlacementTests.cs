using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class StationPlacementTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        City New() { var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; return c; }
        void Edge(City c, int x, int z, int side, TileKind kind)
        {
            for (int i = kind == TileKind.Rail ? -1 : 0; i < (kind == TileKind.Rail ? 3 : 2); i++)
            {
                var p = City.StationSideCell(x, z, side, i); c.Build(p.X, p.Z, kind);
            }
        }
        var city = New();
        long money = city.Money;
        check(!city.CanBuild(8, 8, TileKind.Station) && city.Build(8, 8, TileKind.Station).Contains("線路が2マス")
            && city.Money == money && city[8, 8].Kind == TileKind.Empty,
            "Station without track is rejected without charging or occupying land");
        city.Build(8, 10, TileKind.Rail);
        check(!city.CanBuild(8, 8, TileKind.Station), "One adjacent track cell is insufficient for a station");
        city.Build(7, 8, TileKind.Rail);
        check(!city.CanBuild(8, 8, TileKind.Station), "Two track cells on different sides do not satisfy the platform requirement");
        city.Build(7, 7, TileKind.Rail);
        check(!city.CanBuild(8, 8, TileKind.Station), "Diagonal track does not count toward the platform edge");
        city.Build(9, 10, TileKind.Rail);
        check(!city.CanBuild(8, 8, TileKind.Station), "Two dead-end track cells do not allow station placement");
        city.Build(7, 10, TileKind.Rail); city.Build(10, 10, TileKind.Rail);
        check(city.CanBuild(8, 8, TileKind.Station), "Two aligned straight track cells allow station placement");
        city.Build(9, 9, TileKind.Home);
        money = city.Money;
        city.Build(8, 8, TileKind.Station);
        check(city.Money == money && city[8, 8].Kind == TileKind.Empty && city[9, 9].Kind == TileKind.Home,
            "Valid track does not bypass footprint occupancy checks");

        for (int platform = 0; platform < 4; platform++)
        {
            // Test both platform cells, all directions and every non-straight
            // rail shape, including curves, turnouts, crossings and terminals.
            for (int cell = 0; cell < 2; cell++) for (int railMask = 0; railMask < 16; railMask++)
            {
                city = New(); Edge(city, 8, 8, platform, TileKind.Rail);
                var p = City.StationSideCell(8, 8, platform, cell);
                (int X, int Z)[] arms = [(0, 1), (1, 0), (0, -1), (-1, 0)];
                for (int side = 0; side < 4; side++)
                {
                    var q = (X: p.X + arms[side].X, Z: p.Z + arms[side].Z);
                    if ((railMask & (1 << side)) != 0) city.Build(q.X, q.Z, TileKind.Rail);
                    else if (City.IsRail(city[q.X, q.Z])) city.Build(q.X, q.Z, TileKind.Bulldoze);
                }
                check(city.StationTrackSides(8, 8) == (railMask == (platform % 2 == 0 ? 10 : 5) ? 1 << platform : 0),
                    $"Platform side {platform}, cell {cell}, mask {railMask}: both rails must be straight along the platform");
            }
        }
        for (int platform = 0; platform < 4; platform++) for (int entrance = 0; entrance < 4; entrance++)
        {
            city = New();
            Edge(city, 8, 8, platform, TileKind.Rail);
            Edge(city, 8, 8, entrance, TileKind.Road);
            check(city.CanBuild(8, 8, TileKind.Station), $"Platform side {platform}, road side {entrance}: placement is allowed, including crossings");
            city.Build(8, 8, TileKind.Station);
            check(city[8, 8].StationRotation == platform && city[8, 8].StationEntranceSide == entrance,
                $"Platform side {platform}, road side {entrance}: platform and entrance are selected independently");
            city.Save(path); city = City.Load(path);
            check(city[8, 8].StationRotation == platform && city[8, 8].StationEntranceSide == entrance,
                $"Platform side {platform}, road side {entrance}: orientation survives save/load");
        }
        city = New(); Edge(city, 8, 8, 1, TileKind.Rail);
        city.Build(8, 8, TileKind.Station);
        check(city[8, 8].StationRotation == 1 && city[8, 8].StationEntranceSide == 3,
            "Without roads, a station uses the entrance opposite the railway");
        int design = city[8, 8].StationStyle;
        Edge(city, 8, 8, 0, TileKind.Road);
        Edge(city, 8, 8, 1, TileKind.Bulldoze);
        city.Tick(); city.Save(path); city = City.Load(path);
        check(city[8, 8].StationStyle == design && city[8, 8].StationRotation == 1 && city[8, 8].StationEntranceSide == 3,
            "Existing station design and orientation remain fixed after surroundings change");
        var saved = JsonNode.Parse(File.ReadAllText(path))!;
        var tile = saved["Chunks"]![0]!["Cells"]![(8 * City.Size + 8).ToString()]!;
        tile.AsObject().Remove("StationEntranceSide");
        File.WriteAllText(path, saved.ToJsonString());
        check(City.Load(path)[8, 8].StationEntranceSide is null, "Older station saves without entrance metadata remain loadable");
        foreach (int invalid in new[] { -1, 4 })
        {
            tile["StationEntranceSide"] = invalid; File.WriteAllText(path, saved.ToJsonString());
            bool rejected = false;
            try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, $"Invalid entrance side {invalid} is rejected");
        }
        city = New();
        Edge(city, 8, 8, 0, TileKind.Rail); Edge(city, 8, 8, 1, TileKind.Rail);
        Edge(city, 8, 8, 2, TileKind.Road); Edge(city, 8, 8, 3, TileKind.Road);
        city.Build(8, 8, TileKind.Station);
        check(city[8, 8].StationRotation == 0 && city[8, 8].StationEntranceSide == 2,
            "Multiple railway and road sides deterministically prefer opposing platform and entrance");
        city = New(); Edge(city, 8, 8, 0, TileKind.Rail);
        Edge(city, 8, 8, 1, TileKind.Road); city.Build(8, 7, TileKind.Road);
        city.Build(8, 8, TileKind.Station);
        check(city[8, 8].StationEntranceSide == 2, "An opposite road is preferred even when a side road covers more cells");
        city = New();
        city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
        Edge(city, 23, 23, 1, TileKind.Rail); Edge(city, 23, 23, 2, TileKind.Road);
        city.Build(23, 23, TileKind.Station);
        city.SetActiveChunks([new(0, 0)]); city.Save(path); city = City.Load(path);
        check(city[23, 23].Kind == TileKind.Station && city[23, 23].StationRotation == 1
            && city[23, 23].StationEntranceSide == 2 && city.BuildingAt(24, 24).Kind == TileKind.Station,
            "Platform and entrance persist for stations spanning four chunks and cold storage");
    }
}
