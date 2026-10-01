using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class SupermarketTests
{
    private static City Town()
    {
        var city = SelfTests.DevelopedCity(); city.Money = 20_000_000; city.AppearanceSeed = 707;
        for (int x = 0; x <= 20; x++) city.Build(x, 12, TileKind.Road);
        return city;
    }
    internal static void Run(Action<bool, string> check, string path)
    {
        var legacy = new City(); legacy.Save(path);
        File.WriteAllText(path, File.ReadAllText(path).Replace(",\"MarketStyle\":0", "").Replace(",\"MarketRotation\":0", "").Replace(",\"MarketAccess\":false", ""));
        check(City.Load(path).Population == 48, "Pre-supermarket Version 3 saves remain compatible");
        var city = Town(); long money = city.Money;
        city.Build(22, 8, TileKind.Supermarket);
        check(city.Money == money && city[22, 8].Kind == TileKind.Empty, "Supermarket rejects partially unowned land without charging");
        city.Build(10, 10, TileKind.Home); money = city.Money;
        city.Build(8, 8, TileKind.Supermarket);
        check(city.Money == money && city[8, 8].Kind == TileKind.Empty, "Supermarket interior overlap rejects the whole placement");
        city.Build(10, 10, TileKind.Bulldoze); city.Money = 7199;
        city.Build(8, 8, TileKind.Supermarket);
        check(city[8, 8].Kind == TileKind.Empty && city.Money == 7199, "Supermarket requires its full construction cost");
        city.Money = 20_000_000; money = city.Money; city.Build(8, 8, TileKind.Supermarket);
        check(city.Money == money - 7200 && city.Tiles.Count(t => t.Kind == TileKind.Supermarket) == 16
            && city[8, 8].Connected && ReferenceEquals(city.BuildingAt(11, 11), city[8, 8]),
            "Supermarket occupies sixteen cells and connects through its outer edge");
        check(city.LocalComfort(16, 9) == 10 && city.LocalComfort(17, 9) == 0 && city.LocalComfort(9, 16) == 10
            && city.LocalComfort(3, 9) == 10 && city.LocalComfort(9, 3) == 10 && city.LocalComfort(15, 14) == 0,
            "Supermarket comfort uses distance five on every side and excludes distant diagonals");
        city.Build(12, 11, TileKind.Shop); city.Build(7, 11, TileKind.Home);
        check(city[7, 11].Comfort == 10 && city[12, 11].MarketAccess && city.Jobs == 80
            && city.Chunks[new(0, 0)].Stats.BusinessIncome == 520, "Nearby housing improves and connected shop tax base rises by twenty percent");
        city.Tick();
        check(city.LastExpense == 42 + 160 + 8 + 5, "Supermarket maintenance is charged once");
        // Fill employment so actual tax receipts, rather than only cached bases, can be checked.
        for (int x = 1; x <= 6; x++)
        {
            city.Build(x, 11, TileKind.Home); city[x, 11].Level = 3; city[x, 11].Residents = 72;
            FacadeStyles.CompleteFloors(city, x, 11);
        }
        city.Reconnect(); city.Tick();
        check(city.LastIncome == city.Population * city.TaxRate / 2 + 520, "Supermarket commercial boost reaches monthly tax receipts");
        int style = city[8, 8].MarketStyle, rotation = city[8, 8].MarketRotation;
        for (int z = 6; z < 12; z++) city.Build(0, z, TileKind.Road);
        for (int x = 1; x <= 4; x++) city.Build(x, 6, TileKind.Road);
        city.Build(4, 7, TileKind.Supermarket); city.Build(12, 5, TileKind.Supermarket);
        check(city[4, 7].MarketStyle != style && city[12, 5].MarketStyle != style
            && city[4, 7].MarketStyle != city[12, 5].MarketStyle, "Touching and nearby supermarkets avoid each other's designs");
        check(city.LocalComfort(7, 11) == 10 && city.Chunks[new(0, 0)].Stats.BusinessIncome == 920,
            "Overlapping supermarket service does not stack residential or commercial bonuses");
        for (int i = 0; i < 36; i++) city.Tick();
        check(city[8, 8].Level == 1 && city[8, 8].Growth == 0 && city[8, 8].MarketStyle == style && city[8, 8].MarketRotation == rotation,
            "Supermarket level, design and orientation remain fixed over years and nearby construction");
        city.Save(path); var loaded = City.Load(path);
        check(loaded[8, 8].MarketStyle == style && loaded[8, 8].MarketRotation == rotation && loaded[12, 11].MarketAccess,
            "Supermarket appearance and service survive saving and reloading");
        loaded.Build(0, 12, TileKind.Bulldoze);
        check(loaded.LocalComfort(7, 11) == 0 && !loaded[12, 11].MarketAccess && loaded.Jobs == 0,
            "Road disconnection removes supermarket employment and both neighborhood effects");
        money = loaded.Money; loaded.Build(11, 11, TileKind.Bulldoze);
        check(loaded.Money == money - 40 && Enumerable.Range(0, 16).All(i => loaded[8 + i % 4, 8 + i / 4].Kind == TileKind.Empty),
            "Supermarket demolition from any occupied tile removes all cells for one fee");

        foreach (var (kind, tax, bonus) in new[] { (TileKind.Shop, 100, 20), (TileKind.SuperMixed, 400, 80),
            (TileKind.SuperIndustryCommerce, 1040, 80), (TileKind.UltraCity, 1560, 120), (TileKind.Factory, 160, 0) })
        {
            var mixed = Town(); mixed.Build(8, 8, TileKind.Supermarket);
            mixed.Build(13, 12 - City.Footprint(kind), kind);
            check(mixed.Chunks[new(0, 0)].Stats.BusinessIncome == 400 + tax + bonus,
                $"{kind} receives only its commercial portion of supermarket benefits");
            mixed.Build(8, 8, TileKind.Bulldoze);
            check(mixed.Chunks[new(0, 0)].Stats.BusinessIncome == tax, $"Removing supermarket reverses {kind} tax bonus immediately");
        }
        var edge = Town(); edge.Build(8, 8, TileKind.Supermarket);
        edge.Build(16, 11, TileKind.Shop); edge.Build(17, 11, TileKind.Shop);
        check(edge[16, 11].MarketAccess && !edge[17, 11].MarketAccess, "Commercial service has the same exact five-cell boundary");
        edge.Build(7, 8, TileKind.Home);
        check(edge[7, 8].Comfort == 10 && !edge[7, 8].Connected, "Housing can receive local comfort while requiring its own road for occupancy");

        var crossing = Town(); crossing.Purchase(new(-1, 0)); crossing.Build(-2, 8, TileKind.Supermarket);
        crossing.Build(2, 11, TileKind.Shop); crossing.SetActiveChunks([]);
        check(crossing.Chunks.Values.All(c => c.IsPacked), "Supermarket and commercial service survive cold-chunk compression");
        crossing.Save(path); var restored = City.Load(path);
        check(restored.BuildingAt(1, 11).Kind == TileKind.Supermarket && restored[2, 11].MarketAccess
            && restored.Chunks.Values.Sum(c => c.Stats.BusinessIncome) == 520,
            "Cross-boundary supermarket effects and appearance survive reload");
        restored.Build(1, 11, TileKind.Bulldoze);
        check(!restored.Tiles.Any(t => t.Kind == TileKind.Supermarket) && !restored[2, 11].MarketAccess,
            "Cross-boundary removal clears all supermarket cells and nearby commercial effect");
        string valid = File.ReadAllText(path);
        foreach (var (field, value) in new[] { ("MarketStyle", 16), ("MarketRotation", 4), ("Level", 2) })
        {
            var json = JsonNode.Parse(valid)!;
            foreach (var chunk in json["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
                if (cell.Value!["Kind"]!.GetValue<int>() == (int)TileKind.Supermarket && cell.Value["OffsetX"]!.GetValue<int>() == 0 && cell.Value["OffsetZ"]!.GetValue<int>() == 0)
                    cell.Value[field] = value;
            File.WriteAllText(path, json.ToJsonString());
            bool rejected = false; try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, $"Invalid saved supermarket {field} is rejected");
        }
        var styles = new HashSet<int>();
        for (int seed = 1; seed <= 256; seed++)
        {
            var sample = SelfTests.DevelopedCity(); sample.AppearanceSeed = seed; sample.Build(8, 8, TileKind.Supermarket);
            styles.Add(sample[8, 8].MarketStyle);
        }
        check(styles.Count == 16, "All sixteen supermarket designs can be selected");
        var water = SelfTests.DevelopedCity(); water.Money = 100000; water.AppearanceSeed = 7;
        for (int z = 6; z <= 13; z++) for (int x = 6; x <= 13; x++)
            if (x < 8 || x > 11 || z < 8 || z > 11) water.Build(x, z, TileKind.Water);
        water.Build(8, 8, TileKind.Supermarket);
        check(water[8, 8].MarketStyle is 6 or 12, "Waterfront supermarket placement favors deck or coastal designs");
        var road = SelfTests.DevelopedCity(); road.Build(12, 9, TileKind.Road); road.Build(8, 8, TileKind.Supermarket);
        check(road[8, 8].MarketRotation == 1, "Supermarket storefront faces the adjacent road at construction");
    }
}
