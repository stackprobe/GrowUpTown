using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class TenCellSpecialTests
{
    private static City Town(bool roads = true)
    {
        var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; c.AppearanceSeed = 42;
        if (roads) for (int x = 0; x < 24; x++) c.Build(x, 12, TileKind.Road);
        return c;
    }
    private static bool Service(City c, TileKind k, int x, int z) => k switch
    {
        TileKind.MarineResort => c.HasMarineService(x, z), TileKind.ExpoVenue => c.HasExpoService(x, z), _ => c.HasArsenalService(x, z)
    };
    internal static void Run(Action<bool, string> check, string path)
    {
        check(Enumerable.Range(4, 7).Select(City.SpecialUnlockYear).SequenceEqual(new[] { 30, 35, 40, 45, 50, 55, 60 }),
            "Special unlock dates increase five years per footprint size including future sizes");
        check(Enum.GetValues<TileKind>().Where(City.IsSpecial).All(k => !FacadeStyles.IsBuilding(k) && !City.HasHomes(k)
            && City.Housing(new Tile { Kind = k }) == 0 && City.CommercialTaxBase(new Tile { Kind = k }) == 0 && City.IndustrialTaxBase(new Tile { Kind = k }) == 0),
            "Special facilities are never classified as residential, commercial or industrial buildings");
        TileKind[] kinds = [TileKind.MarineResort, TileKind.ExpoVenue, TileKind.MechaArsenal];
        foreach (var kind in kinds)
        {
            var c = Town(); long money = c.Money; c.Build(8, 2, kind);
            check(City.Footprint(kind) == 10 && City.MaxLevel(kind) == 1 && City.UnlockYear(kind) == 60
                && c.Tiles.Count(t => t.Kind == kind) == 100 && c.Money == money - City.Cost(kind)
                && c.BuildingAt(17, 11) == c[8, 2] && c[8, 2].Connected, $"{kind} builds a connected 10x10 fixed-level facility for one charge");
            money = c.Money; c.Build(7, 5, kind); c.Build(20, 17, kind);
            check(c.Money == money && c[7, 5].Kind == TileKind.Empty && c[20, 17].Kind == TileKind.Empty,
                $"{kind} rejects partial overlap and unowned land atomically");
            int style = c[8, 2].LandmarkStyle, rotation = c[8, 2].LandmarkRotation;
            for (int i = 0; i < 24; i++) c.Tick();
            check(c[8, 2].Level == 1 && c[8, 2].Growth == 0 && c.Capacity == 0 && c.Jobs == 0
                && c.Chunks[new(0, 0)].Stats.BusinessIncome == 0 && c.LastExpense == 48 + LandmarkAppearance.Maintenance(kind),
                $"{kind} never grows, houses residents, employs workers or earns direct tax");
            c.Build(7, 6, TileKind.Water); c.Save(path); var loaded = City.Load(path);
            check(loaded[8, 2].LandmarkStyle == style && loaded[8, 2].LandmarkRotation == rotation,
                $"{kind} retains its appearance after time, nearby construction and saving");
            var valid = File.ReadAllText(path);
            foreach (var (field, value) in new[] { ("LandmarkStyle", -1), ("LandmarkStyle", 16), ("LandmarkRotation", -1), ("LandmarkRotation", 4), ("Level", 2), ("Growth", 1) })
            {
                var data = JsonNode.Parse(valid)!; data["Chunks"]![0]!["Cells"]![(2 * 24 + 8).ToString()]![field] = value;
                File.WriteAllText(path, data.ToJsonString()); bool rejected = false;
                try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
                check(rejected, $"{kind} rejects invalid saved {field}={value}");
            }
            money = loaded.Money; loaded.Build(17, 11, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind) && loaded.Money == money - 40, $"{kind} secondary-cell demolition removes all 100 cells for one fee");
            var poor = Town(false); poor.Money = City.Cost(kind) - 1; poor.Build(5, 5, kind);
            check(poor.Tiles.Count == 0, $"{kind} rejects insufficient funds");
            var styles = new HashSet<int>();
            for (int seed = 1; seed <= 128; seed++)
            {
                var sample = Town(false); sample.AppearanceSeed = seed; sample.Build(8, 2, kind); styles.Add(sample[8, 2].LandmarkStyle);
            }
            check(styles.Count == 16, $"All sixteen {kind} styles are available");
            var dense = Town(false); dense.Purchase(new(1, 0)); dense.Purchase(new(0, 1)); dense.Purchase(new(1, 1));
            for (int z = 0; z <= 30; z += 10) for (int x = 0; x <= 30; x += 10)
            {
                var used = new HashSet<int>();
                for (int dz = -6; dz < 16; dz++) for (int dx = -6; dx < 16; dx++)
                    if (dense[x + dx, z + dz].Kind == kind) used.Add(dense.BuildingAt(x + dx, z + dz).LandmarkStyle);
                dense.Build(x, z, kind);
                check(!used.Contains(dense[x, z].LandmarkStyle), $"{kind} avoids nearby duplicate designs at {x},{z}");
            }
            var water = Town(false);
            for (int x = 7; x <= 18; x++) water.Build(x, 1, TileKind.Water);
            water.Build(18, 8, TileKind.Road); water.Build(8, 2, kind);
            check(water[8, 2].LandmarkStyle < 4 && water[8, 2].LandmarkRotation == 1, $"{kind} selects a waterfront style facing the east road");
            water.Save(path); loaded = City.Load(path); water.Build(0, 14, kind); loaded.Build(0, 14, kind);
            check(water[0, 14].LandmarkStyle == loaded[0, 14].LandmarkStyle, $"{kind} appearance selection remains deterministic after load");
            var cross = Town(false); cross.Purchase(new(-1, 0)); cross.Purchase(new(0, -1)); cross.Purchase(new(-1, -1));
            cross.Build(-3, -3, kind); cross.SetActiveChunks([]); cross.Tick(); cross.Save(path); loaded = City.Load(path);
            check(loaded.Tiles.Count(t => t.Kind == kind) == 100 && loaded.BuildingAt(6, 6).Kind == kind,
                $"{kind} survives four-chunk boundaries and cold storage");
            loaded.Build(6, 6, TileKind.Bulldoze);
            check(!loaded.Tiles.Any(t => t.Kind == kind), $"{kind} cross-chunk demolition removes the whole footprint");

            var boundary = Town(); boundary.Build(8, 2, kind);
            check(Service(boundary, kind, 27, 11) && !Service(boundary, kind, 28, 11)
                && Service(boundary, kind, 21, 17) && !Service(boundary, kind, 22, 17), $"{kind} uses a ten-cell Manhattan radius");
            boundary.Purchase(new(-1, 0)); boundary.Build(-3, 10, TileKind.SuperMixed);
            check(Service(boundary, kind, -3, 10), $"{kind} checks the full target footprint beyond anchor-only range");
            boundary.Build(17, 11, TileKind.Bulldoze);
            check(!Service(boundary, kind, -3, 10) && !boundary[-3, 10].ExpoAccess && !boundary[-3, 10].ArsenalAccess && boundary[-3, 10].Comfort == 0,
                $"{kind} demolition immediately removes neighborhood benefits");
        }
        var marine = Town(); marine.Build(8, 2, TileKind.MarineResort); marine.Build(18, 11, TileKind.Home);
        check(marine[18, 11].Comfort == 25, "Marine resort gives twenty-five residential comfort");
        marine.Build(8, 13, TileKind.MarineResort); marine.Build(19, 11, TileKind.Forest);
        check(marine[18, 11].Comfort == 25, "Marine resort respects the global comfort cap");
        marine.Build(0, 12, TileKind.Bulldoze);
        check(!marine.HasMarineService(18, 11) && marine[18, 11].Comfort == 15, "Marine resort loses its benefit when disconnected");
        foreach (var source in new[] { TileKind.ExpoVenue, TileKind.MechaArsenal })
        foreach (var target in new[] { TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.UltraCity })
        {
            var c = Town(); c.Build(8, 2, source); int z = 12 - City.Footprint(target); c.Build(18, z, target); var t = c[18, z];
            int income = City.CommercialTaxBase(t) + City.IndustrialTaxBase(t);
            int bonus = (source == TileKind.ExpoVenue ? City.CommercialTaxBase(t) : City.IndustrialTaxBase(t)) / 2;
            check(c.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"{source} boosts only the intended portion of {target}");
            c.Build(8, 13, source);
            check(c.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"Multiple {source} facilities do not stack on {target}");
            c.SetActiveChunks([]); c.Tick(); c.Save(path); var restored = City.Load(path);
            check(restored.Chunks[new(0, 0)].Stats.BusinessIncome == income + bonus, $"{source} benefits survive cold simulation and reload");
            restored.Build(0, 12, TileKind.Bulldoze);
            check(!restored[18, z].ExpoAccess && !restored[18, z].ArsenalAccess, $"{source} disconnection clears cached services");
        }
        var old = Town(); old.Build(8, 2, TileKind.Aquarium); old.Save(path);
        var legacy = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var chunk in legacy["Chunks"]!.AsArray()) foreach (var cell in chunk!["Cells"]!.AsObject())
        { cell.Value!.AsObject().Remove("ExpoAccess"); cell.Value.AsObject().Remove("ArsenalAccess"); }
        File.WriteAllText(path, legacy.ToJsonString());
        check(City.Load(path)[8, 2].Kind == TileKind.Aquarium, "Existing saves without ten-cell special fields remain compatible");
    }
}
