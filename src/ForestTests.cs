using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class ForestTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        City New()
        {
            var c = new City(false) { Money = 100_000_000, AppearanceSeed = 42 };
            while (c.Month < 469) c.Tick();
            return c;
        }
        var city = New(); long money = city.Money;
        check(city.IsUnlocked(TileKind.Forest) && City.Footprint(TileKind.Forest) == 1 && City.MaxLevel(TileKind.Forest) == 1,
            "Forest is a one-cell, fixed-level tool available in year forty");
        city.Build(8, 8, TileKind.Forest);
        check(city[8, 8].Kind == TileKind.Forest && city[9, 8].Kind == TileKind.Empty && city.Money == money - City.Cost(TileKind.Forest),
            "Forest construction occupies and charges exactly one cell");
        city.Build(9, 8, TileKind.Home);
        check(city[9, 8].Comfort == 15 && city.LocalComfort(8, 11) == 15 && city.LocalComfort(10, 10) == 0,
            "Forest gives fifteen comfort within Manhattan distance three without road access");
        var park = New(); park.Build(0, 12, TileKind.Road); park.Build(1, 12, TileKind.Park);
        check(city.LocalComfort(8, 11) > park.LocalComfort(2, 12), "Forest comfort exceeds a fully connected ordinary park");
        city.Build(8, 9, TileKind.Forest);
        check(city[9, 8].Comfort == 25, "Dense forests respect the existing maximum comfort of twenty-five");
        city.Build(8, 8, TileKind.Bulldoze);
        check(city[9, 8].Comfort == 15, "Demolishing a forest immediately recalculates nearby residential comfort");
        money = city.Money; city.Build(8, 9, TileKind.Forest);
        check(city.Money == money && !city.CanBuild(8, 9, TileKind.Forest) && !city.CanBuild(24, 8, TileKind.Forest),
            "Occupied and unowned cells reject forest placement without charging");
        city.Money = City.Cost(TileKind.Forest) - 1; city.Build(10, 8, TileKind.Forest);
        check(city[10, 8].Kind == TileKind.Empty, "Insufficient funds reject forest construction");
        city = New(); city.Build(8, 8, TileKind.Forest); city.Tick();
        check(city.LastExpense == 18 && city.Jobs == 0 && city.Capacity == 0 && city[8, 8].Growth == 0,
            "Forest maintenance is eighteen per month and adds neither housing nor jobs");
        city = New(); city.Build(0, 12, TileKind.Road); city.Build(1, 12, TileKind.Park);
        city.Build(2, 12, TileKind.Forest); city.Build(3, 12, TileKind.Park);
        check(city[1, 12].Connected && !city[3, 12].Connected && !City.IsPark(TileKind.Forest),
            "A forest does not carry park walkway access between park clusters");
        check(ParkRenderer.Layout(city, 1, 12).Connections == 8 && ParkRenderer.Layout(city, 3, 12).Connections == 0,
            "Park paths never extend toward forests");
        city.Build(1, 11, TileKind.Forest); city.Build(2, 11, TileKind.Road); city.Build(3, 11, TileKind.Home);
        check(!city[2, 11].Connected && !city[3, 11].Connected, "Forest cannot carry road access into another road or building");

        city = New(); var styles = new HashSet<int>(); bool varied = true;
        for (int z = 5; z < 13; z++) for (int x = 5; x < 13; x++)
        {
            city.Build(x, z, TileKind.Forest); var t = city[x, z]; styles.Add(t.ForestStyle);
            varied &= ForestAppearance.IsValid(t)
                && (city[x - 1, z].Kind != TileKind.Forest || city[x - 1, z].ForestStyle != t.ForestStyle)
                && (city[x, z - 1].Kind != TileKind.Forest || city[x, z - 1].ForestStyle != t.ForestStyle);
        }
        check(styles.Count == 16 && varied, "Dense eight-by-eight forests use all sixteen patterns and avoid matching adjacent styles");
        var original = city.Tiles.Select(t => (t.ForestStyle, t.ForestRotation)).ToArray();
        for (int month = 0; month < 24; month++) city.Tick();
        check(original.SequenceEqual(city.Tiles.Select(t => (t.ForestStyle, t.ForestRotation))),
            "Monthly simulation preserves every forest's chosen appearance");
        city.Save(path); var loaded = City.Load(path);
        check(original.SequenceEqual(loaded.Tiles.Select(t => (t.ForestStyle, t.ForestRotation))) && loaded.LocalComfort(8, 8) == 25,
            "All forest patterns, orientations and environmental effects survive save/load");
        city.Build(13, 8, TileKind.Forest); loaded.Build(13, 8, TileKind.Forest);
        check(city[13, 8].ForestStyle == loaded[13, 8].ForestStyle && city[13, 8].ForestRotation == loaded[13, 8].ForestRotation,
            "Future forest designs remain deterministic after loading");
        city = New(); city.Purchase(new(-1, 0)); city.Build(-1, 8, TileKind.Forest); city.Build(0, 8, TileKind.Forest);
        city.SetActiveChunks([]); city.Save(path); loaded = City.Load(path);
        check(loaded[-1, 8].Kind == TileKind.Forest && loaded[-1, 8].ForestStyle != loaded[0, 8].ForestStyle && loaded.LocalComfort(1, 8) == 25,
            "Forest variety and effects cross negative chunk boundaries and cold storage");
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        var forest = json["Chunks"]![0]!["Cells"]![(8 * City.Size + 23).ToString()]!;
        foreach (var (field, invalid) in new[] { ("ForestStyle", -1), ("ForestStyle", 16), ("ForestRotation", -1), ("ForestRotation", 4) })
        {
            int previous = forest[field]!.GetValue<int>(); forest[field] = invalid; File.WriteAllText(path, json.ToJsonString());
            bool rejected = false; try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, $"Invalid forest metadata {field}={invalid} is rejected on load"); forest[field] = previous;
        }
        var legacy = New(); legacy.Build(5, 5, TileKind.Park); legacy.Save(path);
        json = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var chunk in json["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
        { cell.Value!.AsObject().Remove("ForestStyle"); cell.Value.AsObject().Remove("ForestRotation"); }
        File.WriteAllText(path, json.ToJsonString());
        check(City.Load(path)[5, 5].Kind == TileKind.Park, "Existing saves without forest fields still load");
    }
}
