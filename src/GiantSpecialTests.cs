using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class GiantSpecialTests
{
    private static City Town(bool roads = true)
    {
        var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; c.AppearanceSeed = 42;
        if (roads) for (int x = 0; x < 24; x++) c.Build(x, 12, TileKind.Road);
        return c;
    }
    private static bool Service(City c, TileKind k, int x, int z) => k switch
    {
        TileKind.Zoo => c.HasZooService(x, z), TileKind.AmusementPark => c.HasAmusementService(x, z), _ => c.HasLaunchService(x, z)
    };
    internal static void Run(Action<bool, string> check, string path)
    {
        check(Enumerable.Range(4, 5).Select(City.SpecialUnlockYear).SequenceEqual(new[] { 30, 35, 40, 45, 50 }),
            "Special unlock dates increase five years per footprint size including future sizes");
        check(Enum.GetValues<TileKind>().Where(City.IsSpecial).All(k => !FacadeStyles.IsBuilding(k) && !City.HasHomes(k)
            && City.Housing(new Tile { Kind = k }) == 0 && City.CommercialTaxBase(new Tile { Kind = k }) == 0 && City.IndustrialTaxBase(new Tile { Kind = k }) == 0),
            "Special facilities are never classified as residential, commercial or industrial buildings");
        TileKind[] kinds = [TileKind.Zoo, TileKind.AmusementPark, TileKind.RocketLaunchSite];
        foreach (var kind in kinds)
        {
            var c = Town(); long money = c.Money; c.Build(8, 5, kind);
            check(City.Footprint(kind) == 7 && City.MaxLevel(kind) == 1 && City.UnlockYear(kind) == 45
                && c.Tiles.Count(t => t.Kind == kind) == 49 && c.Money == money - City.Cost(kind)
                && c.BuildingAt(14, 11) == c[8, 5] && c[8, 5].Connected, $"{kind} builds a connected 7x7 fixed-level facility for one charge");
            money = c.Money; c.Build(7, 5, kind); c.Build(20, 17, kind);
            check(c.Money == money && c[7, 5].Kind == TileKind.Empty && c[20, 17].Kind == TileKind.Empty,
                $"{kind} rejects partial overlap and unowned land atomically");
            int style = c[8, 5].LandmarkStyle, rotation = c[8, 5].LandmarkRotation;
            for (int i = 0; i < 24; i++) c.Tick();
            check(c[8, 5].Level == 1 && c[8, 5].Growth == 0 && c.Capacity == 0 && c.Jobs == 0
                && c.Chunks[new(0, 0)].Stats.BusinessIncome == 0 && c.LastExpense == 48 + LandmarkAppearance.Maintenance(kind),
                $"{kind} never grows, houses residents, employs workers or earns direct tax");
            c.Build(7, 6, TileKind.Water); c.Save(path); var loaded = City.Load(path);
            check(loaded[8, 5].LandmarkStyle == style && loaded[8, 5].LandmarkRotation == rotation,
                $"{kind} retains its appearance after time, nearby construction and saving");
            var valid = File.ReadAllText(path);
            foreach (var (field, value) in new[] { ("LandmarkStyle", -1), ("LandmarkStyle", 16), ("LandmarkRotation", -1), ("LandmarkRotation", 4), ("Level", 2), ("Growth", 1) })
            {
                var data = JsonNode.Parse(valid)!; data["Chunks"]![0]!["Cells"]![(5 * 24 + 8).ToString()]![field] = value;
                File.WriteAllText(path, data.ToJsonString()); bool rejected = false;
                try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
                check(rejected, $"{kind} rejects invalid saved {field}={value}");
            }
            money = loaded.Money; loaded.Build(14, 11, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind) && loaded.Money == money - 40, $"{kind} secondary-cell demolition removes all 49 cells for one fee");
            var poor = Town(false); poor.Money = City.Cost(kind) - 1; poor.Build(5, 5, kind);
            check(poor.Tiles.Count == 0, $"{kind} rejects insufficient funds");
            var styles = new HashSet<int>();
            for (int seed = 1; seed <= 128; seed++)
            {
                var sample = Town(false); sample.AppearanceSeed = seed; sample.Build(8, 5, kind); styles.Add(sample[8, 5].LandmarkStyle);
            }
            check(styles.Count == 16, $"All sixteen {kind} styles are available");
            var dense = Town(false);
            for (int z = 0; z <= 14; z += 7) for (int x = 0; x <= 14; x += 7)
            {
                var used = new HashSet<int>();
                for (int dz = -6; dz < 13; dz++) for (int dx = -6; dx < 13; dx++)
                    if (dense[x + dx, z + dz].Kind == kind) used.Add(dense.BuildingAt(x + dx, z + dz).LandmarkStyle);
                dense.Build(x, z, kind);
                check(!used.Contains(dense[x, z].LandmarkStyle), $"{kind} avoids nearby duplicate designs at {x},{z}");
            }
            var water = Town(false);
            for (int x = 7; x <= 15; x++) water.Build(x, 4, TileKind.Water);
            water.Build(15, 8, TileKind.Road); water.Build(8, 5, kind);
            check(water[8, 5].LandmarkStyle < 4 && water[8, 5].LandmarkRotation == 1, $"{kind} selects a waterfront style facing the east road");
            water.Save(path); loaded = City.Load(path); water.Build(16, 5, kind); loaded.Build(16, 5, kind);
            check(water[16, 5].LandmarkStyle == loaded[16, 5].LandmarkStyle, $"{kind} appearance selection remains deterministic after load");
            var cross = Town(false); cross.Purchase(new(-1, 0)); cross.Purchase(new(0, -1)); cross.Purchase(new(-1, -1));
            cross.Build(-3, -3, kind); cross.SetActiveChunks([]); cross.Tick(); cross.Save(path); loaded = City.Load(path);
            check(loaded.Tiles.Count(t => t.Kind == kind) == 49 && loaded.BuildingAt(3, 3).Kind == kind,
                $"{kind} survives four-chunk boundaries and cold storage");
            loaded.Build(3, 3, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind), $"{kind} cross-chunk demolition removes the whole footprint");

            var boundary = Town(); boundary.Build(8, 5, kind);
            check(Service(boundary, kind, 21, 11) && !Service(boundary, kind, 22, 11)
                && Service(boundary, kind, 18, 14) && !Service(boundary, kind, 19, 14), $"{kind} uses a seven-cell Manhattan radius");
            boundary.Build(0, 10, TileKind.SuperMixed);
            check(Service(boundary, kind, 0, 10), $"{kind} checks the full target footprint beyond anchor-only range");
            boundary.Build(14, 11, TileKind.Bulldoze);
            check(!Service(boundary, kind, 0, 10) && !boundary[0, 10].AmusementAccess && !boundary[0, 10].LaunchAccess && boundary[0, 10].Comfort == 0,
                $"{kind} demolition immediately removes neighborhood benefits");
        }
        var zoo = Town(); zoo.Build(8, 5, TileKind.Zoo); zoo.Build(15, 11, TileKind.Home);
        check(zoo[15, 11].Comfort == 25, "Zoo gives twenty-five residential comfort");
        zoo.Build(8, 13, TileKind.Zoo); zoo.Build(16, 11, TileKind.Forest);
        check(zoo[15, 11].Comfort == 25, "Zoo respects the global comfort cap");
        zoo.Build(0, 12, TileKind.Bulldoze);
        check(!zoo.HasZooService(15, 11) && zoo[15, 11].Comfort == 15, "Zoo loses its benefit when disconnected");
        foreach (var source in new[] { TileKind.AmusementPark, TileKind.RocketLaunchSite })
        foreach (var target in new[] { TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.UltraCity })
        {
            var c = Town(); c.Build(8, 5, source); int z = 12 - City.Footprint(target); c.Build(15, z, target); var t = c[15, z];
            int income = City.CommercialTaxBase(t) + City.IndustrialTaxBase(t);
            int bonus = (source == TileKind.AmusementPark ? City.CommercialTaxBase(t) : City.IndustrialTaxBase(t)) * 35 / 100;
            check(c.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"{source} boosts only the intended portion of {target}");
            c.Build(8, 13, source);
            check(c.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"Multiple {source} facilities do not stack on {target}");
            c.SetActiveChunks([]); c.Tick(); c.Save(path); var restored = City.Load(path);
            check(restored.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"{source} benefits survive cold simulation and reload");
            restored.Build(0, 12, TileKind.Bulldoze);
            check(!restored[15, z].AmusementAccess && !restored[15, z].LaunchAccess, $"{source} disconnection clears cached services");
        }
        var old = Town(); old.Build(8, 5, TileKind.Aquarium); old.Save(path);
        var legacy = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var chunk in legacy["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
        { cell.Value!.AsObject().Remove("AmusementAccess"); cell.Value.AsObject().Remove("LaunchAccess"); }
        File.WriteAllText(path, legacy.ToJsonString());
        check(City.Load(path)[8, 5].Kind == TileKind.Aquarium, "Existing saves without seven-cell special fields remain compatible");
    }
}
