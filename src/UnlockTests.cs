using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class UnlockTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        TileKind[] normal = [TileKind.Road, TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.Park, TileKind.Water, TileKind.Bulldoze];
        TileKind[] advanced = [TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.SuperHome,
            TileKind.UltraCity, TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter];
        var initial = new City();
        check(normal.All(initial.IsUnlocked) && advanced.All(k => !initial.IsUnlocked(k)) && !initial.SpecialsUnlocked,
            "New town starts with normal tools available and all advanced categories locked");
        foreach (var kind in advanced)
        {
            var city = City.NewEmpty(); city.Money = 1_000_000;
            int unlockMonth = kind switch
            {
                TileKind.UltraCity => 109,
                TileKind.GrandPark or TileKind.Supermarket or TileKind.LogisticsCenter => 169,
                _ => 49
            };
            while (city.Month < unlockMonth - 1) city.Tick();
            long money = city.Money;
            check(!city.IsUnlocked(kind) && !city.CanBuild(5, 5, kind), $"{kind} remains locked in December before its unlock year");
            string result = city.Build(5, 5, kind);
            check(city.Money == money && city.Tiles.Count == 0 && result.Contains("解放"), $"{kind} cannot bypass its lock through construction calls");
            city.Save(path); var loaded = City.Load(path);
            check(!loaded.IsUnlocked(kind), $"{kind} lock survives save/load before the boundary");
            loaded.Tick();
            check(loaded.Month == unlockMonth && loaded.IsUnlocked(kind) && loaded.CanBuild(5, 5, kind),
                $"{kind} unlocks exactly in January of displayed year {City.UnlockYear(kind)}");
            loaded.Build(5, 5, kind);
            check(loaded[5, 5].Kind == kind && loaded.Money == money - City.Cost(kind), $"{kind} can be built after the boundary");
            loaded.Save(path); loaded = City.Load(path);
            check(loaded.IsUnlocked(kind) && loaded[5, 5].Kind == kind, $"{kind} stays unlocked and built after reloading");
        }
        var stages = City.NewEmpty();
        while (stages.Month < 49) stages.Tick();
        check(stages.IsUnlocked(TileKind.SuperHome) && !stages.IsUnlocked(TileKind.UltraCity) && !stages.SpecialsUnlocked,
            "Year five unlocks only the four super tools");
        while (stages.Month < 109) stages.Tick();
        check(stages.IsUnlocked(TileKind.UltraCity) && !stages.SpecialsUnlocked, "Year ten leaves the special menu sealed");
        while (stages.Month < 169) stages.Tick();
        check(stages.SpecialsUnlocked, "Year fifteen unlocks the special menu");
        // Existing pre-feature saves may already contain advanced buildings in year one.
        stages.Build(5, 5, TileKind.GrandPark); stages.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; json["Month"] = 1;
        foreach (var chunk in json["Chunks"]!.AsArray()) chunk!["LastSimulatedMonth"] = 0;
        File.WriteAllText(path, json.ToJsonString());
        var legacy = City.Load(path);
        check(legacy[5, 5].Kind == TileKind.GrandPark && !legacy.IsUnlocked(TileKind.GrandPark) && !legacy.CanBuild(10, 5, TileKind.GrandPark),
            "Legacy early-game special buildings survive loading without unlocking new construction");
        legacy.Build(8, 8, TileKind.Bulldoze);
        check(legacy[5, 5].Kind == TileKind.Empty, "Legacy locked-category buildings remain demolishable");
    }
}
