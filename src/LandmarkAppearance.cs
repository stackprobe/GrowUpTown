namespace GrowUpTown;

// Appearance is chosen once, stored on the anchor and never changed by rendering.
internal static class LandmarkAppearance
{
    internal const int Count = 16;
    internal static bool IsTenCellLandmark(TileKind kind) => kind is TileKind.MarineResort or TileKind.ExpoVenue or TileKind.MechaArsenal;
    internal static bool IsNineCellLandmark(TileKind kind) => kind is TileKind.SafariPark or TileKind.CasinoResort or TileKind.FutureEnergyBase;
    internal static bool IsMegaLandmark(TileKind kind) => kind is TileKind.HotSpringResort or TileKind.Stadium or TileKind.Shipyard;
    internal static bool IsGiantLandmark(TileKind kind) => kind is TileKind.Zoo or TileKind.AmusementPark or TileKind.RocketLaunchSite;
    internal static bool IsLargeLandmark(TileKind kind) => kind is TileKind.ConcertHall or TileKind.ShoppingMall or TileKind.SpaceCenter;
    internal static bool IsLandmark(TileKind kind) => IsTenCellLandmark(kind) || IsNineCellLandmark(kind) || IsMegaLandmark(kind) || IsGiantLandmark(kind) || IsLargeLandmark(kind) || kind is TileKind.Aquarium or TileKind.FerrisWheel or TileKind.RoboticsLab;
    internal static bool IsValid(Tile tile) => !IsLandmark(tile.Kind)
        || (tile.LandmarkStyle is >= 0 and < Count && tile.LandmarkRotation is >= 0 and < 4 && tile.Growth == 0);
    internal static int Range(TileKind kind) => City.Footprint(kind);
    internal static string Effect(TileKind kind) => kind switch
    {
        TileKind.MarineResort => "住宅の住環境＋25",
        TileKind.ExpoVenue => "商業税収＋50％",
        TileKind.MechaArsenal => "工業税収＋50％",
        TileKind.SafariPark => "住宅の住環境＋25",
        TileKind.CasinoResort => "商業税収＋45％",
        TileKind.FutureEnergyBase => "工業税収＋45％",
        TileKind.HotSpringResort => "住宅の住環境＋25",
        TileKind.Stadium => "商業税収＋40％",
        TileKind.Shipyard => "工業税収＋40％",
        TileKind.Zoo => "住宅の住環境＋25",
        TileKind.AmusementPark => "商業税収＋35％",
        TileKind.RocketLaunchSite => "工業税収＋35％",
        TileKind.ConcertHall => "住宅の住環境＋25",
        TileKind.ShoppingMall => "商業税収＋30％",
        TileKind.SpaceCenter => "工業税収＋30％",
        TileKind.Aquarium => "住宅の住環境＋20",
        TileKind.FerrisWheel => "商業税収＋25％",
        TileKind.RoboticsLab => "工業税収＋25％",
        _ => ""
    };
    internal static int Maintenance(TileKind kind) => kind switch
    {
        TileKind.MarineResort => 1890, TileKind.ExpoVenue => 2160, TileKind.MechaArsenal => 2520, TileKind.SafariPark => 1260, TileKind.CasinoResort => 1440, TileKind.FutureEnergyBase => 1680, TileKind.HotSpringResort => 840, TileKind.Stadium => 960, TileKind.Shipyard => 1120, TileKind.Zoo => 560, TileKind.AmusementPark => 640, TileKind.RocketLaunchSite => 760,
        TileKind.Aquarium => 240, TileKind.FerrisWheel => 260, TileKind.RoboticsLab => 300, TileKind.ConcertHall => 360, TileKind.ShoppingMall => 420, TileKind.SpaceCenter => 480, _ => 0
    };
    internal static void Assign(City city, int x, int z)
    {
        Tile tile = city[x, z];
        int size = City.Footprint(tile.Kind);
        int[] nearby = new int[Count], touching = new int[Count], roads = new int[4];
        int water = 0, homes = 0, commerce = 0, industry = 0, green = 0;
        var seen = new HashSet<(int, int)>();
        for (int dz = -6; dz < size + 6; dz++) for (int dx = -6; dx < size + 6; dx++)
        {
            if (dx >= 0 && dx < size && dz >= 0 && dz < size) continue;
            var t = city[x + dx, z + dz];
            if (t.Kind == tile.Kind)
            {
                var a = city.AnchorAt(x + dx, z + dz);
                if (seen.Add(a))
                {
                    int style = city[a.X, a.Z].LandmarkStyle;
                    nearby[style]++;
                    if (Math.Abs(a.X - x) <= size && Math.Abs(a.Z - z) <= size) touching[style]++;
                }
            }
            if (dx < -2 || dx > size + 1 || dz < -2 || dz > size + 1) continue;
            if (t.Kind == TileKind.Water) water++;
            if (City.HasHomes(t.Kind)) homes++;
            if (City.CommercialTaxBase(t) > 0 || t.Kind == TileKind.Supermarket) commerce++;
            if (City.IndustrialTaxBase(t) > 0 || t.Kind == TileKind.LogisticsCenter) industry++;
            if (City.IsPark(t.Kind) || t.Kind == TileKind.Forest) green++;
            if (t.Kind != TileKind.Road) continue;
            if (dz == size && dx >= 0 && dx < size) roads[0]++;
            if (dx == size && dz >= 0 && dz < size) roads[1]++;
            if (dz == -1 && dx >= 0 && dx < size) roads[2]++;
            if (dx == -1 && dz >= 0 && dz < size) roads[3]++;
        }
        // Four design families: waterfront, garden, urban, research/industrial.
        int[] affinity = [water, green + homes, commerce, industry];
        int best = 0;
        (int Used, int Touching, int Usage, double Affinity) bestScore = (int.MinValue, 0, 0, 0);
        for (int id = 0; id < Count; id++)
        {
            double suitability = Math.Min(affinity[id / 4], 30) * .15
                + ForestAppearance.Hash(city.AppearanceSeed, x, z, id + (int)tile.Kind * 31) % 10000 / 10000.0;
            // Unused nearby styles win first; if exhausted, avoid adjacent matches,
            // then prefer less common styles. Context and stable noise break ties.
            var score = (nearby[id] == 0 ? 1 : 0, -touching[id], -nearby[id], suitability);
            if (score.CompareTo(bestScore) > 0) { best = id; bestScore = score; }
        }
        tile.LandmarkStyle = best;
        tile.LandmarkRotation = Array.IndexOf(roads, roads.Max());
    }
}
