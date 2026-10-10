namespace GrowUpTown;

internal static class RailwayTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        City New() { var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; return c; }
        void Tracks(City c, int x, int z, int side)
        {
            for (int i = -1; i < 3; i++) { var p = City.StationSideCell(x, z, side, i); c.Build(p.X, p.Z, TileKind.Rail); }
        }
        foreach (var kind in new[] { TileKind.Rail, TileKind.Station })
        {
            var locked = City.NewEmpty(); locked.Money = 100_000;
            while (locked.Month < 468) locked.Tick();
            check(!locked.CanBuild(2, 2, kind), $"{kind} locked in year 39 December");
            locked.Tick();
            if (kind == TileKind.Station) Tracks(locked, 2, 2, 0);
            check(locked.Month == 469 && locked.CanBuild(2, 2, kind), $"{kind} unlocks in year 40 January");
        }
        var city = New();
        Tracks(city, 2, 2, 1); Tracks(city, 9, 2, 3);
        city.Build(2, 2, TileKind.Station);
        city.Build(9, 2, TileKind.Station);
        city.Build(5, 4, TileKind.Home);
        city.Build(4, 5, TileKind.Shop);
        check(!city.HasRailService(5, 4), "Isolated stations have no effect");
        for (int x = 4; x < 9; x++) city.Build(x, 2, TileKind.Rail);
        check(city[2, 2].RailActive && city[9, 2].RailActive && city[5, 4].Comfort == 10 && city[4, 5].RailAccess,
            "Two distinct stations activate comfort and commercial service through track");
        check(!city[5, 4].Connected, "Railway service does not substitute for road access");
        check(!city.HasRailService(4, 10), "Rail service has a bounded five-cell radius");
        city.Build(6, 2, TileKind.Road);
        city.Build(6, 1, TileKind.Road); city.Build(6, 3, TileKind.Road);
        check(city[6, 2].RailCrossing && city.RoadMask(6, 2) == 5 && city.RailMask(6, 2) == 10 && city[2, 2].RailActive,
            "Road over rail creates a crossing with independent road and railway masks");
        int style = city[2, 2].StationStyle, rotation = city[2, 2].StationRotation;
        city.Save(path); city = City.Load(path);
        check(city[6, 2].RailCrossing && city[2, 2].RailActive && city[2, 2].StationStyle == style,
            "Crossing, railway service and station design survive saving");
        city.Build(6, 2, TileKind.Bulldoze);
        check(!city[2, 2].RailActive && !city[9, 2].RailActive && city[5, 4].Comfort == 0 && !city[4, 5].RailAccess,
            "Cutting track immediately removes both station effects");
        city.Build(6, 2, TileKind.Road); city.Build(6, 2, TileKind.Rail);
        check(city[6, 2].RailCrossing && city[2, 2].RailActive && !city.CanBuild(6, 2, TileKind.Rail),
            "Rail over road reconnects stations without allowing duplicate track charges");
        city.Build(3, 4, TileKind.Park); city.Tick();
        check(city[2, 2].StationStyle == style && city[2, 2].StationRotation == rotation,
            "Environment changes and monthly simulation preserve station design and orientation");
        city.Build(3, 3, TileKind.Bulldoze);
        check(city[2, 2].Kind == TileKind.Empty && city[3, 3].Kind == TileKind.Empty && !city[9, 2].RailActive,
            "Demolition from a station child clears the whole footprint and deactivates the remaining station");
        city = New();
        Tracks(city, 2, 2, 3); Tracks(city, 4, 2, 1);
        city.Build(2, 2, TileKind.Station); city.Build(4, 2, TileKind.Station);
        check(!city[2, 2].RailActive && !city[4, 2].RailActive, "Touching stations and a single station's own cells do not count as a railway connection");
        for (int x = 2; x <= 5; x++) city.Build(x, 1, TileKind.Rail);
        check(city[2, 2].RailActive && city[4, 2].RailActive, "Track outside adjacent stations activates both");
        city = New(); city.Purchase(new(1, 0));
        Tracks(city, 21, 5, 1); Tracks(city, 26, 5, 3);
        city.Build(21, 5, TileKind.Station); city.Build(26, 5, TileKind.Station);
        for (int x = 23; x < 26; x++) city.Build(x, 5, TileKind.Rail);
        city.Build(24, 4, TileKind.Water); city.Build(24, 6, TileKind.Water);
        check(city.BridgeAxis(24, 5) == 1 && city.HasWater(24, 5) && city[21, 5].RailActive,
            "Rail bridges carry station connections across chunk boundaries");
        city.SetActiveChunks([new(0, 0)]); city.Save(path); city = City.Load(path);
        check(city[26, 5].RailActive && city.BridgeAxis(24, 5) == 1, "Cold chunks preserve station connections and bridges");
        city = New();
        var styles = new HashSet<int>();
        bool diverse = true;
        for (int z = 5; z <= 14; z += 3) for (int x = 5; x <= 11; x += 2)
        {
            Tracks(city, x, z, 2);
            var nearby = new HashSet<int>();
            for (int dz = -6; dz <= 7; dz++) for (int dx = -6; dx <= 7; dx++)
                if (city[x + dx, z + dz].Kind == TileKind.Station) nearby.Add(city.BuildingAt(x + dx, z + dz).StationStyle);
            city.Build(x, z, TileKind.Station); styles.Add(city[x, z].StationStyle);
            diverse &= !nearby.Contains(city[x, z].StationStyle) && StationAppearance.IsValid(city[x, z]);
        }
        check(diverse && StationAppearance.Count == 16 && styles.Count > 1, "Sixteen trackside stations avoid nearby designs from the sixteen-style palette");
        city.Save(path);
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        json["Chunks"]![0]!["Cells"]![(5 * City.Size + 5).ToString()]!["StationStyle"] = 16;
        File.WriteAllText(path, json.ToJsonString());
        bool rejected = false;
        try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Invalid station design is rejected on load");
        var green = New(); var urban = New(); green.AppearanceSeed = urban.AppearanceSeed = 42;
        green.Build(5, 4, TileKind.Park); urban.Build(5, 4, TileKind.Shop);
        Tracks(green, 5, 5, 1); Tracks(urban, 5, 5, 1);
        green.Build(5, 5, TileKind.Station); urban.Build(5, 5, TileKind.Station);
        check(green[5, 5].StationStyle / 4 == 0 && urban[5, 5].StationStyle / 4 == 2,
            "Park and commercial surroundings select fitting station architecture families");
        city = New();
        Tracks(city, 2, 2, 1); Tracks(city, 7, 2, 3); Tracks(city, 12, 2, 3);
        city.Build(2, 2, TileKind.Station); city.Build(7, 2, TileKind.Station); city.Build(12, 2, TileKind.Station);
        foreach (int x in new[] { 4, 5, 6, 9, 10, 11 }) city.Build(x, 2, TileKind.Rail);
        city.Build(5, 3, TileKind.Shop);
        for (int z = 4; z <= 12; z++) city.Build(0, z, TileKind.Road);
        for (int x = 1; x <= 5; x++) city.Build(x, 4, TileKind.Road);
        check(city[2, 2].RailActive && city[7, 2].RailActive && city[12, 2].RailActive,
            "Tracks connect through a middle station to the rest of the network");
        check(city.Chunks[new(0, 0)].Stats.BusinessIncome == 120, "Overlapping stations apply a single twenty-percent commercial tax bonus");
        city.Tick();
        check(city.LastExpense == 3 * 100 + 15 * 3 + 14 * 2 + 8, "Station maintenance counts once per footprint and rails charge per tile");
        for (int z = 5; z <= 10; z++)
        {
            city.Build(1, z, TileKind.Home); city[1, z].Residents = 24;
        }
        city.Reconnect(); city.Tick();
        check(city.LastIncome == city.Population * city.TaxRate / 2 + 120, "Station commercial bonus reaches actual monthly income");
        city = New(); city.Build(5, 5, TileKind.Water); city.Build(5, 5, TileKind.Rail);
        check(city[5, 5].Kind == TileKind.Rail && city.LocalComfort(5, 6) == 0, "Track can replace water and has no standalone comfort effect");
        bool masks = true;
        (int X, int Z)[] offsets = [(0, 1), (1, 0), (0, -1), (-1, 0)];
        for (int mask = 0; mask < 16; mask++)
        {
            city = New(); city.Build(5, 5, TileKind.Rail);
            for (int direction = 0; direction < 4; direction++)
                if ((mask & (1 << direction)) != 0) city.Build(5 + offsets[direction].X, 5 + offsets[direction].Z, TileKind.Rail);
            masks &= city.RailMask(5, 5) == mask;
        }
        check(masks, "All sixteen track connection patterns support ends, turns and junctions");
        bool buffers = RailwayRenderer.BufferStopArms(0).SequenceEqual(new[] { 1, 3 });
        for (int mask = 1; mask < 16; mask++)
        {
            var stops = RailwayRenderer.BufferStopArms(mask);
            int entry = System.Numerics.BitOperations.TrailingZeroCount((uint)mask);
            buffers &= System.Numerics.BitOperations.PopCount((uint)mask) == 1
                ? stops.SequenceEqual(new[] { (entry + 2) % 4 }) : stops.Length == 0;
        }
        check(buffers, "Buffer stops face the closed end in all four directions and are absent on through routes");
        foreach (int junction in new[] { 7, 11, 13, 14 })
        {
            int straight = RailwayRenderer.StraightArms(junction);
            var curves = RailwayRenderer.CurveRoutes(junction);
            check((straight == 5 || straight == 10) && curves.Length == 2
                && curves.All(c => (c & junction) == c)
                && (straight | curves[0] | curves[1]) == junction,
                $"T junction {junction} has a complete through route and two connecting turnouts");
        }
        check(RailwayRenderer.StraightArms(15) == 15 && RailwayRenderer.CurveRoutes(15).Length == 0,
            "Diamond crossing continues both straight tracks through the center");
        city = New();
        for (int x = 3; x <= 12; x++) city.Build(x, 5, TileKind.Rail);
        int[] before = Enumerable.Range(4, 9).Select(x => city.RailMask(x, 5)).ToArray();
        city.Build(4, 6, TileKind.Station); city.Build(10, 6, TileKind.Station);
        check(Enumerable.Range(4, 9).Select(x => city.RailMask(x, 5)).SequenceEqual(before)
            && city[4, 6].RailActive && city[10, 6].RailActive,
            "Stations beside a straight railway receive service without branches into the buildings");
        city.Save(path); city = City.Load(path);
        check(city.RailMask(5, 5) == 10 && city[4, 6].RailActive,
            "Loading preserves station service without adding station-facing rails");
    }
}
