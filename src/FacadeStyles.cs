namespace GrowUpTown;

// Simulation-only appearance decisions. Rendering never reads the neighborhood.
internal static class FacadeStyles
{
    internal const int Count = 16;
    [Flags]
    private enum Setting { None = 0, Street = 1, Green = 2, Open = 4, Dense = 8, Industry = 16 }

    // Each entry corresponds to the same numbered drawing in BuildingFacades.
    private static readonly Setting[][] Affinities =
    [
        [Setting.Open, Setting.Green, Setting.Green, Setting.Open, Setting.Street, Setting.Dense, Setting.Green, Setting.Open,
         Setting.Street, Setting.Dense, Setting.Open, Setting.Green, Setting.Dense, Setting.Street, Setting.Open, Setting.Industry],
        [Setting.Street, Setting.Street, Setting.Green, Setting.Dense, Setting.Open, Setting.Street, Setting.Green, Setting.Dense,
         Setting.Street, Setting.Open, Setting.Dense, Setting.Green, Setting.Street, Setting.Dense, Setting.Open, Setting.Industry],
        [Setting.Industry, Setting.Open, Setting.Dense, Setting.Street, Setting.Industry, Setting.Open, Setting.Dense, Setting.Green,
         Setting.Street, Setting.Industry, Setting.Dense, Setting.Open, Setting.Street, Setting.Industry, Setting.Green, Setting.Open]
    ];

    internal static bool IsBuilding(TileKind kind) => kind is TileKind.Home or TileKind.Shop or TileKind.Factory;
    internal static bool IsValid(Tile tile) => tile.FaceStyles is not null
        && tile.FaceStyles.Length == (IsBuilding(tile.Kind) ? tile.Level * 4 : 0)
        && tile.FaceStyles.All(id => id >= 0 && id < Count);

    internal static void CompleteFloors(City city, int x, int z, int? builtMonth = null)
    {
        Tile tile = city[x, z];
        if (!IsBuilding(tile.Kind) || tile.FaceStyles.Length == tile.Level * 4) return;
        int[] usage = city.AppearanceUsage(tile.Kind);
        var styles = tile.FaceStyles.ToList();
        for (int index = styles.Count; index < tile.Level * 4; index++)
        {
            int floor = index / 4, face = index % 4;
            Setting setting = Neighborhood(city, x, z, face, floor);
            int minimum = usage.Min(), best = -1;
            double bestScore = double.NegativeInfinity;
            for (int id = 0; id < Count; id++)
            {
                // Hard balancing keeps even a homogeneous neighborhood varied.
                if (usage[id] != minimum) continue;
                double score = Noise(city.AppearanceSeed, x, z, builtMonth ?? city.Month, index, id);
                if ((Affinities[(int)tile.Kind - (int)TileKind.Home][id] & setting) != 0) score += .65;
                if (styles.Skip(floor * 4).Contains(id)) score -= 2;
                if (floor > 0 && styles[index - 4] == id) score -= .8;
                if (score > bestScore) { bestScore = score; best = id; }
            }
            styles.Add(best);
            usage[best]++;
        }
        city.RegisterFaces(tile.Kind, styles.Skip(tile.FaceStyles.Length));
        tile.FaceStyles = styles.ToArray();
    }

    private static Setting Neighborhood(City city, int x, int z, int face, int floor)
    {
        (int nx, int nz) = face switch { 0 => (0, 1), 1 => (1, 0), 2 => (0, -1), _ => (-1, 0) };
        Setting result = Setting.None;
        int ax = x + nx, az = z + nz;
        if (!city.Inside(ax, az) || city[ax, az].Kind is TileKind.Empty or TileKind.Park or TileKind.Water || city[ax, az].Level <= floor)
            result |= Setting.Open;
        if (city.Inside(ax, az) && city[ax, az].Kind == TileKind.Road) result |= Setting.Street;
        for (int dz = -2; dz <= 2; dz++) for (int dx = -2; dx <= 2; dx++)
        {
            if (dx * nx + dz * nz <= 0 || Math.Abs(dx) + Math.Abs(dz) > 3 || !city.Inside(x + dx, z + dz)) continue;
            var nearby = city[x + dx, z + dz];
            if (nearby.Kind is TileKind.Park or TileKind.Water) result |= Setting.Green;
            if (nearby.Kind == TileKind.Factory) result |= Setting.Industry;
            if (IsBuilding(nearby.Kind) && nearby.Level > floor) result |= Setting.Dense;
        }
        return result;
    }

    private static double Noise(int seed, int x, int z, int month, int faceIndex, int style)
    {
        // Explicit integer mixing is stable across processes and .NET versions.
        unchecked
        {
            uint value = (uint)seed ^ (uint)x * 0x9e3779b9u ^ (uint)z * 0x85ebca6bu
                ^ (uint)month * 0xc2b2ae35u ^ (uint)faceIndex * 0x27d4eb2fu ^ (uint)style * 0x165667b1u;
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
            return value / (double)uint.MaxValue;
        }
    }
}
