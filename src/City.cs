using System.Text.Json;

namespace GrowUpTown;

public enum TileKind { Empty, Road, Home, Shop, Factory, Park, Bulldoze, Water, SuperMixed, SuperFactory, SuperIndustryCommerce, SuperHome, UltraCity, GrandPark, Supermarket, LogisticsCenter }
public sealed class Tile
{
    public TileKind Kind { get; set; }
    public int Level { get; set; } = 1;
    public int Residents { get; set; }
    public int LogisticsStyle { get; set; }
    public int LogisticsRotation { get; set; }
    public bool LogisticsAccess { get; set; }
    public int MarketStyle { get; set; }
    public int MarketRotation { get; set; }
    // Cached service state; preserved during cold-chunk compression, recomputed on load/edits.
    public bool MarketAccess { get; set; }
    public int ParkStyle { get; set; }
    public int ParkRotation { get; set; }
    public int Growth { get; set; }
    public bool Connected { get; set; }
    public int Comfort { get; set; }
    public int OffsetX { get; set; }
    public int OffsetZ { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsAnchor => OffsetX == 0 && OffsetZ == 0;
    // Four faces per completed floor: +Z, +X, -Z, -X.
    public int[] FaceStyles { get; set; } = [];
}

public sealed class City
{
    public const int Size = 24;
    public long LandPrice => 1_000_000L + (chunks.Count - 1L) * 500_000L;
    public const int DistantUpdateInterval = 12;
    public const int MaxChunkCoordinate = int.MaxValue / Size - 4;
    private readonly SortedDictionary<ChunkPos, WorldChunk> chunks = new();
    private HashSet<ChunkPos> active = [new(0, 0)];
    private readonly int[][] appearanceUsage = [new int[16], new int[16], new int[16]];
    private static readonly Tile Empty = new();
    public int AppearanceSeed { get; set; } = Random.Shared.Next(1, int.MaxValue);
    public IReadOnlyDictionary<ChunkPos, WorldChunk> Chunks => chunks;
    // Diagnostic enumeration only; rendering uses visible chunks.
    public IReadOnlyList<Tile> Tiles => chunks.Values.SelectMany(c => c.Cells.Values).ToArray();
    public long Money { get; set; } = 16000;
    public int Month { get; set; } = 1;
    private int taxRate = 10;
    public int TaxRate
    {
        get => taxRate;
        set { if (taxRate != value) SynchronizeAll(); taxRate = Math.Clamp(value, 5, 20); ReleaseInactive(); }
    }
    public long LastIncome { get; set; }
    public long LastExpense { get; set; }
    public int Happiness { get; set; } = 70;
    public long Population => chunks.Values.Sum(c => c.Stats.Population);
    public long Capacity => chunks.Values.Sum(c => c.Stats.Capacity);
    public long Jobs => chunks.Values.Sum(c => c.Stats.Jobs);
    public long Workers => Population * 55 / 100;
    public long Employed => Math.Min(Workers, Jobs);
    public long Balance => LastIncome - LastExpense;
    public static int Cost(TileKind kind) => kind switch { TileKind.LogisticsCenter => 8400, TileKind.Supermarket => 7200, TileKind.GrandPark => 5600, TileKind.UltraCity => 18000, TileKind.SuperHome => 4800, TileKind.SuperIndustryCommerce => 6200, TileKind.SuperMixed => 4280, TileKind.SuperFactory => 3600, TileKind.Road => 60, TileKind.Home => 420, TileKind.Shop => 650, TileKind.Factory => 900, TileKind.Park => 280, TileKind.Water => 280, TileKind.Bulldoze => 40, _ => 0 };
    public static string Name(TileKind kind) => kind switch { TileKind.LogisticsCenter => "物流センター", TileKind.Supermarket => "スーパーマーケット", TileKind.GrandPark => "大公園", TileKind.UltraCity => "ultra住宅地＆商業地＆工業地", TileKind.SuperHome => "super住宅地", TileKind.SuperIndustryCommerce => "super工業商業地", TileKind.SuperMixed => "super住宅商業地", TileKind.SuperFactory => "super工業地", TileKind.Road => "道路", TileKind.Home => "住宅", TileKind.Shop => "商業地", TileKind.Factory => "工業地", TileKind.Park => "公園", TileKind.Water => "水域", TileKind.Bulldoze => "撤去", _ => "空き地" };
    public static int Footprint(TileKind kind) => kind is TileKind.GrandPark or TileKind.Supermarket or TileKind.LogisticsCenter ? 4 : kind == TileKind.UltraCity ? 3 : kind is TileKind.SuperHome or TileKind.SuperMixed or TileKind.SuperFactory or TileKind.SuperIndustryCommerce ? 2 : 1;
    public static int MaxLevel(TileKind kind) => kind is TileKind.GrandPark or TileKind.Supermarket or TileKind.LogisticsCenter ? 1 : kind == TileKind.UltraCity ? 9 : Footprint(kind) == 2 ? 6 : 3;
    public static bool HasHomes(TileKind kind) => kind is TileKind.Home or TileKind.SuperMixed or TileKind.SuperHome or TileKind.UltraCity;
    public static int Housing(Tile tile) => tile.Level * (tile.Kind == TileKind.UltraCity ? 432 : tile.Kind == TileKind.SuperHome ? 192 : tile.Kind == TileKind.SuperMixed ? 96 : tile.Kind == TileKind.Home ? 24 : 0);
    public static int UnlockYear(TileKind kind) => kind switch
    {
        TileKind.SuperMixed or TileKind.SuperFactory or TileKind.SuperIndustryCommerce or TileKind.SuperHome => 5,
        TileKind.UltraCity => 10,
        TileKind.GrandPark or TileKind.Supermarket or TileKind.LogisticsCenter => 15,
        _ => 1
    };
    public bool IsUnlocked(TileKind kind) => (Month - 1) / 12 + 1 >= UnlockYear(kind);
    public bool SpecialsUnlocked => IsUnlocked(TileKind.GrandPark);
    public static bool IsPark(TileKind kind) => kind is TileKind.Park or TileKind.GrandPark;
    public (int X, int Z) AnchorAt(int x, int z) => (x - this[x, z].OffsetX, z - this[x, z].OffsetZ);
    public Tile BuildingAt(int x, int z) { var p = AnchorAt(x, z); return this[p.X, p.Z]; }
    public bool CanBuild(int x, int z, TileKind kind)
    {
        if (kind is < TileKind.Road or > TileKind.LogisticsCenter || !IsUnlocked(kind) || Money < Cost(kind) || !Inside(x, z)) return false;
        if (kind == TileKind.Bulldoze) return this[x, z].Kind != TileKind.Empty;
        for (int dz = 0; dz < Footprint(kind); dz++) for (int dx = 0; dx < Footprint(kind); dx++)
            if (!Inside(x + dx, z + dz) || (this[x + dx, z + dz].Kind != TileKind.Empty && !(kind == TileKind.Road && this[x + dx, z + dz].Kind == TileKind.Water))) return false;
        return true;
    }
    public bool Inside(int x, int z) => chunks.ContainsKey(ChunkPos.FromTile(x, z));
    // 1: east/west deck, 2: north/south deck. Recomputed when neighbors change.
    public int BridgeAxis(int x, int z)
    {
        if (this[x, z].Kind != TileKind.Road) return 0;
        int mask = RoadMask(x, z);
        if (mask == 10 && this[x, z - 1].Kind == TileKind.Water && this[x, z + 1].Kind == TileKind.Water) return 1;
        if (mask == 5 && this[x - 1, z].Kind == TileKind.Water && this[x + 1, z].Kind == TileKind.Water) return 2;
        return 0;
    }
    public bool HasWater(int x, int z) => this[x, z].Kind == TileKind.Water || BridgeAxis(x, z) != 0;
    // +Z, +X, -Z, -X. Derived from neighbors, so editing either tile updates it.
    public int RoadMask(int x, int z) => (this[x, z + 1].Kind == TileKind.Road ? 1 : 0)
        | (this[x + 1, z].Kind == TileKind.Road ? 2 : 0)
        | (this[x, z - 1].Kind == TileKind.Road ? 4 : 0)
        | (this[x - 1, z].Kind == TileKind.Road ? 8 : 0);
    public Tile this[int x, int z]
    {
        get
        {
            var key = ChunkPos.FromTile(x, z);
            return chunks.TryGetValue(key, out var chunk) && chunk.Cells.TryGetValue(LocalIndex(key, x, z), out var tile) ? tile : Empty;
        }
    }
    private static int LocalIndex(ChunkPos key, int x, int z) => (z - key.OriginZ) * Size + x - key.OriginX;
    public IEnumerable<(int x, int z)> Neighbors(int x, int z)
    {
        if (Inside(x - 1, z)) yield return (x - 1, z);
        if (Inside(x + 1, z)) yield return (x + 1, z);
        if (Inside(x, z - 1)) yield return (x, z - 1);
        if (Inside(x, z + 1)) yield return (x, z + 1);
    }
    public City(bool starter = true)
    {
        chunks.Add(new(0, 0), new(new(0, 0), Month));
        if (!starter) return;
        void Place(int x, int z, TileKind kind, int residents = 0) => chunks[new(0, 0)].Cells[z * Size + x] = new Tile { Kind = kind, Residents = residents };
        for (int x = 0; x <= 16; x++) Place(x, 12, TileKind.Road);
        for (int z = 8; z <= 16; z++) Place(10, z, TileKind.Road);
        foreach (int x in new[] { 6, 7, 8, 9 }) Place(x, 11, TileKind.Home, 12);
        Place(8, 13, TileKind.Shop); Place(14, 11, TileKind.Factory); Place(6, 13, TileKind.Park);
        Reconnect();
        foreach (var (x, z, _) in PlacedCells()) FacadeStyles.CompleteFloors(this, x, z);
    }
    public static City NewEmpty() => new(false);
    internal IEnumerable<(int X, int Z, Tile Tile)> PlacedCells()
    {
        foreach (var chunk in chunks.Values)
            foreach (var (index, tile) in chunk.Cells)
                yield return (chunk.Position.OriginX + index % Size, chunk.Position.OriginZ + index / Size, tile);
    }
    internal int[] AppearanceUsage(TileKind kind) => (int[])appearanceUsage[FacadeStyles.Category(kind)].Clone();
    internal void RegisterFaces(TileKind kind, IEnumerable<int> ids, int sign = 1)
    {
        if (!FacadeStyles.IsBuilding(kind)) return;
        foreach (int id in ids) appearanceUsage[FacadeStyles.Category(kind)][id] += sign;
    }
    public bool CanPurchase(ChunkPos key) => Math.Abs((long)key.X) <= MaxChunkCoordinate && Math.Abs((long)key.Z) <= MaxChunkCoordinate
        && !chunks.ContainsKey(key) && (chunks.ContainsKey(new(key.X - 1, key.Z)) || chunks.ContainsKey(new(key.X + 1, key.Z))
        || chunks.ContainsKey(new(key.X, key.Z - 1)) || chunks.ContainsKey(new(key.X, key.Z + 1)));
    public string Purchase(ChunkPos key)
    {
        if (!CanPurchase(key)) return "所有地の上下左右に接する土地を選んでください";
        long price = LandPrice;
        if (Money < price) return $"土地の購入には ¥{price:N0} が必要です";
        Money -= price;
        chunks.Add(key, new(key, Month));
        InvalidateTerrain(key.OriginX, key.OriginZ, Size);
        return $"土地 [{key.X}, {key.Z}] を購入しました（24×24 / ¥{price:N0}）";
    }
    public void Reconnect()
    {
        var placed = PlacedCells().ToArray();
        foreach (var (_, _, tile) in placed) tile.Connected = false;
        var queue = new Queue<(int x, int z)>();
        if (this[0, 12].Kind == TileKind.Road) { this[0, 12].Connected = true; queue.Enqueue((0, 12)); }
        while (queue.TryDequeue(out var p))
            foreach (var n in Neighbors(p.x, p.z))
            {
                var tile = this[n.x, n.z];
                if (tile.Connected || tile.Kind == TileKind.Empty) continue;
                tile.Connected = true;
                if (tile.Kind == TileKind.Road) queue.Enqueue(n);
            }
        // Walkways carry access only between parks, never into another road network
        // or into residential/business buildings. Grand parks participate via all cells.
        foreach (var (x, z, tile) in placed)
            if (IsPark(tile.Kind) && tile.Connected) queue.Enqueue((x, z));
        while (queue.TryDequeue(out var park))
            foreach (var next in Neighbors(park.x, park.z))
            {
                var tile = this[next.x, next.z];
                if (!IsPark(tile.Kind) || tile.Connected) continue;
                tile.Connected = true;
                queue.Enqueue(next);
            }
        foreach (var (x, z, tile) in placed)
            if (!tile.IsAnchor && tile.Connected) BuildingAt(x, z).Connected = true;
        foreach (var (x, z, tile) in placed)
        {
            if (!tile.IsAnchor) tile.Connected = BuildingAt(x, z).Connected;
            if (tile.IsAnchor && HasHomes(tile.Kind)) tile.Comfort = LocalComfort(x, z);
            if (tile.IsAnchor) tile.MarketAccess = CommercialTaxBase(tile) > 0 && HasMarketService(x, z);
            if (tile.IsAnchor) tile.LogisticsAccess = CommercialTaxBase(tile) + IndustrialTaxBase(tile) > 0 && HasLogisticsService(x, z);
        }
        foreach (var chunk in chunks.Values) chunk.Recount();
    }
    public string Build(int x, int z, TileKind kind)
    {
        if (!IsUnlocked(kind)) return $"この項目は{UnlockYear(kind)}年1月に解放されます";
        if (!Inside(x, z)) return "未購入の土地には建設できません";
        var tile = this[x, z];
        if (kind is < TileKind.Road or > TileKind.LogisticsCenter) return "建設する種類を選択してください";
        if (kind == TileKind.Bulldoze && tile.Kind == TileKind.Empty) return "ここは空き地です";
        if (kind != TileKind.Bulldoze && tile.Kind != TileKind.Empty && !(kind == TileKind.Road && tile.Kind == TileKind.Water)) return "既存の建物を先に撤去してください";
        if (Money < Cost(kind)) return "資金が足りません。税収を待つか、支出を見直しましょう";
        if (!CanBuild(x, z, kind)) return $"建設には所有地内の空いた{Footprint(kind)}×{Footprint(kind)}マスが必要です";
        SynchronizeAll();
        if (kind == TileKind.Bulldoze) (x, z) = AnchorAt(x, z);
        tile = this[x, z];
        RegisterFaces(tile.Kind, tile.FaceStyles, -1);
        Money -= Cost(kind);
        int size = Footprint(kind == TileKind.Bulldoze ? tile.Kind : kind);
        for (int dz = 0; dz < size; dz++) for (int dx = 0; dx < size; dx++)
        {
            var key = ChunkPos.FromTile(x + dx, z + dz);
            int index = LocalIndex(key, x + dx, z + dz);
            if (kind == TileKind.Bulldoze) chunks[key].Cells.Remove(index);
            else chunks[key].Cells[index] = new Tile { Kind = kind, OffsetX = dx, OffsetZ = dz };
        }
        if (kind == TileKind.GrandPark) GrandParkAppearance.Assign(this, x, z);
        if (kind == TileKind.Supermarket) SupermarketAppearance.Assign(this, x, z);
        if (kind == TileKind.LogisticsCenter) LogisticsAppearance.Assign(this, x, z);
        InvalidateTerrain(x, z, Math.Max(3, size));
        Reconnect();
        FacadeStyles.CompleteFloors(this, x, z);
        bool connected = this[x, z].Connected;
        ReleaseInactive();
        return kind == TileKind.Bulldoze ? "撤去しました" : $"{Name(kind)}を建設しました" + (connected || kind == TileKind.Water ? "" : " / 最初の道路入口 [0, 12] へ接続してください");
    }
    public int LocalComfort(int x, int z)
    {
        int value = 0;
        for (int dz = -3; dz <= 3; dz++) for (int dx = -3; dx <= 3; dx++)
        {
            if (Math.Abs(dx) + Math.Abs(dz) > 3) continue;
            var t = this[x + dx, z + dz];
            if (t.Kind == TileKind.Park) value += t.Connected ? 10 : 3;
            if (t.Kind == TileKind.Water) value += 10;
            if (t.Kind is TileKind.Factory or TileKind.SuperFactory or TileKind.SuperIndustryCommerce) value -= 14;
        }
        // Count each park once, measuring from the entire residential footprint.
        int homeSize = HasHomes(this[x, z].Kind) ? Footprint(this[x, z].Kind) : 1;
        var parks = new HashSet<(int, int)>();
        for (int dz = -5; dz < homeSize + 5; dz++) for (int dx = -5; dx < homeSize + 5; dx++)
        {
            int distance = Math.Max(0, Math.Max(-dx, dx - homeSize + 1)) + Math.Max(0, Math.Max(-dz, dz - homeSize + 1));
            if (distance > 5 || this[x + dx, z + dz].Kind != TileKind.GrandPark) continue;
            var anchor = AnchorAt(x + dx, z + dz);
            if (parks.Add(anchor)) value += this[anchor.X, anchor.Z].Connected ? 20 : 6;
        }
        if (HasMarketService(x, z)) value += 10;
        return Math.Clamp(value, -35, 25);
    }
    // Commercial portion only: mixed-use industrial tax is never boosted.
    internal static int CommercialTaxBase(Tile tile) => tile.Level * (tile.Kind switch
    {
        TileKind.Shop => 100,
        TileKind.SuperMixed or TileKind.SuperIndustryCommerce => 400,
        TileKind.UltraCity => 600,
        _ => 0
    });
    internal static int IndustrialTaxBase(Tile tile) => tile.Level * (tile.Kind switch
    {
        TileKind.Factory => 160,
        TileKind.SuperFactory or TileKind.SuperIndustryCommerce => 640,
        TileKind.UltraCity => 960,
        _ => 0
    });
    public bool HasMarketService(int x, int z) => HasService(x, z, TileKind.Supermarket);
    public bool HasLogisticsService(int x, int z) => HasService(x, z, TileKind.LogisticsCenter);
    private bool HasService(int x, int z, TileKind source)
    {
        (x, z) = AnchorAt(x, z);
        int size = Footprint(this[x, z].Kind);
        for (int dz = -5; dz < size + 5; dz++) for (int dx = -5; dx < size + 5; dx++)
        {
            int distance = Math.Max(0, Math.Max(-dx, dx - size + 1)) + Math.Max(0, Math.Max(-dz, dz - size + 1));
            if (distance <= 5 && this[x + dx, z + dz].Kind == source
                && BuildingAt(x + dx, z + dz).Connected) return true;
        }
        return false;
    }
    private void InvalidateTerrain(int x, int z, int radius)
    {
        // A road edit can change a neighboring bridge and the bank beside it.
        var min = ChunkPos.FromTile(x - radius, z - radius);
        var max = ChunkPos.FromTile(x + radius, z + radius);
        for (int cz = min.Z; cz <= max.Z; cz++) for (int cx = min.X; cx <= max.X; cx++)
            if (chunks.TryGetValue(new(cx, cz), out var chunk)) chunk.TerrainRevision++;
    }
    private sealed class Conditions(City city)
    {
        public long Population = city.Population;
        public readonly long Target = Math.Min(city.Capacity, city.Jobs * 100 / 55 + 16);
        public readonly bool BusinessViable = city.Workers >= city.Jobs * .55;
        public readonly int Happiness = city.Happiness;
    }
    private void Advance(WorldChunk chunk, int throughMonth, Conditions conditions)
    {
        if (chunk.LastSimulatedMonth >= throughMonth) return;
        // Bounded catch-up: past offscreen months use the latest shared conditions.
        for (int month = chunk.LastSimulatedMonth + 1; month <= throughMonth; month++)
            foreach (var (index, tile) in chunk.Cells)
            {
                if (!tile.IsAnchor) continue;
                int before = tile.Residents;
                if (HasHomes(tile.Kind))
                {
                    if (!tile.Connected || conditions.Happiness < 40 || tile.Comfort <= -28) tile.Residents = Math.Max(0, tile.Residents - 4);
                    else if (conditions.Population < conditions.Target)
                        tile.Residents += (int)Math.Max(0, Math.Min(Math.Min(tile.Kind == TileKind.UltraCity ? 72 : tile.Kind == TileKind.SuperHome ? 32 : tile.Kind == TileKind.SuperMixed ? 16 : 4, conditions.Target - conditions.Population), Housing(tile) - tile.Residents));
                    else if (conditions.Population > conditions.Target + 8) tile.Residents = Math.Max(0, tile.Residents - 2);
                }
                conditions.Population += tile.Residents - before;
                if (!FacadeStyles.IsBuilding(tile.Kind)) continue;
                bool viable = tile.Connected && conditions.Happiness >= 60 && (HasHomes(tile.Kind) ? tile.Residents >= Housing(tile) * 5 / 6 : conditions.BusinessViable);
                tile.Growth = viable && tile.Level < MaxLevel(tile.Kind) ? tile.Growth + 1 : 0;
                if (tile.Growth >= 6)
                {
                    tile.Level++; tile.Growth = 0;
                    FacadeStyles.CompleteFloors(this, chunk.Position.OriginX + index % Size, chunk.Position.OriginZ + index / Size, month);
                }
            }
        chunk.LastSimulatedMonth = throughMonth;
        chunk.Recount();
    }
    public void SetActiveChunks(IEnumerable<ChunkPos> positions)
    {
        var next = positions.Where(chunks.ContainsKey).ToHashSet();
        if (next.SetEquals(active)) return;
        var conditions = new Conditions(this);
        foreach (var key in next.Order()) Advance(chunks[key], Month - 1, conditions);
        active = next;
        ReleaseInactive();
    }
    private void ReleaseInactive()
    {
        foreach (var (key, chunk) in chunks) if (!active.Contains(key)) chunk.Pack();
    }
    private void SynchronizeAll()
    {
        var conditions = new Conditions(this);
        foreach (var chunk in chunks.Values) Advance(chunk, Month - 1, conditions);
    }
    public void Tick()
    {
        long homes = chunks.Values.Sum(c => (long)c.Stats.Homes);
        long comfort = chunks.Values.Sum(c => (long)c.Stats.Comfort);
        int jobScore = Workers == 0 ? 10 : (int)(20.0 * Employed / Workers) - 10;
        Happiness = (int)Math.Clamp(72 + jobScore + (homes == 0 ? 0 : comfort / homes) - (TaxRate - 10) * 4 - (Money < 0 ? 15 : 0), 5, 100);
        var conditions = new Conditions(this);
        foreach (var (key, chunk) in chunks)
        {
            int phase = (int)(Math.Abs((long)key.X * 37 + (long)key.Z * 61) % DistantUpdateInterval);
            if (chunk.Stats.Placed == 0) chunk.LastSimulatedMonth = Month;
            else if (active.Contains(key) || Month - chunk.LastSimulatedMonth >= DistantUpdateInterval || (Month + phase) % DistantUpdateInterval == 0)
                Advance(chunk, Month, conditions);
        }
        double occupancy = Jobs == 0 ? 0 : Math.Min(1, Workers / (double)Jobs);
        LastIncome = Population * TaxRate / 2 + (long)(chunks.Values.Sum(c => c.Stats.BusinessIncome) * occupancy * TaxRate / 10);
        LastExpense = chunks.Values.Sum(c => c.Stats.Expense);
        Money += Balance;
        Month++;
        ReleaseInactive();
    }
    public bool HasNaturalTree(int x, int z)
    {
        unchecked { uint hash = (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)AppearanceSeed; hash ^= hash >> 16; return hash % 43 == 0; }
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        var records = chunks.Values.Select(c => new ChunkSave(c.Position.X, c.Position.Z, c.PurchasedMonth, c.LastSimulatedMonth, c.Cells));
        using (var stream = File.Create(temp)) JsonSerializer.Serialize(stream, new SaveData(3, records.ToArray(), Money, Month, TaxRate, LastIncome, LastExpense, Happiness, AppearanceSeed, active.Order().ToArray()));
        File.Move(temp, path, true);
        ReleaseInactive();
    }
    public static City Load(string path)
    {
        using var stream = File.OpenRead(path);
        var s = JsonSerializer.Deserialize<SaveData>(stream) ?? throw new InvalidDataException("保存データが空です");
        if (s.Version is 1 or 2) throw new InvalidDataException("旧形式の保存です。新しい町を始めて F5 で保存してください");
        if (s.Version != 3 || s.AppearanceSeed <= 0 || s.Chunks is null || s.Chunks.Length == 0 || s.Month < 1 || s.Month > int.MaxValue - 1000 || s.TaxRate < 5 || s.TaxRate > 20 || s.Happiness < 0 || s.Happiness > 100 || Math.Abs((decimal)s.Money) > 9_000_000_000_000_000m)
            throw new InvalidDataException("保存データの形式が正しくありません");
        var city = new City(false) { Money = s.Money, Month = s.Month, taxRate = s.TaxRate, LastIncome = s.LastIncome, LastExpense = s.LastExpense, Happiness = s.Happiness, AppearanceSeed = s.AppearanceSeed };
        city.chunks.Clear();
        foreach (var record in s.Chunks)
        {
            if (record is null || Math.Abs((long)record.X) > MaxChunkCoordinate || Math.Abs((long)record.Z) > MaxChunkCoordinate || record.PurchasedMonth < 1 || record.PurchasedMonth > s.Month || record.LastSimulatedMonth < record.PurchasedMonth - 1 || record.LastSimulatedMonth >= s.Month || s.Month - 1 - record.LastSimulatedMonth >= DistantUpdateInterval || record.Cells is null || record.Cells.Count > Size * Size)
                throw new InvalidDataException("区画の保存データが正しくありません");
            var key = new ChunkPos(record.X, record.Z);
            var chunk = new WorldChunk(key, record.PurchasedMonth) { LastSimulatedMonth = record.LastSimulatedMonth };
            if (!city.chunks.TryAdd(key, chunk)) throw new InvalidDataException("区画が重複しています");
            foreach (var (index, t) in record.Cells)
            {
                if (index < 0 || index >= Size * Size || t is null || t.Kind < TileKind.Road || (t.Kind > TileKind.LogisticsCenter || t.Kind == TileKind.Bulldoze) || t.Level < 1 || t.Level > MaxLevel(t.Kind) || t.Residents < 0 || t.Residents > Housing(t) || t.OffsetX < 0 || t.OffsetX >= Footprint(t.Kind) || t.OffsetZ < 0 || t.OffsetZ >= Footprint(t.Kind) || (!t.IsAnchor && (t.Residents != 0 || t.Growth != 0 || t.Level != 1)) || t.Growth < 0 || t.Growth >= 6 || !FacadeStyles.IsValid(t) || !GrandParkAppearance.IsValid(t) || !SupermarketAppearance.IsValid(t) || !LogisticsAppearance.IsValid(t))
                    throw new InvalidDataException("建物の保存データが正しくありません");
                chunk.Cells.Add(index, t);
                city.RegisterFaces(t.Kind, t.FaceStyles);
            }
        }
        foreach (var (x, z, tile) in city.PlacedCells())
        {
            var p = city.AnchorAt(x, z);
            var anchor = city[p.X, p.Z];
            if (!anchor.IsAnchor || anchor.Kind != tile.Kind) throw new InvalidDataException("建物の占有範囲が正しくありません");
            if (!tile.IsAnchor) continue;
            for (int dz = 0; dz < Footprint(tile.Kind); dz++) for (int dx = 0; dx < Footprint(tile.Kind); dx++)
            {
                var part = city[x + dx, z + dz];
                if (part.Kind != tile.Kind || part.OffsetX != dx || part.OffsetZ != dz) throw new InvalidDataException("建物の占有範囲が正しくありません");
            }
        }
        var reached = new HashSet<ChunkPos>(); var queue = new Queue<ChunkPos>();
        if (city.chunks.ContainsKey(new(0, 0))) { reached.Add(new(0, 0)); queue.Enqueue(new(0, 0)); }
        while (queue.TryDequeue(out var key))
            foreach (var next in new[] { new ChunkPos(key.X - 1, key.Z), new(key.X + 1, key.Z), new(key.X, key.Z - 1), new(key.X, key.Z + 1) })
                if (city.chunks.ContainsKey(next) && reached.Add(next)) queue.Enqueue(next);
        if (reached.Count != city.chunks.Count) throw new InvalidDataException("所有地は開始区画につながっている必要があります");
        if (s.ActiveChunks is null || s.ActiveChunks.Distinct().Count() != s.ActiveChunks.Length || s.ActiveChunks.Any(key => !city.chunks.ContainsKey(key) || city.chunks[key].LastSimulatedMonth != s.Month - 1))
            throw new InvalidDataException("更新範囲の保存データが正しくありません");
        city.active = s.ActiveChunks.ToHashSet();
        city.Reconnect();
        city.ReleaseInactive();
        return city;
    }
    public sealed record ChunkSave(int X, int Z, int PurchasedMonth, int LastSimulatedMonth, SortedDictionary<int, Tile> Cells);
    public sealed record SaveData(int Version, ChunkSave[] Chunks, long Money, int Month, int TaxRate, long LastIncome, long LastExpense, int Happiness, int AppearanceSeed = 0, ChunkPos[]? ActiveChunks = null);
}
