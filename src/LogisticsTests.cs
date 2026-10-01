using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class LogisticsTests
{
    private static City Town()
    {
        var city = SelfTests.DevelopedCity(); city.Money = 20_000_000; city.AppearanceSeed = 812;
        for (int x = 0; x <= 20; x++) city.Build(x, 12, TileKind.Road);
        return city;
    }
    internal static void Run(Action<bool, string> check, string path)
    {
        var legacy = new City(); legacy.Save(path);
        File.WriteAllText(path, File.ReadAllText(path).Replace(",\"LogisticsStyle\":0", "").Replace(",\"LogisticsRotation\":0", "").Replace(",\"LogisticsAccess\":false", ""));
        check(City.Load(path).Population == 48, "Pre-logistics Version 3 saves remain compatible");
        var city = Town(); long money = city.Money;
        city.Build(22, 8, TileKind.LogisticsCenter);
        check(city.Money == money && city[22, 8].Kind == TileKind.Empty, "Logistics center rejects partially unowned footprints atomically");
        city.Build(10, 10, TileKind.Road); money = city.Money; city.Build(8, 8, TileKind.LogisticsCenter);
        check(city.Money == money && city[8, 8].Kind == TileKind.Empty, "Logistics center rejects occupied interior cells");
        city.Build(10, 10, TileKind.Bulldoze); city.Money = 8399; city.Build(8, 8, TileKind.LogisticsCenter);
        check(city.Money == 8399 && city[8, 8].Kind == TileKind.Empty, "Logistics center requires full construction funds");
        city.Money = 20_000_000; money = city.Money; city.Build(8, 8, TileKind.LogisticsCenter);
        check(city.Money == money - 8400 && city.Tiles.Count(t => t.Kind == TileKind.LogisticsCenter) == 16
            && city[8, 8].Connected && ReferenceEquals(city.BuildingAt(11, 11), city[8, 8]),
            "Logistics center occupies 4x4 and connects at its far edge");
        check(city.HasLogisticsService(16, 9) && !city.HasLogisticsService(17, 9) && city.HasLogisticsService(3, 9)
            && city.HasLogisticsService(9, 3) && city.HasLogisticsService(9, 16) && !city.HasLogisticsService(15, 14),
            "Logistics effect extends exactly five Manhattan cells from every footprint edge");
        city.Build(12, 11, TileKind.Shop); city.Build(13, 11, TileKind.Factory);
        check(city.Jobs == 144 && city.Chunks[new(0, 0)].Stats.BusinessIncome == 912
            && city[12, 11].LogisticsAccess && city[13, 11].LogisticsAccess,
            "Logistics center adds 96 jobs and boosts industrial and commercial tax bases by twenty percent");
        city.Tick(); check(city.LastExpense == 42 + 180 + 8 + 14, "Logistics maintenance counts once per building");
        for (int x = 1; x <= 7; x++)
        {
            city.Build(x, 11, TileKind.Home); city[x, 11].Level = 3; city[x, 11].Residents = 72;
            FacadeStyles.CompleteFloors(city, x, 11);
        }
        city.Reconnect(); city.Tick();
        check(city.LastIncome == city.Population * city.TaxRate / 2 + 912, "Logistics bonus reaches actual monthly tax receipts");
        check(city[7, 11].Comfort == 0 && !city[7, 11].LogisticsAccess, "Logistics center does not add residential effects or industrial pollution");
        int style = city[8, 8].LogisticsStyle, rotation = city[8, 8].LogisticsRotation;
        for (int z = 6; z < 12; z++) city.Build(0, z, TileKind.Road);
        for (int x = 1; x <= 4; x++) city.Build(x, 6, TileKind.Road);
        city.Build(4, 7, TileKind.LogisticsCenter); city.Build(12, 5, TileKind.LogisticsCenter);
        check(city[4, 7].LogisticsStyle != style && city[12, 5].LogisticsStyle != style
            && city[4, 7].LogisticsStyle != city[12, 5].LogisticsStyle, "Nearby and touching logistics centers select distinct designs");
        check(city.Chunks[new(0, 0)].Stats.BusinessIncome == 1512, "Multiple logistics centers never stack their bonuses");
        for (int i = 0; i < 36; i++) city.Tick();
        check(city[8, 8].Level == 1 && city[8, 8].Growth == 0 && city[8, 8].LogisticsStyle == style && city[8, 8].LogisticsRotation == rotation,
            "Logistics center level and appearance remain fixed over time and nearby construction");
        city.Save(path); var loaded = City.Load(path);
        check(loaded[8, 8].LogisticsStyle == style && loaded[8, 8].LogisticsRotation == rotation && loaded[12, 11].LogisticsAccess,
            "Logistics appearance, orientation and service survive save/load");
        loaded.Build(0, 12, TileKind.Bulldoze);
        check(!loaded[12, 11].LogisticsAccess && !loaded[13, 11].LogisticsAccess && loaded.Jobs == 0,
            "Road disconnection removes logistics employment and both neighborhood effects");
        money = loaded.Money; loaded.Build(11, 11, TileKind.Bulldoze);
        check(loaded.Money == money - 40 && Enumerable.Range(0, 16).All(i => loaded[8 + i % 4, 8 + i / 4].Kind == TileKind.Empty),
            "Logistics demolition from an interior tile removes all sixteen cells for one fee");
        foreach (var (kind, tax) in new[] { (TileKind.Shop, 100), (TileKind.Factory, 160), (TileKind.SuperMixed, 400),
            (TileKind.SuperFactory, 640), (TileKind.SuperIndustryCommerce, 1040), (TileKind.UltraCity, 1560) })
        {
            var mixed = Town(); mixed.Build(8, 8, TileKind.LogisticsCenter);
            mixed.Build(13, 12 - City.Footprint(kind), kind);
            check(mixed.Chunks[new(0, 0)].Stats.BusinessIncome == 600 + tax + tax / 5, $"{kind} receives logistics benefits for its business portions");
            mixed.Build(8, 8, TileKind.Bulldoze);
            check(mixed.Chunks[new(0, 0)].Stats.BusinessIncome == tax, $"Removing logistics center immediately reverses {kind} bonus");
        }
        var combined = Town(); combined.Build(8, 8, TileKind.LogisticsCenter); combined.Build(4, 8, TileKind.Supermarket);
        combined.Build(12, 11, TileKind.Shop); combined.Build(2, 10, TileKind.SuperIndustryCommerce);
        check(combined[2, 10].LogisticsAccess, "Logistics range uses the target's full footprint, not only its anchor");
        check(combined.Chunks[new(0, 0)].Stats.BusinessIncome == 1000 + 140 + 1040 + 208 + 80,
            "Supermarket and logistics bonuses add without compounding or boosting the special facilities themselves");
        combined.Build(16, 11, TileKind.Shop); combined.Build(17, 11, TileKind.Shop);
        check(combined[16, 11].LogisticsAccess && !combined[17, 11].LogisticsAccess, "Commercial logistics service respects exact range boundary");

        var crossing = Town(); crossing.Purchase(new(-1, 0)); crossing.Build(-2, 8, TileKind.LogisticsCenter);
        crossing.Build(2, 11, TileKind.Factory); crossing.SetActiveChunks([]);
        check(crossing.Chunks.Values.All(c => c.IsPacked), "Logistics data can be compressed offscreen");
        crossing.Tick(); crossing.Save(path); var restored = City.Load(path);
        check(restored.BuildingAt(1, 11).Kind == TileKind.LogisticsCenter && restored[2, 11].LogisticsAccess
            && restored.Chunks.Values.Sum(c => c.Stats.BusinessIncome) == 792,
            "Cross-chunk logistics footprint and bonuses survive compression, time and save/load");
        restored.Build(1, 11, TileKind.Bulldoze);
        check(!restored.Tiles.Any(t => t.Kind == TileKind.LogisticsCenter) && !restored[2, 11].LogisticsAccess,
            "Cross-chunk logistics demolition clears every occupied cell and industrial bonus");
        string valid = File.ReadAllText(path);
        foreach (var (field, value) in new[] { ("LogisticsStyle", 16), ("LogisticsRotation", 4), ("Level", 2) })
        {
            var json = JsonNode.Parse(valid)!;
            foreach (var chunk in json["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
                if (cell.Value!["Kind"]!.GetValue<int>() == (int)TileKind.LogisticsCenter && cell.Value["OffsetX"]!.GetValue<int>() == 0 && cell.Value["OffsetZ"]!.GetValue<int>() == 0) cell.Value[field] = value;
            File.WriteAllText(path, json.ToJsonString());
            bool rejected = false; try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, $"Invalid logistics {field} is rejected on load");
        }
        var styles = new HashSet<int>();
        for (int seed = 1; seed <= 256; seed++)
        {
            var sample = SelfTests.DevelopedCity(); sample.AppearanceSeed = seed; sample.Build(8, 8, TileKind.LogisticsCenter);
            styles.Add(sample[8, 8].LogisticsStyle);
        }
        check(styles.Count == 16, "All sixteen logistics designs are selectable");
        var water = SelfTests.DevelopedCity(); water.Money = 100000; water.AppearanceSeed = 7;
        for (int z = 6; z <= 13; z++) for (int x = 6; x <= 13; x++)
            if (x < 8 || x > 11 || z < 8 || z > 11) water.Build(x, z, TileKind.Water);
        water.Build(8, 8, TileKind.LogisticsCenter);
        check(water[8, 8].LogisticsStyle is 3 or 10, "Waterfront logistics placement favors container or refrigerated depots");
        var road = SelfTests.DevelopedCity(); road.Build(12, 9, TileKind.Road); road.Build(8, 8, TileKind.LogisticsCenter);
        check(road[8, 8].LogisticsRotation == 1, "Logistics loading apron faces adjacent roads when constructed");
    }
}
