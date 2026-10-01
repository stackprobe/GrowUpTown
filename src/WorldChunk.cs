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
            if ((FacadeStyles.IsBuilding(t.Kind) || t.Kind is TileKind.Supermarket or TileKind.LogisticsCenter) && !t.Connected) disconnected++;
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
                if (t.MarketAccess) income += City.CommercialTaxBase(t) / 5;
                if (t.Kind == TileKind.LogisticsCenter) { jobs += 96; income += 600; }
                if (t.LogisticsAccess) income += (City.CommercialTaxBase(t) + City.IndustrialTaxBase(t)) / 5;
            }
            expense += t.Kind switch { TileKind.LogisticsCenter => 180, TileKind.Supermarket => 160, TileKind.GrandPark => 120, TileKind.UltraCity => 150 * t.Level, TileKind.SuperHome => 40 * t.Level, TileKind.SuperIndustryCommerce => 88 * t.Level, TileKind.SuperMixed => 52 * t.Level, TileKind.SuperFactory => 56 * t.Level, TileKind.Road => 2, TileKind.Home => 5 * t.Level, TileKind.Shop => 8 * t.Level, TileKind.Factory => 14 * t.Level, TileKind.Park => 12, TileKind.Water => 12, _ => 0 };
        }
        Stats = new(population, capacity, jobs, income, expense, homes, comfort, Cells.Count, disconnected);
    }
}

internal readonly record struct ChunkStats(long Population, long Capacity, long Jobs, long BusinessIncome, long Expense, int Homes, int Comfort, int Placed, int DisconnectedBuildings);
