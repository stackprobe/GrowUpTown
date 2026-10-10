using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class LandmarkTests
{
    private static City Town(bool road = true)
    {
        var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; c.AppearanceSeed = 42;
        if (road) for (int x = 0; x < 24; x++) c.Build(x, 12, TileKind.Road);
        return c;
    }
    internal static void Run(Action<bool, string> check, string path)
    {
        TileKind[] kinds = [TileKind.Aquarium, TileKind.FerrisWheel, TileKind.RoboticsLab];
        foreach (var kind in kinds)
        {
            var city = Town(); long money = city.Money;
            city.Build(8, 7, kind);
            check(City.Footprint(kind) == 5 && City.MaxLevel(kind) == 1 && City.UnlockYear(kind) == 35,
                $"{kind} is a fixed-level 5x5 facility unlocked five years after the original specials");
            check(city.Tiles.Count(t => t.Kind == kind) == 25 && city[8, 7].Connected && city.BuildingAt(12, 11) == city[8, 7]
                && city.Money == money - City.Cost(kind), $"{kind} occupies 25 cells, connects at the far edge and charges once");
            money = city.Money; city.Build(7, 6, kind); city.Build(22, 18, kind);
            check(city.Money == money && city[7, 6].Kind == TileKind.Empty && city[22, 18].Kind == TileKind.Empty,
                $"{kind} rejects overlapping or unowned footprints atomically");
            int style = city[8, 7].LandmarkStyle, rotation = city[8, 7].LandmarkRotation;
            city.Build(7, 7, TileKind.Water);
            for (int i = 0; i < 12; i++) city.Tick();
            check(city[8, 7].Level == 1 && city[8, 7].Growth == 0 && city[8, 7].LandmarkStyle == style
                && city[8, 7].LandmarkRotation == rotation, $"{kind} never grows or changes appearance after nearby edits or time");
            check(city.LastExpense == 48 + 12 + LandmarkAppearance.Maintenance(kind) && city.Capacity == 0 && city.Jobs == 0,
                $"{kind} charges maintenance once and is a neighborhood support facility");
            city.Save(path); var loaded = City.Load(path);
            check(loaded[8, 7].LandmarkStyle == style && loaded[8, 7].LandmarkRotation == rotation && loaded[12, 11].Kind == kind,
                $"{kind} footprint and appearance survive save/load");
            string valid = File.ReadAllText(path);
            foreach (var (field, value) in new[] { ("LandmarkStyle", -1), ("LandmarkStyle", 16), ("LandmarkRotation", -1), ("LandmarkRotation", 4), ("Level", 2), ("Growth", 1) })
            {
                var json = JsonNode.Parse(valid)!;
                json["Chunks"]![0]!["Cells"]![(7 * 24 + 8).ToString()]![field] = value;
                File.WriteAllText(path, json.ToJsonString());
                bool rejected = false; try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
                check(rejected, $"{kind} rejects invalid saved {field}={value}");
            }
            money = loaded.Money; loaded.Build(12, 11, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind) && loaded.Money == money - City.Cost(TileKind.Bulldoze),
                $"{kind} demolition from a secondary cell clears all 25 cells for one charge");
            var poor = Town(false); poor.Money = City.Cost(kind) - 1; poor.Build(5, 5, kind);
            check(poor.Tiles.Count == 0 && poor.Money == City.Cost(kind) - 1, $"{kind} rejects insufficient funds");
            var adjacent = Town(false);
            for (int z = 1; z <= 16; z += 5) for (int x = 1; x <= 16; x += 5)
            {
                var used = new HashSet<int>();
                for (int dz = -6; dz <= 10; dz++) for (int dx = -6; dx <= 10; dx++)
                    if (adjacent[x + dx, z + dz].Kind == kind) used.Add(adjacent.BuildingAt(x + dx, z + dz).LandmarkStyle);
                adjacent.Build(x, z, kind);
                check(used.Count == 16 || !used.Contains(adjacent[x, z].LandmarkStyle), $"{kind} at {x},{z} uses an unused nearby appearance");
            }
            var styles = new HashSet<int>();
            for (int seed = 1; seed <= 128; seed++)
            {
                var sample = Town(false); sample.AppearanceSeed = seed; sample.Build(8, 7, kind); styles.Add(sample[8, 7].LandmarkStyle);
            }
            check(styles.Count == 16, $"All sixteen {kind} appearances can be selected");
            var waterfront = Town(false);
            for (int x = 7; x <= 13; x++) waterfront.Build(x, 6, TileKind.Water);
            waterfront.Build(13, 8, TileKind.Road); waterfront.Build(8, 7, kind);
            check(waterfront[8, 7].LandmarkStyle < 4 && waterfront[8, 7].LandmarkRotation == 1,
                $"{kind} favors waterfront designs and faces the adjoining eastern road");
            waterfront.Save(path); loaded = City.Load(path);
            waterfront.Build(14, 7, kind); loaded.Build(14, 7, kind);
            check(waterfront[14, 7].LandmarkStyle == loaded[14, 7].LandmarkStyle
                && waterfront[14, 7].LandmarkRotation == loaded[14, 7].LandmarkRotation, $"{kind} choices remain deterministic after reload");
            var crossing = Town(false); crossing.Purchase(new(-1, 0)); crossing.Purchase(new(0, -1)); crossing.Purchase(new(-1, -1));
            crossing.Build(-2, -2, kind); crossing.SetActiveChunks([]); crossing.Tick(); crossing.Save(path); loaded = City.Load(path);
            check(loaded.BuildingAt(2, 2).Kind == kind && loaded.Tiles.Count(t => t.Kind == kind) == 25,
                $"{kind} survives four-chunk boundaries, negative coordinates and cold storage");
            loaded.Build(2, 2, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind), $"{kind} demolition crosses all four chunks");

            var crowded = Town(false); crowded.Purchase(new(1, 0)); crowded.Purchase(new(0, 1)); crowded.Purchase(new(1, 1));
            int nextStyle = 0; var touching = new HashSet<int>();
            for (int z = 0; z <= 20; z += 5) for (int x = 0; x <= 20; x += 5)
            {
                if (x == 10 && z == 10) continue;
                crowded.Build(x, z, kind); int id = nextStyle++ % 16; crowded[x, z].LandmarkStyle = id;
                if (Math.Abs(x - 10) <= 5 && Math.Abs(z - 10) <= 5) touching.Add(id);
            }
            crowded.Build(10, 10, kind);
            check(!touching.Contains(crowded[10, 10].LandmarkStyle), $"{kind} avoids adjacent duplicates even after all sixteen nearby styles are used");
        }
        var aquarium = Town(); aquarium.Build(8, 7, TileKind.Aquarium); aquarium.Build(13, 11, TileKind.Home);
        check(aquarium[13, 11].Comfort == 20 && aquarium.LocalComfort(17, 11) == 20 && aquarium.LocalComfort(18, 11) == 0,
            "Aquarium gives residential comfort twenty at distances up to five from its footprint");
        aquarium.Build(8, 13, TileKind.Aquarium);
        check(aquarium.LocalComfort(13, 11) == 20, "Overlapping aquariums do not stack comfort");
        aquarium.Build(14, 11, TileKind.Park);
        check(aquarium[13, 11].Comfort == 25, "Aquarium respects the global comfort cap");
        aquarium.Build(0, 12, TileKind.Bulldoze);
        check(!aquarium.HasAquariumService(13, 11) && aquarium[13, 11].Comfort == 3, "Disconnected aquariums immediately lose their effect");
        foreach (var source in new[] { TileKind.FerrisWheel, TileKind.RoboticsLab })
        foreach (var target in new[] { TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.UltraCity })
        {
            var city = Town(); city.Build(8, 7, source); city.Build(13, 12 - City.Footprint(target), target);
            var t = city[13, 12 - City.Footprint(target)];
            int expectedBase = City.CommercialTaxBase(t) + City.IndustrialTaxBase(t);
            int bonus = (source == TileKind.FerrisWheel ? City.CommercialTaxBase(t) : City.IndustrialTaxBase(t)) / 4;
            check(city.Chunks[new(0, 0)].Stats.BusinessIncome == expectedBase + bonus, $"{source} boosts only the intended tax portion of {target}");
            city.Build(8, 13, source);
            check(city.Chunks[new(0, 0)].Stats.BusinessIncome == expectedBase + bonus, $"Two {source} facilities do not multiply the {target} bonus");
            city.Build(0, 12, TileKind.Bulldoze);
            check(!t.FerrisWheelAccess && !t.RoboticsAccess, $"Road disconnection clears {source} service caches");
        }
        var combined = Town(); combined.Build(8, 7, TileKind.FerrisWheel); combined.Build(8, 13, TileKind.RoboticsLab);
        for (int z = 9; z < 12; z++) combined.Build(19, z, TileKind.Road);
        combined.Build(15, 6, TileKind.LogisticsCenter); combined.Build(15, 13, TileKind.Supermarket);
        combined.Build(13, 10, TileKind.SuperIndustryCommerce);
        check(combined[13, 10].FerrisWheelAccess && combined[13, 10].RoboticsAccess && combined[13, 10].MarketAccess && combined[13, 10].LogisticsAccess,
            "Support facilities reach the full target footprint");
        check(combined.Chunks[new(0, 0)].Stats.BusinessIncome == 1000 + 1040 + 260 + 208 + 80,
            "New bonuses add to existing service bonuses without compounding");
        combined.SetActiveChunks([]); combined.Tick(); combined.Save(path); var restored = City.Load(path);
        check(restored.Chunks[new(0, 0)].Stats.BusinessIncome == 2588 && restored[13, 10].FerrisWheelAccess && restored[13, 10].RoboticsAccess,
            "Tax service caches survive cold simulation and are restored on load");
        foreach (var kind in kinds)
        {
            var boundary = Town(); boundary.Build(8, 7, kind);
            check(Service(boundary, kind, 17, 11) && !Service(boundary, kind, 18, 11)
                && Service(boundary, kind, 15, 13) && !Service(boundary, kind, 16, 13), $"{kind} respects the Manhattan range boundary");
            boundary.Build(2, 10, TileKind.SuperMixed);
            check(Service(boundary, kind, 2, 10), $"{kind} reaches a multi-cell target whose anchor alone is out of range");
            boundary.Build(12, 11, TileKind.Bulldoze);
            check(!Service(boundary, kind, 2, 10) && !boundary[2, 10].FerrisWheelAccess && !boundary[2, 10].RoboticsAccess
                && boundary[2, 10].Comfort == 0, $"{kind} demolition immediately removes its neighborhood effect");
        }
        var legacy = Town(); legacy.Build(8, 8, TileKind.Supermarket); legacy.Save(path);
        var old = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var chunk in old["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
            foreach (var field in new[] { "LandmarkStyle", "LandmarkRotation", "FerrisWheelAccess", "RoboticsAccess" }) cell.Value!.AsObject().Remove(field);
        File.WriteAllText(path, old.ToJsonString());
        check(City.Load(path)[8, 8].Kind == TileKind.Supermarket, "Older version-three saves without landmark fields still load");
    }
    private static bool Service(City city, TileKind kind, int x, int z) => kind switch
    {
        TileKind.Aquarium => city.HasAquariumService(x, z), TileKind.FerrisWheel => city.HasFerrisWheelService(x, z), _ => city.HasRoboticsService(x, z)
    };
}
