namespace GrowUpTown;

internal static class UltraCityTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        Appearance(check, path);
        var city = SelfTests.DevelopedCity();
        city.Money = 10_000_000;
        for (int x = 0; x <= 2; x++) city.Build(x, 12, TileKind.Road);
        long money = city.Money;
        city.Build(0, 9, TileKind.UltraCity);
        check(city.Money == money - 18000 && city.Tiles.Count == 12 && city[2, 11].Connected
            && city.BuildingAt(2, 11) == city[0, 9], "Ultra city occupies nine cells and connects through its far edge");
        check(city.Capacity == 432 && city.Jobs == 240 && city.Chunks[new(0, 0)].Stats.BusinessIncome == 1560,
            "Ultra city combines housing, commercial and industrial income exactly once");
        money = city.Money;
        city.Build(2, 11, TileKind.Home);
        city.Build(2, 8, TileKind.UltraCity);
        city.Build(22, 22, TileKind.UltraCity);
        check(city.Money == money && city[2, 8].Kind == TileKind.Empty && city[22, 22].Kind == TileKind.Empty,
            "Ultra city overlap and partially unowned 3x3 footprint are rejected atomically");
        check(city.LocalComfort(3, 10) == 0 && city.LocalComfort(0, 9) == 0,
            "Ultra city industry causes no pollution inside or outside its footprint");
        var tile = city[0, 9];
        var faces = tile.FaceStyles.ToArray();
        city.Tick();
        check(city.Population == 72 && city.LastExpense == 156, "Ultra city admits residents and charges upkeep once per month");
        for (int month = 0; month < 240; month++) city.Tick();
        check(tile.Level == 9 && tile.Residents == 3888 && city.Jobs == 2160 && tile.FaceStyles.Length == 36
            && faces.SequenceEqual(tile.FaceStyles.Take(4)), "Ultra city naturally grows to level 9 with stable original styles");
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(2, 11).Level == 9 && city.Population == 3888, "Ultra level 9 and population survive save/load");
        city.Build(0, 12, TileKind.Bulldoze);
        city.Tick();
        check(city.Capacity == 0 && city.Jobs == 0 && city.Population < 3888, "Disconnected ultra city loses service and residents");
        money = city.Money;
        city.Build(2, 11, TileKind.Bulldoze);
        check(city.Money == money - 40 && city.Population == 0
            && Enumerable.Range(0, 3).All(x => Enumerable.Range(9, 3).All(z => city[x, z].Kind == TileKind.Empty)),
            "Demolishing a far occupied cell removes all nine cells for one fee");
        city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
        city.Build(22, 22, TileKind.UltraCity);
        city.SetActiveChunks([]);
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(24, 24).Kind == TileKind.UltraCity && city[24, 24].OffsetX == 2,
            "Ultra 3x3 footprint survives compression across four chunks");
        city[22, 22].Level = 10;
        FacadeStyles.CompleteFloors(city, 22, 22);
        city.Save(path);
        bool rejected = false;
        try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Ultra levels above nine are rejected when loading");
    }

    private static void Appearance(Action<bool, string> check, string path)
    {
        var city = SelfTests.DevelopedCity();
        city.AppearanceSeed = 20260930;
        city.Money = 1_000_000;
        for (int z = 0; z < 16; z += 4) for (int x = 0; x < 16; x += 4)
            city.Build(x, z, TileKind.UltraCity);
        var towers = city.Tiles.Where(t => t.IsAnchor).ToArray();
        check(towers.Select(UltraAppearance.RoofVariant).Distinct().Count() >= 3,
            "Neighboring ultra buildings have distinct roof silhouettes");
        check(towers.Select(t => string.Join(',', Enumerable.Range(0, 4).SelectMany(face =>
            Enumerable.Range(0, 3).Select(panel => UltraAppearance.Style(t, 0, face, panel))))).Distinct().Count() >= 8,
            "Neighboring ultra buildings have distinct multi-face facade patterns");
        check(towers.All(t => Enumerable.Range(0, 4).All(face =>
            Enumerable.Range(0, 3).Select(panel => UltraAppearance.Category(t, face, panel)).Order().SequenceEqual(new[] { 0, 1, 2 })
            && Math.Abs(Enumerable.Range(0, 3).Sum(panel => UltraAppearance.PanelWidth(t, face, panel)) - 2.32f) < .001f)),
            "Every ultra face combines residential green, commercial blue and industrial orange across its full width");
        var tile = city[0, 0];
        int roof = UltraAppearance.RoofVariant(tile);
        int[] oldStyles = Enumerable.Range(0, 4).SelectMany(face => Enumerable.Range(0, 3)
            .Select(panel => UltraAppearance.Style(tile, 0, face, panel))).ToArray();
        tile.Level = 9;
        FacadeStyles.CompleteFloors(city, 0, 0);
        city.Save(path);
        tile = City.Load(path)[0, 0];
        check(UltraAppearance.RoofVariant(tile) == roof && oldStyles.SequenceEqual(Enumerable.Range(0, 4)
            .SelectMany(face => Enumerable.Range(0, 3).Select(panel => UltraAppearance.Style(tile, 0, face, panel)))),
            "Ultra colors and details retain their identity through growth and save/load");
    }
}
