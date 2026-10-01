namespace GrowUpTown;

internal static class SuperHomeTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        var city = SelfTests.DevelopedCity();
        city.Money = 10_000_000;
        city.Build(0, 12, TileKind.Road);
        long money = city.Money;
        city.Build(0, 10, TileKind.SuperHome);
        check(city.Money == money - 4800 && city.Tiles.Count == 5 && city[1, 11].Connected,
            "Residential tower occupies 2x2, charges once and connects at its outer edge");
        check(city.Capacity == 192 && city.Jobs == 0 && city.Chunks[new(0, 0)].Stats.BusinessIncome == 0,
            "Residential tower adds housing without jobs or business taxes");
        money = city.Money;
        city.Build(1, 11, TileKind.Road);
        city.Build(1, 9, TileKind.SuperHome);
        city.Build(23, 23, TileKind.SuperHome);
        check(city.Money == money && city[1, 9].Kind == TileKind.Empty,
            "Residential tower rejects overlaps and unowned footprints atomically");
        for (int x = 1; x <= 20; x++) city.Build(x, 12, TileKind.Road);
        for (int x = 4; x <= 20; x++) city.Build(x, 11, TileKind.Factory);
        var tower = city[0, 10];
        var styles = tower.FaceStyles.ToArray();
        city.Tick();
        check(tower.Residents == 32 && city.LastExpense == 21 * 2 + 17 * 14 + 40,
            "Residential tower admits 32 residents and charges maintenance once");
        for (int month = 0; month < 240; month++) city.Tick();
        check(tower.Level == 6 && tower.Residents <= 1152 && tower.Residents > 960
            && tower.FaceStyles.Length == 24 && styles.SequenceEqual(tower.FaceStyles.Take(4)),
            "Residential tower naturally grows to level 6 with stable existing floor styles");
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(1, 11).Level == 6 && city.BuildingAt(1, 11).Residents == tower.Residents,
            "Residential tower population and level survive save/load");
        long population = city.Population;
        city.Build(0, 12, TileKind.Bulldoze);
        city.Tick();
        check(city.Capacity == 0 && city.Population < population && !city.BuildingAt(1, 11).Connected,
            "Disconnected tower loses service and residents");
        city.Build(1, 11, TileKind.Bulldoze);
        check(city[0, 10].Kind == TileKind.Empty && city.Population == 0,
            "Residential tower demolition from secondary cell removes all residents and cells");
        city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
        city.Build(23, 23, TileKind.SuperHome);
        city.SetActiveChunks([]);
        city.Save(path);
        city = City.Load(path);
        check(city.BuildingAt(24, 24).Kind == TileKind.SuperHome,
            "Residential tower survives compression across four chunk boundaries");
        city[23, 23].Level = 7;
        FacadeStyles.CompleteFloors(city, 23, 23);
        city.Save(path);
        bool rejected = false;
        try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Residential tower levels above six are rejected when loading");
    }
}
