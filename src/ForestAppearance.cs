namespace GrowUpTown;

internal static class ForestAppearance
{
    internal const int Count = 16;
    internal static bool IsValid(Tile t) => t.Kind != TileKind.Forest
        || (t.ForestStyle is >= 0 and < Count && t.ForestRotation is >= 0 and < 4 && t.Growth == 0);
    internal static uint Hash(int seed, int x, int z, int salt = 0)
    {
        uint h = unchecked((uint)seed ^ (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)salt * 2654435761u);
        h ^= h >> 16; h *= 2246822519u; return h ^ (h >> 13);
    }
    internal static void Assign(City city, int x, int z)
    {
        int[] usage = new int[Count], touching = new int[Count];
        for (int dz = -3; dz <= 3; dz++) for (int dx = -3; dx <= 3; dx++)
        {
            if (dx == 0 && dz == 0) continue;
            var t = city[x + dx, z + dz];
            if (t.Kind != TileKind.Forest) continue;
            usage[t.ForestStyle]++;
            if (Math.Abs(dx) + Math.Abs(dz) == 1) touching[t.ForestStyle]++;
        }
        double best = double.NegativeInfinity;
        for (int style = 0; style < Count; style++)
        {
            double score = -touching[style] * 1000 - usage[style] * 10 + Hash(city.AppearanceSeed, x, z, style) % 1000 / 1000.0;
            if (score > best) { best = score; city[x, z].ForestStyle = style; }
        }
        city[x, z].ForestRotation = (int)(Hash(city.AppearanceSeed, x, z, 31) % 4);
    }
}
