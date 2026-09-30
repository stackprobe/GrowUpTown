using System.Numerics;

namespace GrowUpTown;

internal static class TrafficTests
{
    internal static void Run(Action<bool, string> check)
    {
        bool validRoutes = true, continuous = true;
        Vector2[] arms = [new(0, 1), new(1, 0), new(0, -1), new(-1, 0)];
        for (int mask = 1; mask < 16; mask++) for (int entry = 0; entry < 4; entry++)
        {
            if ((mask & (1 << entry)) == 0) continue;
            for (uint seed = 0; seed < 12; seed++)
            {
                int exit = TrafficRenderer.ChooseExit(mask, entry, seed);
                validRoutes &= (mask & (1 << exit)) != 0 && (BitOperations.PopCount((uint)mask) == 1 || exit != entry);
                var end = TrafficRenderer.Pose(entry, exit, 1);
                var next = TrafficRenderer.Pose((exit + 2) % 4, exit, 0);
                continuous &= Vector2.Distance(end.Position, arms[exit] + next.Position) < .00001f
                    && Vector2.Distance(end.Heading, next.Heading) < .00001f;
                for (int t = 0; t <= 20; t++)
                {
                    var pose = TrafficRenderer.Pose(entry, exit, t / 20f);
                    validRoutes &= float.IsFinite(pose.Heading.X) && float.IsFinite(pose.Heading.Y)
                        && Math.Abs(pose.Position.X) <= .501f && Math.Abs(pose.Position.Y) <= .501f;
                }
            }
        }
        check(validRoutes, "Traffic follows all 15 road connection patterns, including dead ends");
        check(continuous, "Traffic positions and headings match at tile boundaries");

        var city = City.NewEmpty(); city.Money = 10_000_000;
        city.Purchase(new(1, 0));
        // Disconnected roads away from the original street, across a chunk seam.
        for (int x = 18; x <= 30; x++) city.Build(x, 5, TileKind.Road);
        for (int z = 6; z <= 10; z++) city.Build(25, z, TileKind.Road);
        city.Build(21, 4, TileKind.Water); city.Build(21, 6, TileKind.Water);
        city.Build(29, 9, TileKind.Road);
        var chunks = city.Chunks.Values.ToArray();
        var traffic = new TrafficRenderer();
        var seen = new HashSet<(int X, int Z)>();
        for (int frame = 0; frame <= 1200; frame++)
        {
            traffic.Update(city, chunks, 18, 30, 4, 10, frame / 30.0);
            seen.UnionWith(traffic.Positions);
        }
        check(city.PlacedCells().Where(c => c.Tile.Kind == TileKind.Road).All(c => seen.Contains((c.X, c.Z))),
            "Traffic reaches every added road, bridge, isolated tile and chunk seam without inlet service");
        var paused = traffic.Positions.ToArray();
        traffic.Update(city, chunks, 18, 30, 4, 10, 40);
        check(paused.SequenceEqual(traffic.Positions), "Paused traffic retains its vehicles");
        city.Build(25, 5, TileKind.Bulldoze);
        traffic.Update(city, chunks, 18, 30, 4, 10, 40.1);
        check(!traffic.Positions.Contains((25, 5)), "Bulldozed road immediately loses its traffic");
        traffic.Update(city, chunks, 0, 2, 0, 2, 40.2);
        check(traffic.Count == 0, "Offscreen traffic is discarded even within a visible chunk");
        traffic.Update(City.NewEmpty(), [], 0, 23, 0, 23, 40.3);
        check(traffic.Count == 0, "New or loaded towns discard prior traffic");

        var large = City.NewEmpty(); large.Money = 10_000_000;
        for (int x = 1; x <= 5; x++) large.Purchase(new(x, 0));
        foreach (var chunk in large.Chunks.Values)
            for (int index = 0; index < City.Size * City.Size; index++) chunk.Cells[index] = new() { Kind = TileKind.Road };
        var largeChunks = large.Chunks.Values.ToArray();
        traffic.Update(large, largeChunks, 0, 143, 0, 23, 0);
        check(traffic.Count == TrafficRenderer.MaxCars, "Dense visible roads respect the fixed traffic budget");
    }
}
