namespace GrowUpTown;

internal static class SuperBuildingTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        var legacy = new City();
        legacy.Save(path);
        File.WriteAllText(path, File.ReadAllText(path).Replace(",\"OffsetX\":0", "").Replace(",\"OffsetZ\":0", ""));
        check(City.Load(path).Population == 48, "Existing version 3 saves without footprint fields still load");
        var city = SelfTests.DevelopedCity();
        city.Money = 10_000_000;
        city.Build(0, 12, TileKind.Road);
        city.Build(0, 10, TileKind.SuperMixed);
        check(city[0, 10].Connected && city[1, 11].Connected, "Super building connects through a non-anchor edge");
        check(city.Capacity == 96 && city.Jobs == 64 && city.Tiles.Count == 5, "Mixed building occupies four cells and counts housing/jobs once");
        long money = city.Money;
        city.Build(1, 11, TileKind.Home);
        city.Build(1, 9, TileKind.SuperFactory);
        city.Build(23, 1, TileKind.SuperFactory);
        check(city.Money == money && city[1, 9].Kind == TileKind.Empty, "Overlap and unowned footprint reject atomically");
        city.Tick();
        check(city.Population == 16 && city.LastExpense == 54, "Mixed building monthly population and maintenance count once");
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(1, 11).Residents == 16 && city.Capacity == 96, "Super building save/load resolves occupied cells");
        city.Build(1, 11, TileKind.Bulldoze);
        check(city.Tiles.Count == 1 && city.Capacity == 0 && city.Jobs == 0, "Demolition from any occupied cell removes entire building");
        city.Purchase(new(1, 0));
        city.Purchase(new(0, 1));
        city.Purchase(new(1, 1));
        city.Build(23, 23, TileKind.SuperFactory);
        city.SetActiveChunks([]);
        city.Tick();
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(24, 24).Kind == TileKind.SuperFactory && city[24, 24].OffsetX == 1, "Four-chunk footprint survives compression and save/load");
        city.Build(24, 24, TileKind.Bulldoze);
        check(city[23, 23].Kind == TileKind.Empty && city[24, 24].Kind == TileKind.Empty, "Cross-chunk demolition removes all four cells");

        foreach (var kind in new[] { TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce })
        {
            city = SelfTests.DevelopedCity();
            city.Money = 10_000_000;
            for (int x = 0; x < 20; x++) city.Build(x, 12, TileKind.Road);
            city.Build(0, 10, kind);
            if (kind is TileKind.SuperFactory or TileKind.SuperIndustryCommerce)
                for (int x = 0; x < 20; x++)
                {
                    city.Build(x, 20, TileKind.Home);
                    city[x, 20].Level = 3;
                    FacadeStyles.CompleteFloors(city, x, 20);
                }
            var building = city[0, 10];
            int[] firstFaces = building.FaceStyles.ToArray();
            for (int month = 0; month < 42; month++)
            {
                foreach (var t in city.Tiles.Where(t => t.IsAnchor && City.HasHomes(t.Kind))) t.Residents = City.Housing(t);
                city.Reconnect();
                city.Tick();
            }
            check(building.Level == 6 && building.FaceStyles.Length == 24 && firstFaces.SequenceEqual(building.FaceStyles.Take(4)), $"{kind} grows to level 6, caps there and preserves original faces");
            city.Save(path);
            city = City.Load(path);
            check(city.BuildingAt(1, 11).Level == 6, $"{kind} level 6 round trip");
            city.Build(0, 12, TileKind.Bulldoze);
            check(!city.BuildingAt(1, 11).Connected && city.Jobs == 0 && city.Capacity == 0, $"{kind} disconnects when the entrance road is removed");
        }

        city = SelfTests.DevelopedCity();
        city.Build(2, 2, TileKind.SuperMixed);
        city.Save(path);
        string valid = File.ReadAllText(path);
        File.WriteAllText(path, valid.Replace("\"OffsetX\":1", "\"OffsetX\":0"));
        bool rejected = false;
        try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Malformed footprint save is rejected");
        IndustryCommerce(check, path);
    }

    private static void IndustryCommerce(Action<bool, string> check, string path)
    {
        var city = SelfTests.DevelopedCity();
        city.Money = 10_000_000;
        city.Build(0, 12, TileKind.Road);
        long before = city.Money;
        city.Build(0, 10, TileKind.SuperIndustryCommerce);
        check(city.Money == before - 6200 && city.Tiles.Count == 5 && city[1, 11].Connected,
            "Industry-commerce charges once, occupies 2x2 and connects at its outer edge");
        check(city.Jobs == 192 && city.Capacity == 0 && city.Chunks[new(0, 0)].Stats.BusinessIncome == 1040,
            "Industry-commerce combines industrial and commercial jobs and tax base without housing");
        before = city.Money;
        city.Build(1, 11, TileKind.Shop);
        city.Build(1, 9, TileKind.SuperIndustryCommerce);
        city.Build(23, 23, TileKind.SuperIndustryCommerce);
        check(city.Money == before && city[1, 9].Kind == TileKind.Empty,
            "Industry-commerce overlap and unowned footprint are rejected without partial writes");
        check(city.LocalComfort(2, 11) < 0 && city.LocalComfort(6, 11) == 0,
            "Industry-commerce production affects nearby housing only within pollution range");
        for (int month = 0; month < 6; month++) city.Tick();
        check(city[0, 10].Level == 1 && city[0, 10].Growth == 0 && city.LastExpense == 90 && city.LastIncome == 0,
            "Industry-commerce needs workers to grow and pays maintenance once even when idle");
        city.Build(1, 11, TileKind.Bulldoze);
        check(city.Tiles.Count == 1 && city.Jobs == 0,
            "Industry-commerce demolition from a secondary cell removes the full building");
        city.Purchase(new(1, 0));
        city.Purchase(new(0, 1));
        city.Purchase(new(1, 1));
        city.Build(23, 23, TileKind.SuperIndustryCommerce);
        city[23, 23].Level = 6;
        FacadeStyles.CompleteFloors(city, 23, 23);
        city.Reconnect();
        city.SetActiveChunks([]);
        city.Tick();
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(24, 24).Kind == TileKind.SuperIndustryCommerce && city.BuildingAt(24, 24).Level == 6
            && city.BuildingAt(24, 24).FaceStyles.Length == 24,
            "Industry-commerce level 6 survives four-chunk compression and save/load");
        city[23, 23].Level = 7;
        FacadeStyles.CompleteFloors(city, 23, 23);
        city.Save(path);
        bool rejected = false;
        try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Industry-commerce saves above level 6 are rejected");
    }
}
