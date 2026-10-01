namespace GrowUpTown;

// Chosen only at construction and serialized with the anchor, including orientation.
internal static class LogisticsAppearance
{
    internal const int Count = 16;
    internal static bool IsValid(Tile t) => t.Kind != TileKind.LogisticsCenter
        || (t.LogisticsStyle is >= 0 and < Count && t.LogisticsRotation is >= 0 and < 4 && t.Growth == 0);

    internal static void Assign(City city, int x, int z)
    {
        int[] nearby = new int[Count], touching = new int[Count], roads = new int[4];
        int water = 0, homes = 0, shops = 0, industry = 0, green = 0;
        var seen = new HashSet<(int, int)>();
        for (int dz = -6; dz <= 9; dz++) for (int dx = -6; dx <= 9; dx++)
        {
            if (dx is >= 0 and < 4 && dz is >= 0 and < 4) continue;
            Tile t = city[x + dx, z + dz];
            if (t.Kind == TileKind.LogisticsCenter)
            {
                var a = city.AnchorAt(x + dx, z + dz);
                if (!seen.Add(a)) continue;
                int id = city[a.X, a.Z].LogisticsStyle;
                nearby[id]++;
                if (Math.Abs(a.X - x) <= 4 && Math.Abs(a.Z - z) <= 4) touching[id]++;
            }
            if (dx < -2 || dx > 5 || dz < -2 || dz > 5) continue;
            if (t.Kind == TileKind.Water) water++;
            if (City.HasHomes(t.Kind)) homes++;
            if (t.Kind is TileKind.Shop or TileKind.SuperMixed or TileKind.SuperIndustryCommerce or TileKind.UltraCity or TileKind.Supermarket) shops++;
            if (t.Kind is TileKind.Factory or TileKind.SuperFactory or TileKind.SuperIndustryCommerce or TileKind.UltraCity) industry++;
            if (t.Kind is TileKind.Park or TileKind.GrandPark || (t.Kind == TileKind.Empty && city.Inside(x + dx, z + dz) && city.HasNaturalTree(x + dx, z + dz))) green++;
            if (t.Kind == TileKind.Road)
            {
                if (dz == 4 && dx is >= 0 and < 4) roads[0]++;
                if (dx == 4 && dz is >= 0 and < 4) roads[1]++;
                if (dz == -1 && dx is >= 0 and < 4) roads[2]++;
                if (dx == -1 && dz is >= 0 and < 4) roads[3]++;
            }
        }
        int[] affinity = [industry, industry, shops, water, green, industry, shops, homes,
            industry, shops, water, green, industry, shops, homes, green];
        int best = 0; double bestScore = double.NegativeInfinity;
        for (int id = 0; id < Count; id++)
        {
            uint noise = unchecked((uint)city.AppearanceSeed ^ (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)id * 2654435761u);
            noise ^= noise >> 16; noise *= 2246822519u; noise ^= noise >> 13;
            double score = -touching[id] * 10000 - nearby[id] * 100 + Math.Min(affinity[id], 20) * .15 + (noise % 10000) / 10000.0;
            if (score > bestScore) { bestScore = score; best = id; }
        }
        city[x, z].LogisticsStyle = best;
        city[x, z].LogisticsRotation = Array.IndexOf(roads, roads.Max());
    }
}
