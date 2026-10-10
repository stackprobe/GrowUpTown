namespace GrowUpTown;

public sealed partial class City
{
    private static bool CanReplace(Tile tile, TileKind kind) => tile.Kind == TileKind.Empty
        || (kind is TileKind.Road or TileKind.Rail && tile.Kind == TileKind.Water)
        || (kind == TileKind.Rail && tile.Kind == TileKind.Road && !tile.RailCrossing)
        || (kind == TileKind.Road && tile.Kind == TileKind.Rail);
    public static bool IsRail(Tile tile) => tile.Kind == TileKind.Rail || tile.RailCrossing;
    // Side order matches rail masks: +Z, +X, -Z, -X.
    internal static (int X, int Z) StationSideCell(int x, int z, int side, int index) => side switch
    {
        0 => (x + index, z + 2), 1 => (x + 2, z + index),
        2 => (x + index, z - 1), 3 => (x - 1, z + index),
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };
    public int StationTrackSides(int x, int z)
    {
        int mask = 0;
        for (int side = 0; side < 4; side++)
        {
            var a = StationSideCell(x, z, side, 0); var b = StationSideCell(x, z, side, 1);
            int straight = side % 2 == 0 ? 10 : 5;
            if (IsRail(this[a.X, a.Z]) && IsRail(this[b.X, b.Z])
                && RailMask(a.X, a.Z) == straight && RailMask(b.X, b.Z) == straight) mask |= 1 << side;
        }
        return mask;
    }
    public int RailMask(int x, int z)
    {
        // Station service uses adjacency; buildings never create a visible rail branch.
        bool Connect(int nx, int nz) => IsRail(this[nx, nz]);
        return (Connect(x, z + 1) ? 1 : 0) | (Connect(x + 1, z) ? 2 : 0)
            | (Connect(x, z - 1) ? 4 : 0) | (Connect(x - 1, z) ? 8 : 0);
    }
    public bool HasRailService(int x, int z) => HasService(x, z, TileKind.Station);
    private void ReconnectRail()
    {
        var visited = new HashSet<(int X, int Z, int Lane)>();
        var cells = PlacedCells().ToArray();
        foreach (var (_, _, tile) in cells) tile.RailActive = false;
        // A station joins tracks on its perimeter, but cannot connect directly to another station.
        foreach (var (x, z, tile) in cells)
        for (int lane = 0; lane < (RailMask(x, z) == 15 ? 2 : 1); lane++)
        {
            if (!IsRail(tile) || !visited.Add((x, z, lane))) continue;
            var stations = new HashSet<(int X, int Z)>();
            var queue = new Queue<(int X, int Z, int Lane)>();
            queue.Enqueue((x, z, lane));
            while (queue.TryDequeue(out var p))
                foreach (var n in Neighbors(p.X, p.Z))
                {
                    int direction = n.x > p.X ? 1 : n.x < p.X ? 3 : n.z > p.Z ? 0 : 2;
                    if (IsRail(this[p.X, p.Z]) && RailMask(p.X, p.Z) == 15 && direction % 2 != p.Lane) continue;
                    bool station = this[n.x, n.z].Kind == TileKind.Station;
                    bool canEnter = IsRail(this[n.x, n.z]) || (station &&
                        (IsRail(this[p.X, p.Z]) || AnchorAt(p.X, p.Z) == AnchorAt(n.x, n.z)));
                    if (!canEnter) continue;
                    if (station) stations.Add(AnchorAt(n.x, n.z));
                    var next = (n.x, n.z, IsRail(this[n.x, n.z]) && RailMask(n.x, n.z) == 15 ? direction % 2 : 0);
                    if (visited.Add(next)) queue.Enqueue(next);
                }
            if (stations.Count >= 2)
                foreach (var station in stations) this[station.X, station.Z].RailActive = true;
        }
    }
}

internal static class StationAppearance
{
    internal const int Count = 16;
    internal static bool IsValid(Tile t) => t.Kind != TileKind.Station
        || (t.StationStyle is >= 0 and < Count && t.StationRotation is >= 0 and < 4
            && (t.StationEntranceSide is null or >= 0 and < 4) && t.Growth == 0);
    internal static void Assign(City city, int x, int z)
    {
        int[] usage = new int[16], environment = new int[4];
        var seen = new HashSet<(int, int)>();
        for (int dz = -6; dz <= 7; dz++) for (int dx = -6; dx <= 7; dx++)
        {
            if (dx is >= 0 and < 2 && dz is >= 0 and < 2) continue;
            var t = city[x + dx, z + dz];
            if (t.Kind == TileKind.Station && seen.Add(city.AnchorAt(x + dx, z + dz)))
                usage[city.BuildingAt(x + dx, z + dz).StationStyle]++;
            if (Math.Abs(dx) > 3 || Math.Abs(dz) > 3) continue;
            if (t.Kind == TileKind.Water) environment[1]++;
            else if (City.CommercialTaxBase(t) > 0) environment[2]++;
            else if (City.IndustrialTaxBase(t) > 0) environment[3]++;
            else if (t.Kind == TileKind.Forest || City.IsPark(t.Kind) || City.HasHomes(t.Kind)) environment[0]++;
        }
        double best = double.NegativeInfinity;
        for (int id = 0; id < Count; id++)
        {
            uint noise = unchecked((uint)city.AppearanceSeed ^ (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)id * 2654435761u);
            noise ^= noise >> 16; noise *= 2246822519u; noise ^= noise >> 13;
            double score = -usage[id] * 1000 + Math.Min(20, environment[id / 4]) + noise % 1000 / 1000.0;
            if (score > best) { best = score; city[x, z].StationStyle = id; }
        }
        int[] roads = new int[4];
        for (int side = 0; side < 4; side++) for (int i = 0; i < 2; i++)
        {
            var p = City.StationSideCell(x, z, side, i);
            if (city[p.X, p.Z].Kind == TileKind.Road) roads[side]++;
        }
        int trackSides = city.StationTrackSides(x, z);
        int platform = 0, bestAccess = -1;
        for (int side = 0; side < 4; side++)
        {
            if ((trackSides & (1 << side)) == 0) continue;
            int score = roads[(side + 2) % 4] * 10 + roads[(side + 1) % 4] + roads[(side + 3) % 4];
            if (score > bestAccess) { bestAccess = score; platform = side; }
        }
        int entrance = (platform + 2) % 4, bestRoad = -1;
        for (int side = 0; side < 4; side++)
        {
            if (roads[side] == 0) continue;
            int score = roads[side] * 10 + (side != platform ? 100 : 0) + (side == (platform + 2) % 4 ? 30 : 0);
            if (score > bestRoad) { bestRoad = score; entrance = side; }
        }
        city[x, z].StationRotation = platform;
        city[x, z].StationEntranceSide = entrance;
    }
}
