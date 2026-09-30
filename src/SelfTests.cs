namespace GrowUpTown;

internal static class SelfTests
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + name);
            checks++; Console.WriteLine("PASS: " + name);
        }
        string path = Path.Combine(Path.GetTempPath(), "growuptown-test-" + Guid.NewGuid() + ".json");
        try
        {
            var city = new City();
            Check(city.Population == 48 && city.Jobs == 48, "Starter population and jobs");
            city.Build(4, 4, TileKind.Home);
            Check(!city[4, 4].Connected, "Isolated residence has no road service");
            long money = city.Money;
            city.Build(4, 4, TileKind.Factory);
            Check(city.Money == money && city[4, 4].Kind == TileKind.Home, "Occupied cell cannot be overwritten");
            city.Money = 0;
            city.Build(4, 5, TileKind.Road);
            Check(city.Money == 0 && city[4, 5].Kind == TileKind.Empty, "Insufficient funds are rejected");
            city = new City();
            long initialPopulation = city.Population;
            for (int i = 0; i < 12; i++) city.Tick();
            Check(city.Population > initialPopulation && city.LastIncome > 0 && city.Balance > 0, "Monthly population growth and positive taxes");
            Check(city.Tiles.Any(t => t.Level > 1), "Viable buildings grow to higher levels");
            money = city.Money;
            city.Tick();
            Check(city.Money == money + city.LastIncome - city.LastExpense, "Monthly accounting balances");
            city.Build(0, 12, TileKind.Bulldoze);
            Check(city.Jobs == 0 && city.Capacity == 0, "Removing the external road disconnects the town");
            long before = city.Population;
            city.Tick();
            Check(city.Population < before, "Disconnected residents leave");
            city.Build(0, 12, TileKind.Road);
            Check(city.Jobs > 0 && city.Capacity > 0, "Restoring a road reconnects the town");
            city.TaxRate = 17;
            city.Save(path);
            var loaded = City.Load(path);
            Check(loaded.Population == city.Population && loaded.Money == city.Money && loaded.Month == city.Month && loaded.TaxRate == 17 && loaded.Jobs == city.Jobs, "Save/load round trip");
            File.WriteAllText(path, "{\"Version\":1,\"Tiles\":[]}");
            bool rejected = false;
            try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Invalid save is rejected");
            var local = new City(false);
            local.Build(0, 12, TileKind.Road); local.Build(1, 12, TileKind.Road); local.Build(2, 12, TileKind.Road);
            local.Build(1, 11, TileKind.Home); local.Build(2, 11, TileKind.Park);
            Check(local.LocalComfort(1, 11) > 0, "Connected parks improve nearby comfort");
            local.Build(1, 13, TileKind.Factory);
            Check(local.LocalComfort(1, 11) < 0, "Industry reduces nearby comfort");
            var lowTax = new City(); var highTax = new City { TaxRate = 20 };
            lowTax.Tick(); highTax.Tick();
            Check(lowTax.Happiness > highTax.Happiness, "High taxes reduce happiness");
            AppearanceChecks(Check, path);
            WorldTests.Run(Check, path);
            TrafficTests.Run(Check);
            ParkTests.Run(Check, path);
            Console.WriteLine($"All {checks} checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void AppearanceChecks(Action<bool, string> check, string path)
    {
        var city = new City();
        check(city.Tiles.All(FacadeStyles.IsValid), "Starter buildings have four valid face IDs per floor");
        var original = city.Tiles.Select(t => t.FaceStyles.ToArray()).ToArray();
        bool preserved = true;
        for (int month = 0; month < 60; month++)
        {
            city.Tick();
            for (int i = 0; i < original.Length; i++)
                preserved &= city.Tiles[i].FaceStyles.Take(original[i].Length).SequenceEqual(original[i]);
            original = city.Tiles.Select(t => t.FaceStyles.ToArray()).ToArray();
        }
        check(city.Tiles.Any(t => t.Level == 3), "Real monthly growth reaches a third floor");
        check(preserved && city.Tiles.All(FacadeStyles.IsValid), "Growth appends new faces and preserves every existing floor");
        float story = BuildingFacades.FloorHeight(TileKind.Factory);
        check(Math.Abs(BuildingFacades.Height(new Tile { Kind = TileKind.Factory, Level = 3 }) - 3 * story) < .00001f,
            "Factory growth uses fixed-height floors");

        var neighbors = new City(false) { AppearanceSeed = 1234, Money = 1000000 };
        neighbors.Build(10, 10, TileKind.Home);
        int[] first = neighbors[10, 10].FaceStyles.ToArray();
        neighbors.Build(10, 11, TileKind.Park);
        neighbors.Build(11, 10, TileKind.Factory);
        neighbors.Build(9, 10, TileKind.Road);
        neighbors.Tick();
        check(neighbors[10, 10].FaceStyles.SequenceEqual(first), "Environment changes do not re-roll existing facades");
        neighbors.Build(10, 10, TileKind.Bulldoze);
        check(neighbors[10, 10].FaceStyles.Length == 0, "Demolition removes the building's face IDs");
        neighbors.Build(10, 10, TileKind.Shop);
        check(FacadeStyles.IsValid(neighbors[10, 10]), "Rebuilding creates a fresh set of face IDs");

        foreach (var kind in new[] { TileKind.Home, TileKind.Shop, TileKind.Factory })
        {
            var uniform = new City(false) { AppearanceSeed = 42, Money = 1000000 };
            for (int z = 1; z < 23; z += 2) for (int x = 1; x < 23; x += 2) uniform.Build(x, z, kind);
            int[] counts = new int[FacadeStyles.Count];
            foreach (var tile in uniform.Tiles) foreach (int style in tile.FaceStyles) counts[style]++;
            check(counts.Min() > 0 && counts.Max() - counts.Min() <= 1, $"{kind}: all 16 designs stay balanced in a uniform neighborhood");
        }

        int influenced = 0;
        foreach (var kind in new[] { TileKind.Home, TileKind.Shop, TileKind.Factory })
            for (int seed = 1; seed <= 24; seed++)
            {
                var street = new City(false) { AppearanceSeed = seed };
                var park = new City(false) { AppearanceSeed = seed };
                street.Build(10, 11, TileKind.Road); park.Build(10, 11, TileKind.Park);
                street.Build(10, 10, kind); park.Build(10, 10, kind);
                if (street[10, 10].FaceStyles[0] != park[10, 10].FaceStyles[0]) influenced++;
            }
        check(influenced >= 12, "Facing a road versus a park affects new facade selection");

        city = new City();
        for (int i = 0; i < 8; i++) city.Tick();
        city.Save(path);
        string saved = File.ReadAllText(path);
        var loaded = City.Load(path);
        bool SameFaces(City a, City b) => a.Tiles.Zip(b.Tiles).All(pair => pair.First.FaceStyles.SequenceEqual(pair.Second.FaceStyles));
        check(SameFaces(city, loaded) && city.AppearanceSeed == loaded.AppearanceSeed, "Every facade and the appearance seed survive save/load");
        city.Build(3, 3, TileKind.Shop); loaded.Build(3, 3, TileKind.Shop);
        int levelsBeforeLoadingGrowth = city.Tiles.Sum(t => t.Level);
        for (int i = 0; i < 12; i++) { city.Tick(); loaded.Tick(); }
        check(city.Tiles.Sum(t => t.Level) > levelsBeforeLoadingGrowth && SameFaces(city, loaded), "Future construction and growth remain reproducible after loading");

        void RejectMutation(Action<System.Text.Json.Nodes.JsonNode> mutate, string name)
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(saved)!;
            mutate(json);
            File.WriteAllText(path, json.ToJsonString());
            bool rejected = false;
            try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, name);
        }
        int home = 11 * City.Size + 6;
        RejectMutation(json => json["Chunks"]![0]!["Cells"]![home.ToString()]!["FaceStyles"] = null, "Missing facade array is rejected");
        RejectMutation(json => json["Chunks"]![0]!["Cells"]![home.ToString()]!["FaceStyles"] = new System.Text.Json.Nodes.JsonArray(0), "Incorrect floor/face count is rejected");
        RejectMutation(json => json["Chunks"]![0]!["Cells"]![home.ToString()]!["FaceStyles"]![0] = 16, "Out-of-range facade ID is rejected");
        RejectMutation(json => json["Chunks"]![0]!["Cells"]![home.ToString()]!["FaceStyles"]![0] = -1, "Negative facade ID is rejected");
        RejectMutation(json => json["AppearanceSeed"] = 0, "Invalid appearance seed is rejected");
        RejectMutation(json => json["Version"] = 1, "Old saves require a new town");
    }
}
