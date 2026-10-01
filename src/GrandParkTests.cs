using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class GrandParkTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        var legacy = new City(); legacy.Save(path);
        File.WriteAllText(path, File.ReadAllText(path).Replace(",\"ParkStyle\":0", "").Replace(",\"ParkRotation\":0", ""));
        check(City.Load(path).Population == legacy.Population, "Version 3 saves predating grand park fields remain compatible");
        var city = SelfTests.DevelopedCity(); city.Money = 20_000_000; city.AppearanceSeed = 101;
        long money = city.Money;
        city.Build(22, 4, TileKind.GrandPark);
        check(city.Money == money && city[22, 4].Kind == TileKind.Empty, "Grand park rejects a partially unowned footprint without spending");
        city.Build(10, 10, TileKind.Home); money = city.Money;
        city.Build(8, 8, TileKind.GrandPark);
        check(city.Money == money && city[8, 8].Kind == TileKind.Empty, "Grand park rejects occupied interior cells atomically");
        city.Build(10, 10, TileKind.Bulldoze);
        for (int x = 0; x <= 12; x++) city.Build(x, 12, TileKind.Road);
        money = city.Money; city.Build(8, 8, TileKind.GrandPark);
        check(city.Money == money - 5600 && city.Tiles.Count(t => t.Kind == TileKind.GrandPark) == 16
            && ReferenceEquals(city.BuildingAt(11, 11), city[8, 8]) && city[8, 8].Connected,
            "Grand park occupies 4x4 and connects through the far edge as one building");
        check(city.LocalComfort(16, 9) == 20 && city.LocalComfort(17, 9) == 0
            && city.LocalComfort(9, 16) == 20 && city.LocalComfort(3, 9) == 20 && city.LocalComfort(9, 3) == 20,
            "Grand park improves all four sides up to distance five, counted once");
        city.Build(13, 8, TileKind.SuperHome);
        check(city[13, 8].Comfort == 20, "Grand park improves super residential comfort");
        int style = city[8, 8].ParkStyle, rotation = city[8, 8].ParkRotation;
        city.Build(4, 8, TileKind.GrandPark);
        check(city[4, 8].ParkStyle != style, "Touching grand parks choose different designs");
        city.Build(12, 3, TileKind.GrandPark);
        check(city[12, 3].ParkStyle != style && city[12, 3].ParkStyle != city[4, 8].ParkStyle,
            "Nearby grand parks avoid existing designs before considering environmental affinity");
        for (int i = 0; i < 36; i++) city.Tick();
        check(city[8, 8].Level == 1 && city[8, 8].Growth == 0 && city[8, 8].ParkStyle == style && city[8, 8].ParkRotation == rotation,
            "Grand park level and appearance stay fixed over years and neighborhood edits");
        city.Save(path); var loaded = City.Load(path);
        check(loaded[8, 8].ParkStyle == style && loaded[8, 8].ParkRotation == rotation && loaded.BuildingAt(11, 11).Kind == TileKind.GrandPark,
            "Grand park footprint, design and orientation survive saving");
        loaded.Build(0, 12, TileKind.Bulldoze);
        check(loaded.LocalComfort(16, 9) == 12, "Two disconnected grand parks each provide thirty percent comfort");
        money = loaded.Money; loaded.Build(11, 11, TileKind.Bulldoze);
        check(loaded.Money == money - 40 && Enumerable.Range(0, 16).All(i => loaded[8 + i % 4, 8 + i / 4].Kind == TileKind.Empty),
            "Demolishing a grand park interior removes all sixteen cells for one fee");

        var crossing = SelfTests.DevelopedCity(); crossing.Money = 20_000_000;
        crossing.Purchase(new(-1, 0)); crossing.Build(-2, 5, TileKind.GrandPark);
        crossing.Save(path); var restored = City.Load(path);
        check(restored.BuildingAt(1, 8).Kind == TileKind.GrandPark && restored[-2, 5].ParkStyle == crossing[-2, 5].ParkStyle,
            "Grand park spans negative chunk boundaries and compressed save data");
        restored.Tick();
        check(restored.LastExpense == 120, "Grand park maintenance is charged once per building");
        restored.Build(1, 8, TileKind.Bulldoze);
        check(!restored.Tiles.Any(t => t.Kind == TileKind.GrandPark), "Cross-chunk grand park demolition removes both chunks' cells");
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var chunk in json["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
            if (cell.Value!["OffsetX"]!.GetValue<int>() == 0 && cell.Value["OffsetZ"]!.GetValue<int>() == 0) cell.Value["ParkStyle"] = 16;
        File.WriteAllText(path, json.ToJsonString());
        bool invalid = false; try { City.Load(path); } catch (InvalidDataException) { invalid = true; }
        check(invalid, "Invalid saved grand park appearance is rejected");

        var styles = new HashSet<int>();
        for (int seed = 1; seed <= 256; seed++)
        {
            var sample = SelfTests.DevelopedCity(); sample.AppearanceSeed = seed; sample.Build(8, 8, TileKind.GrandPark);
            styles.Add(sample[8, 8].ParkStyle);
        }
        check(styles.Count == 16, "All sixteen grand park designs can be selected");
        var water = SelfTests.DevelopedCity(); water.Money = 100000; water.AppearanceSeed = 7;
        for (int z = 6; z <= 13; z++) for (int x = 6; x <= 13; x++)
            if (x < 8 || x > 11 || z < 8 || z > 11) water.Build(x, z, TileKind.Water);
        water.Build(8, 8, TileKind.GrandPark);
        check(water[8, 8].ParkStyle is 1 or 7 or 13, "Waterfront surroundings favor lake, Japanese or wetland park designs");
        var road = SelfTests.DevelopedCity(); road.Build(12, 9, TileKind.Road); road.Build(8, 8, TileKind.GrandPark);
        check(road[8, 8].ParkRotation == 1, "Grand park entrance orientation favors the road-facing side");
    }
}
