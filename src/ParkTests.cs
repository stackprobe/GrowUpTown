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
        AccessChecks(check, path);
    }
    private static void AccessChecks(Action<bool, string> check, string path)
    {
        (int X, int Z)[] directions = [(0, 1), (1, 0), (0, -1), (-1, 0)];
        bool roads = true;
        for (int mask = 0; mask < 16; mask++)
        {
            var city = City.NewEmpty(); city.Money = 100000;
            city.Build(10, 10, TileKind.Park);
            var before = ParkRenderer.Layout(city, 10, 10);
            for (int arm = 0; arm < 4; arm++)
                if ((mask & (1 << arm)) != 0) city.Build(10 + directions[arm].X, 10 + directions[arm].Z, TileKind.Road);
            var after = ParkRenderer.Layout(city, 10, 10);
            roads &= after.Connections == mask && after.Style == before.Style && after.Rotation == before.Rotation;
        }
        check(roads, "All sixteen road adjacency patterns extend park walkways without changing decorations");
        var chain = City.NewEmpty(); chain.Money = 10_000_000;
        chain.Build(0, 12, TileKind.Road);
        for (int x = 1; x <= 6; x++) chain.Build(x, 12, TileKind.Park);
        chain.Build(7, 12, TileKind.Home);
        chain.Build(6, 11, TileKind.Road); chain.Build(6, 10, TileKind.Factory);
        chain.Build(7, 13, TileKind.Park);
        check(Enumerable.Range(1, 6).All(x => chain[x, 12].Connected) && !chain[7, 13].Connected,
            "Park chains receive inlet-road access but diagonal parks do not");
        check(!chain[7, 12].Connected && !chain[6, 11].Connected && !chain[6, 10].Connected,
            "Park walkways cannot connect homes, industry or a separate road network");
        check(ParkRenderer.Layout(chain, 6, 12).Connections == 12, "Park walkways extend to a park and a road simultaneously");
        chain.Build(3, 12, TileKind.Bulldoze);
        check(chain[2, 12].Connected && !chain[4, 12].Connected && !chain[6, 12].Connected,
            "Removing a middle park immediately disconnects the downstream parks");
        chain.Build(3, 12, TileKind.Park);
        check(chain[6, 12].Connected, "Restoring a park reconnects the chain");
        chain.Purchase(new(-1, 0)); chain.Build(-1, 12, TileKind.Park); chain.Build(-2, 12, TileKind.Park);
        chain.SetActiveChunks([]); chain.Save(path); var loaded = City.Load(path);
        check(loaded[-2, 12].Connected && loaded[6, 12].Connected, "Park access across negative chunk boundaries survives compression and saving");
        loaded.Build(0, 12, TileKind.Bulldoze);
        check(loaded.Tiles.Where(t => t.Kind == TileKind.Park).All(t => !t.Connected), "Removing inlet access disconnects every park cluster");

        var effect = City.NewEmpty(); effect.Money = 100000;
        effect.Build(1, 12, TileKind.Park); effect.Build(2, 12, TileKind.Home);
        check(effect[2, 12].Comfort == 3 && effect.LocalComfort(4, 12) == 3 && effect.LocalComfort(5, 12) == 0,
            "Disconnected park provides exactly thirty percent comfort within its existing range");
        effect.Build(0, 12, TileKind.Road);
        check(effect[2, 12].Comfort == 10, "Road connection upgrades park comfort from three to ten");
        effect.Build(0, 12, TileKind.Bulldoze);
        check(effect[2, 12].Comfort == 3, "Road removal restores thirty percent park comfort");
        check(ParkRenderer.Layout(effect, 1, 12).Connections == 0, "Removing adjacent road retracts the walkway");

        var large = SelfTests.DevelopedCity(); large.Money = 10_000_000;
        large.Build(2, 9, TileKind.GrandPark); large.Build(6, 9, TileKind.GrandPark); large.Build(10, 12, TileKind.Park);
        int style = large[2, 9].ParkStyle, rotation = large[2, 9].ParkRotation;
        check(large.LocalComfort(2, 5) == 6, "Disconnected grand park provides six comfort, counted once");
        large.Build(0, 12, TileKind.Road); large.Build(1, 12, TileKind.Park);
        check(large[2, 9].Connected && large[6, 9].Connected && large[10, 12].Connected
            && large[9, 12].Connected && large.LocalComfort(2, 5) == 20,
            "Access crosses entire grand-park footprints and continues into other parks");
        check(ParkRenderer.Layout(large, 1, 12).Connections == 10, "Walkway reaches both the road and the grand park");
        large.Build(1, 12, TileKind.Bulldoze);
        check(!large[2, 9].Connected && !large[6, 9].Connected && !large[10, 12].Connected
            && large[2, 9].ParkStyle == style && large[2, 9].ParkRotation == rotation,
            "Breaking grand-park access disconnects the chain without changing its fixed appearance");
    }
}
