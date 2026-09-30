namespace GrowUpTown;

internal static class ParkTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        (int X, int Z)[] directions = [(0, 1), (1, 0), (0, -1), (-1, 0)];
        bool patterns = true, edits = true, reciprocal = true;
        for (int mask = 0; mask < 16; mask++)
        {
            var city = City.NewEmpty(); city.Money = 100000;
            city.Build(10, 10, TileKind.Park);
            for (int arm = 0; arm < 4; arm++)
                if ((mask & (1 << arm)) != 0) city.Build(10 + directions[arm].X, 10 + directions[arm].Z, TileKind.Park);
            var original = ParkRenderer.Layout(city, 10, 10);
            patterns &= original.Connections == mask;
            for (int arm = 0; arm < 4; arm++)
            {
                int x = 10 + directions[arm].X, z = 10 + directions[arm].Z;
                bool present = (mask & (1 << arm)) != 0;
                if (present) reciprocal &= (ParkRenderer.Layout(city, x, z).Connections & (1 << ((arm + 2) % 4))) != 0;
                city.Build(x, z, present ? TileKind.Bulldoze : TileKind.Park);
                var changed = ParkRenderer.Layout(city, 10, 10);
                edits &= changed.Connections == (mask ^ (1 << arm)) && changed.Style != original.Style;
                city.Build(x, z, present ? TileKind.Park : TileKind.Bulldoze);
                edits &= ParkRenderer.Layout(city, 10, 10) == original;
            }
        }
        check(patterns && reciprocal, "All 16 park patterns connect paths in matching directions");
        check(edits, "Adding or removing any adjacent park changes its neighbor's layout and decoration");

        var stable = City.NewEmpty(); stable.Money = 10000000; stable.AppearanceSeed = 321;
        stable.Build(0, 5, TileKind.Park);
        var before = ParkRenderer.Layout(stable, 0, 5);
        stable.Build(1, 6, TileKind.Park); stable.Build(1, 5, TileKind.Home);
        stable.Tick();
        check(ParkRenderer.Layout(stable, 0, 5) == before, "Diagonal parks, non-park neighbors and monthly updates preserve park appearance");
        stable.Purchase(new(-1, 0));
        stable.Build(-1, 5, TileKind.Park);
        check(ParkRenderer.Layout(stable, 0, 5).Connections == 8 && ParkRenderer.Layout(stable, -1, 5).Connections == 2,
            "Park paths connect across negative chunk boundaries");
        before = ParkRenderer.Layout(stable, 0, 5);
        stable.Money = 0;
        stable.Build(0, 6, TileKind.Park);
        check(ParkRenderer.Layout(stable, 0, 5) == before, "Rejected construction does not alter a park");
        stable.Save(path);
        var loaded = City.Load(path);
        check(ParkRenderer.Layout(loaded, 0, 5) == before && ParkRenderer.Layout(loaded, -1, 5) == ParkRenderer.Layout(stable, -1, 5),
            "Park appearances survive save/load without additional save fields");
        loaded.Money = 1000; loaded.Build(-1, 5, TileKind.Bulldoze);
        check(ParkRenderer.Layout(loaded, 0, 5).Connections == 0 && ParkRenderer.Layout(loaded, 0, 5).Style != before.Style,
            "Removing a park across a chunk boundary closes the path and changes decoration");

        var styles = new HashSet<int>();
        for (int z = 12; z < 22; z += 2) for (int x = 2; x < 22; x += 2)
        {
            stable.Money = 1000; stable.Build(x, z, TileKind.Park);
            styles.Add(ParkRenderer.Layout(stable, x, z).Style);
        }
        check(styles.Count == ParkRenderer.StyleCount, "Placed isolated parks use all six decorative styles");
    }
}
