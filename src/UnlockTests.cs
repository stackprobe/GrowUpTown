using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class UnlockTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        TileKind[] normal = [TileKind.Road, TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.Park, TileKind.Water, TileKind.Bulldoze];
        TileKind[] advanced = [TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.SuperHome,
            TileKind.UltraCity, TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter, TileKind.Forest,
            TileKind.Aquarium, TileKind.FerrisWheel, TileKind.RoboticsLab, TileKind.ConcertHall, TileKind.ShoppingMall, TileKind.SpaceCenter, TileKind.Zoo, TileKind.AmusementPark, TileKind.RocketLaunchSite, TileKind.HotSpringResort, TileKind.Stadium, TileKind.Shipyard, TileKind.SafariPark, TileKind.CasinoResort, TileKind.FutureEnergyBase, TileKind.MarineResort, TileKind.ExpoVenue, TileKind.MechaArsenal];
        var initial = new City();
        check(normal.All(initial.IsUnlocked) && advanced.All(k => !initial.IsUnlocked(k)) && !initial.SpecialsUnlocked,
            "New town starts with normal tools available and all advanced categories locked");
        foreach (var kind in advanced)
        {
            var city = City.NewEmpty(); city.Money = 1_000_000;
            int unlockMonth = kind switch
            {
                TileKind.SuperMixed => 109,
                TileKind.SuperFactory => 169,
                TileKind.SuperIndustryCommerce => 133,
                TileKind.SuperHome => 229,
                TileKind.UltraCity or TileKind.HotSpringResort or TileKind.Stadium or TileKind.Shipyard => 589,
                TileKind.GrandPark or TileKind.Supermarket or TileKind.LogisticsCenter => 349,
                TileKind.Aquarium or TileKind.FerrisWheel or TileKind.RoboticsLab => 409,
                TileKind.Forest or TileKind.ConcertHall or TileKind.ShoppingMall or TileKind.SpaceCenter => 469,
                TileKind.Zoo or TileKind.AmusementPark or TileKind.RocketLaunchSite => 529,
                TileKind.SafariPark or TileKind.CasinoResort or TileKind.FutureEnergyBase => 649,
                TileKind.MarineResort or TileKind.ExpoVenue or TileKind.MechaArsenal => 709,
                _ => throw new InvalidOperationException($"Missing unlock expectation: {kind}")
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
        (int Year, TileKind[] Unlocked)[] milestones =
        [
            (5, []),
            (10, [TileKind.SuperMixed]),
            (12, [TileKind.SuperMixed, TileKind.SuperIndustryCommerce]),
            (15, [TileKind.SuperMixed, TileKind.SuperIndustryCommerce, TileKind.SuperFactory]),
            (20, [TileKind.SuperMixed, TileKind.SuperIndustryCommerce, TileKind.SuperFactory, TileKind.SuperHome]),
            (30, [TileKind.SuperMixed, TileKind.SuperIndustryCommerce, TileKind.SuperFactory, TileKind.SuperHome,
                TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter]),
            (35, [TileKind.SuperMixed, TileKind.SuperIndustryCommerce, TileKind.SuperFactory, TileKind.SuperHome,
                TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter, TileKind.Aquarium, TileKind.FerrisWheel, TileKind.RoboticsLab]),
            (40, [TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.SuperHome,
                TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter, TileKind.Forest, TileKind.Aquarium, TileKind.FerrisWheel, TileKind.RoboticsLab, TileKind.ConcertHall, TileKind.ShoppingMall, TileKind.SpaceCenter]),
            (45, advanced.Where(k => k != TileKind.UltraCity && !LandmarkAppearance.IsMegaLandmark(k) && !LandmarkAppearance.IsNineCellLandmark(k) && !LandmarkAppearance.IsTenCellLandmark(k)).ToArray()),
            (50, advanced.Where(k => !LandmarkAppearance.IsNineCellLandmark(k) && !LandmarkAppearance.IsTenCellLandmark(k)).ToArray()),
            (55, advanced.Where(k => !LandmarkAppearance.IsTenCellLandmark(k)).ToArray()),
            (60, advanced)
        ];
        foreach (var (year, unlocked) in milestones)
        {
            while (stages.Month < (year - 1) * 12 + 1) stages.Tick();
            check(advanced.All(k => stages.IsUnlocked(k) == unlocked.Contains(k)), $"Year {year} unlocks exactly the expected buildings");
            check(stages.SpecialsUnlocked == (year >= 30), $"Year {year} has the expected special menu availability");
        }
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
