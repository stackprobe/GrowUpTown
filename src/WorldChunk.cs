using System.IO.Compression;
using System.Text.Json;

namespace GrowUpTown;

public readonly record struct ChunkPos(int X, int Z) : IComparable<ChunkPos>
{
    public int CompareTo(ChunkPos other) => Z != other.Z ? Z.CompareTo(other.Z) : X.CompareTo(other.X);
    public static int Divide(int value) => (int)Math.Floor(value / (double)City.Size);
    public static ChunkPos FromTile(int x, int z) => new(Divide(x), Divide(z));
    public int OriginX => X * City.Size;
    public int OriginZ => Z * City.Size;
}

public sealed class WorldChunk
{
    public ChunkPos Position { get; }
    internal long TerrainRevision { get; set; }
    public int LastSimulatedMonth { get; internal set; }
    public int PurchasedMonth { get; }
    private SortedDictionary<int, Tile>? cells = new();
    private byte[]? packed;
    internal ChunkStats Stats { get; private set; }
    public bool IsPacked => cells is null;
    private byte[]? networkPayload;
    internal WorldChunk NetworkSnapshot()
    {
        var copy = new WorldChunk(Position, PurchasedMonth) { LastSimulatedMonth = LastSimulatedMonth };
        // Packed bytes are immutable; the worker decompresses its own copy of the data.
        if (cells is null) { copy.cells = null; copy.packed = packed; }
        else copy.cells = new(cells.ToDictionary(p => p.Key, p => p.Value.Snapshot()));
        return copy;
    }
    internal void PrepareNetworkPayload()
    {
        using var destination = new MemoryStream();
        using (var zip = new BrotliStream(destination, CompressionLevel.Fastest, true)) JsonSerializer.Serialize(zip, Cells);
        networkPayload = destination.ToArray();
    }
    internal void ApplyNetwork(WorldChunk result)
    {
        if (IsPacked && LastSimulatedMonth == result.LastSimulatedMonth && result.networkPayload is not null)
        { packed = result.networkPayload; Stats = result.Stats; return; }
        foreach (var (index, tile) in Cells)
            if (result.Cells.TryGetValue(index, out var source)) tile.ApplyNetwork(source);
        // Use live levels and residents, which may have progressed during the worker job.
        Recount();
    }
    internal SortedDictionary<int, Tile> Cells
    {
        get
        {
            if (cells is not null) return cells;
            using var source = new MemoryStream(packed!);
            using var unzip = new BrotliStream(source, CompressionMode.Decompress);
            cells = JsonSerializer.Deserialize<SortedDictionary<int, Tile>>(unzip)!;
            packed = null;
            return cells;
        }
    }
    public WorldChunk(ChunkPos position, int month)
    {
        Position = position; PurchasedMonth = month; LastSimulatedMonth = month - 1;
    }
    internal void Pack()
    {
        if (cells is null || cells.Count == 0) return;
        using var destination = new MemoryStream();
        using (var zip = new BrotliStream(destination, CompressionLevel.Fastest, true)) JsonSerializer.Serialize(zip, cells);
        packed = destination.ToArray(); cells = null;
    }
    internal void Recount()
    {
        long population = 0, capacity = 0, jobs = 0, income = 0, expense = 0;
        int homes = 0, comfort = 0, disconnected = 0;
        foreach (var t in Cells.Values)
        {
            if (!t.IsAnchor) continue;
            if ((FacadeStyles.IsBuilding(t.Kind) || LandmarkAppearance.IsLandmark(t.Kind) || t.Kind is TileKind.Supermarket or TileKind.LogisticsCenter) && !t.Connected) disconnected++;
            population += t.Residents;
            if (City.HasHomes(t.Kind)) { homes++; comfort += t.Comfort; }
            if (t.Connected)
            {
                if (City.HasHomes(t.Kind)) capacity += City.Housing(t);
                if (t.Kind == TileKind.UltraCity) { jobs += t.Level * 240; income += t.Level * 1560; }
                if (t.Kind == TileKind.SuperMixed) { jobs += t.Level * 64; income += t.Level * 400; }
                if (t.Kind == TileKind.SuperFactory) { jobs += t.Level * 128; income += t.Level * 640; }
                // Industrial production (128 jobs) plus commerce/distribution (64 jobs).
                if (t.Kind == TileKind.SuperIndustryCommerce) { jobs += t.Level * 192; income += t.Level * 1040; }
                if (t.Kind == TileKind.Shop) { jobs += t.Level * 16; income += t.Level * 100; }
                if (t.Kind == TileKind.Factory) { jobs += t.Level * 32; income += t.Level * 160; }
                if (t.Kind == TileKind.Supermarket) { jobs += 64; income += 400; }
                if (t.MallAccess) income += City.CommercialTaxBase(t) * 3 / 10;
                if (t.AmusementAccess) income += City.CommercialTaxBase(t) * 35 / 100;
                if (t.StadiumAccess) income += City.CommercialTaxBase(t) * 2 / 5;
                if (t.CasinoAccess) income += City.CommercialTaxBase(t) * 45 / 100;
                if (t.ExpoAccess) income += City.CommercialTaxBase(t) / 2;
                if (t.ArsenalAccess) income += City.IndustrialTaxBase(t) / 2;
                if (t.EnergyAccess) income += City.IndustrialTaxBase(t) * 45 / 100;
                if (t.ShipyardAccess) income += City.IndustrialTaxBase(t) * 2 / 5;
                if (t.LaunchAccess) income += City.IndustrialTaxBase(t) * 35 / 100;
                if (t.SpaceAccess) income += City.IndustrialTaxBase(t) * 3 / 10;
                if (t.FerrisWheelAccess) income += City.CommercialTaxBase(t) / 4;
                if (t.RoboticsAccess) income += City.IndustrialTaxBase(t) / 4;
                if (t.RailAccess) income += City.CommercialTaxBase(t) / 5;
                if (t.MarketAccess) income += City.CommercialTaxBase(t) / 5;
                if (t.Kind == TileKind.LogisticsCenter) { jobs += 96; income += 600; }
                if (t.LogisticsAccess) income += (City.CommercialTaxBase(t) + City.IndustrialTaxBase(t)) / 5;
            }
            expense += (t.RailCrossing ? 3 : 0) + (t.Kind switch { TileKind.MarineResort or TileKind.ExpoVenue or TileKind.MechaArsenal or TileKind.SafariPark or TileKind.CasinoResort or TileKind.FutureEnergyBase or TileKind.HotSpringResort or TileKind.Stadium or TileKind.Shipyard or TileKind.Zoo or TileKind.AmusementPark or TileKind.RocketLaunchSite or TileKind.ConcertHall or TileKind.ShoppingMall or TileKind.SpaceCenter or TileKind.Aquarium or TileKind.FerrisWheel or TileKind.RoboticsLab => LandmarkAppearance.Maintenance(t.Kind), TileKind.Forest => 18, TileKind.Rail => 3, TileKind.Station => 100, TileKind.LogisticsCenter => 180, TileKind.Supermarket => 160, TileKind.GrandPark => 120, TileKind.UltraCity => 150 * t.Level, TileKind.SuperHome => 40 * t.Level, TileKind.SuperIndustryCommerce => 88 * t.Level, TileKind.SuperMixed => 52 * t.Level, TileKind.SuperFactory => 56 * t.Level, TileKind.Road => 2, TileKind.Home => 5 * t.Level, TileKind.Shop => 8 * t.Level, TileKind.Factory => 14 * t.Level, TileKind.Park => 12, TileKind.Water => 12, _ => 0 });
        }
        Stats = new(population, capacity, jobs, income, expense, homes, comfort, Cells.Count, disconnected);
    }
}

internal readonly record struct ChunkStats(long Population, long Capacity, long Jobs, long BusinessIncome, long Expense, int Homes, int Comfort, int Placed, int DisconnectedBuildings);
