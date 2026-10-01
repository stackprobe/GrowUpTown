using System.Numerics;
using System.Reflection;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

public sealed class Game : IDisposable
{
    private readonly Color ink = new(21, 34, 46, 255), panel = new(29, 45, 57, 255), muted = new(151, 174, 183, 255);
    private readonly Color paper = new(239, 245, 236, 255), accent = new(179, 226, 131, 255);
    private readonly Font font;
    private readonly BuildingFacades buildingFacades;
    private readonly TerrainRenderer terrain;
    private readonly RoadRenderer roads;
    private readonly TrafficRenderer traffic = new();
    private readonly CameraDrag rightDrag = new();
    private readonly List<WorldChunk> visibleChunks = [];
    private int originX, originZ, viewMinX, viewMaxX, viewMinZ, viewMaxZ;
    private (int, int, int, int)? previousView;
    private bool landMode, hasHover, purchaseJustOpened;
    private ChunkPos? hoverLand, purchaseConfirm;
    private (int X, int Z)? painted;
    private bool rotatingView => rightDrag.Active;
    private double smokeTime;
    private readonly string savePath = StoragePaths.ForFile("city.json");
    private City city = new();
    private TileKind selected = TileKind.Home;
    private Camera3D camera;
    private Vector3 focus = new(11, 0, 12);
    private static readonly float DefaultPitch = MathF.Atan2(32, 28);
    private float pitch = DefaultPitch;
    private float zoom = 30, angle = .78f, elapsed;
    private int speed = 1, hoverX, hoverZ;
    private bool help, resetConfirm, specialOpen, specialJustOpened;
    private int specialPage;
    private static readonly TileKind[] Specials = [TileKind.GrandPark, TileKind.Supermarket, TileKind.LogisticsCenter];
    private bool showBuildPanel = true, showInfoPanel = true;
    private string message = "町づくりへようこそ。道路沿いに住宅を建ててみましょう。";
    private static readonly TileKind[] Tools = [TileKind.Road, TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.Park, TileKind.Bulldoze, TileKind.Water, TileKind.SuperMixed, TileKind.SuperFactory, TileKind.SuperIndustryCommerce, TileKind.SuperHome, TileKind.UltraCity];
    private int W => GetScreenWidth();
    private int H => GetScreenHeight();

    public Game()
    {
        nint startupMonitor = WindowPlacement.CaptureMonitor();
        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow | ConfigFlags.HiddenWindow);
        InitWindow(1440, 900, "Grow Up Town | Raylib / C#");
        SetWindowMinSize(1200, 800);
        WindowPlacement.Center(startupMonitor);
        ClearWindowState(ConfigFlags.HiddenWindow);
        SetTargetFPS(60);
        SetExitKey(KeyboardKey.Null);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "meiryo.ttc");
        if (!File.Exists(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "YuGothM.ttc");
        var assembly = Assembly.GetExecutingAssembly();
        string text = string.Concat(assembly.GetManifestResourceNames().Where(n => n.EndsWith(".cs")).Select(n => { using var r = new StreamReader(assembly.GetManifestResourceStream(n)!); return r.ReadToEnd(); }));
        int[] glyphs = text.Select(c => (int)c).Concat(Enumerable.Range(32, 95)).Distinct().Order().ToArray();
        font = LoadFontFromMemory(".ttf", FontLoader.ReadTrueType(path), 32, glyphs, glyphs.Length);
        if (font.Texture.Id == GetFontDefault().Texture.Id) throw new InvalidOperationException("日本語フォントを読み込めませんでした");
        SetTextureFilter(font.Texture, TextureFilter.Bilinear);
        buildingFacades = new BuildingFacades();
        terrain = new TerrainRenderer();
        roads = new RoadRenderer();
        UpdateCamera();
        RefreshView();
    }
    private static City NewPreviewCity()
    {
        var preview = City.NewEmpty();
        while (!preview.SpecialsUnlocked) preview.Tick();
        return preview;
    }
    public void Run(bool smokeTest)
    {
        if (smokeTest && int.TryParse(Environment.GetEnvironmentVariable("GROWUPTOWN_UNLOCK_PREVIEW"), out int year)
            && year is 1 or 5 or 10 or 15)
        {
            city = City.NewEmpty();
            while (city.Month < (year - 1) * 12 + 1) city.Tick();
            speed = 0;
            RefreshView(true);
        }
        if (smokeTest && Environment.GetEnvironmentVariable("GROWUPTOWN_SPECIAL_PREVIEW") is "markets" or "logistics")
        {
            city = NewPreviewCity(); city.Money = 1_000_000;
            for (int id = 0; id < 16; id++)
            {
                int x = 1 + id % 4 * 5, z = 1 + id / 4 * 5;
                city.Build(x, z, Environment.GetEnvironmentVariable("GROWUPTOWN_SPECIAL_PREVIEW") == "logistics" ? TileKind.LogisticsCenter : TileKind.Supermarket);
                city[x, z].MarketStyle = id; city[x, z].MarketRotation = 0;
                city[x, z].LogisticsStyle = id; city[x, z].LogisticsRotation = 0;
            }
            speed = 0; selected = Environment.GetEnvironmentVariable("GROWUPTOWN_SPECIAL_PREVIEW") == "logistics" ? TileKind.LogisticsCenter : TileKind.Supermarket;
            showBuildPanel = showInfoPanel = false;
            zoom = 32; angle = .12f; pitch = .94f; focus = new(10 + zoom * 140 / H, 0, 10);
            message = $"{City.Name(selected)}・16種類の外観（左上から順に1～16）";
            UpdateCamera(); RefreshView(true);
        }
        if (smokeTest && Environment.GetEnvironmentVariable("GROWUPTOWN_SPECIAL_PREVIEW") is "parks" or "menu")
        {
            city = NewPreviewCity(); city.Money = 1_000_000;
            for (int id = 0; id < 16; id++)
            {
                int x = 1 + id % 4 * 5, z = 1 + id / 4 * 5;
                city.Build(x, z, TileKind.GrandPark);
                city[x, z].ParkStyle = id; city[x, z].ParkRotation = 0;
            }
            speed = 0; selected = TileKind.GrandPark;
            showBuildPanel = showInfoPanel = false;
            focus = new(10, 0, 10); zoom = 32; angle = 0; pitch = 1.12f;
            focus.X += zoom * 140 / H;
            message = "大公園・16種類の外観プレビュー（左上から順に1～16）";
            specialOpen = Environment.GetEnvironmentVariable("GROWUPTOWN_SPECIAL_PREVIEW") == "menu";
            UpdateCamera(); RefreshView(true);
        }
        if (smokeTest && Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") is "ultra" or "ultra-variants")
        {
            city = NewPreviewCity();
            city.AppearanceSeed = 20260930;
            city.Money = 1_000_000;
            for (int z = 4; z <= 18; z++) city.Build(0, z, TileKind.Road);
            for (int row = 0; row < 3; row++)
            {
                int roadZ = 4 + row * 7;
                for (int x = 1; x <= 20; x++) city.Build(x, roadZ, TileKind.Road);
                for (int col = 0; col < 3; col++)
                {
                    int x = 3 + col * 7, z = roadZ - 3;
                    city.Build(x, z, TileKind.UltraCity);
                    city[x, z].Level = Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") == "ultra-variants" ? 6 : 1 + row * 3 + col;
                    FacadeStyles.CompleteFloors(city, x, z);
                }
            }
            city.Reconnect();
            speed = 0;
            selected = TileKind.UltraCity;
            showInfoPanel = false;
            if (Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") == "ultra-variants")
            {
                zoom = 36;
                UpdateCamera();
            }
            RefreshView(true);
        }
        if (smokeTest && Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") is "1" or "industry-commerce" or "home")
        {
            city = NewPreviewCity();
            city.Money = 100_000;
            for (int x = 0; x <= 20; x++) city.Build(x, 12, TileKind.Road);
            for (int level = 1; level <= 6; level++)
                foreach (var (kind, z) in Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") == "home"
                    ? new[] { (TileKind.SuperHome, 10), (TileKind.SuperHome, 13) }
                    : Environment.GetEnvironmentVariable("GROWUPTOWN_SUPER_PREVIEW") == "industry-commerce"
                    ? new[] { (TileKind.SuperIndustryCommerce, 10), (TileKind.SuperIndustryCommerce, 13) }
                    : new[] { (TileKind.SuperMixed, 10), (TileKind.SuperFactory, 13) })
                {
                    int x = 2 + (level - 1) * 3;
                    city.Build(x, z, kind);
                    city[x, z].Level = level;
                    FacadeStyles.CompleteFloors(city, x, z);
                }
            city.Reconnect();
            speed = 0;
            RefreshView(true);
        }
        int frames = 0;
        while (!WindowShouldClose())
        {
            Update();
            BeginDrawing();
            ClearBackground(new Color(192, 213, 211, 255));
            DrawWorld();
            DrawUi();
            if (help || resetConfirm || purchaseConfirm.HasValue || specialOpen) DrawModal();
            EndDrawing();
            if (smokeTest && ++frames >= 8)
            {
                string? screenshot = Environment.GetEnvironmentVariable("GROWUPTOWN_SCREENSHOT");
                if (!string.IsNullOrEmpty(screenshot))
                {
                    var capture = LoadImageFromScreen();
                    ExportImage(capture, StoragePaths.ForFile(screenshot));
                    UnloadImage(capture);
                }
                break;
            }
        }
    }
    private bool Hit(Rectangle r) => CheckCollisionPointRec(GetMousePosition(), r);
    private void ToggleDisplayMode()
    {
        // Raylib uses the window's current monitor and preserves its windowed bounds.
        ToggleBorderlessWindowed();
        rightDrag.Cancel();
        painted = null;
        UpdateCamera();
        RefreshView(true);
    }
    private void Update()
    {
        if ((IsKeyDown(KeyboardKey.LeftAlt) || IsKeyDown(KeyboardKey.RightAlt))
            && (IsKeyPressed(KeyboardKey.Enter) || IsKeyPressed(KeyboardKey.KpEnter)))
            ToggleDisplayMode();
        if (IsKeyPressed(KeyboardKey.F1) && !purchaseConfirm.HasValue && !specialOpen) help = !help;
        if (IsKeyPressed(KeyboardKey.Escape)) { help = false; resetConfirm = false; specialOpen = false; purchaseConfirm = null; landMode = false; selected = TileKind.Empty; }
        if (help || resetConfirm || purchaseConfirm.HasValue || specialOpen) { rightDrag.Cancel(); return; }
        if (IsKeyPressed(KeyboardKey.F2)) showBuildPanel = !showBuildPanel;
        if (IsKeyPressed(KeyboardKey.F3)) showInfoPanel = !showInfoPanel;
        if (IsKeyPressed(KeyboardKey.Tab))
        {
            bool show = !showBuildPanel && !showInfoPanel;
            showBuildPanel = showInfoPanel = show;
        }
        float dt = Math.Min(GetFrameTime(), .1f);
        if (IsKeyPressed(KeyboardKey.Space)) speed = speed == 0 ? 1 : 0;
        if (IsKeyPressed(KeyboardKey.F5)) Save();
        if (IsKeyPressed(KeyboardKey.F9)) Load();
        if (IsKeyPressed(KeyboardKey.L)) { landMode = !landMode; selected = TileKind.Empty; }
        for (int i = 0; i < Tools.Length; i++)
            if (IsKeyPressed(i == 11 ? KeyboardKey.U : i == 10 ? KeyboardKey.T : i == 9 ? KeyboardKey.Zero : (KeyboardKey)((int)KeyboardKey.One + i)) && city.IsUnlocked(Tools[i])) { selected = Tools[i]; landMode = false; }
        Vector3 right = new(MathF.Cos(angle), 0, -MathF.Sin(angle));
        Vector3 forward = new(-MathF.Sin(angle), 0, -MathF.Cos(angle));
        if (IsKeyDown(KeyboardKey.W) || IsKeyDown(KeyboardKey.Up)) focus += forward * dt * 12;
        if (IsKeyDown(KeyboardKey.S) || IsKeyDown(KeyboardKey.Down)) focus -= forward * dt * 12;
        if (IsKeyDown(KeyboardKey.D) || IsKeyDown(KeyboardKey.Right)) focus += right * dt * 12;
        if (IsKeyDown(KeyboardKey.A) || IsKeyDown(KeyboardKey.Left)) focus -= right * dt * 12;
        if (IsKeyDown(KeyboardKey.Q)) angle -= dt;
        if (IsKeyDown(KeyboardKey.E)) angle += dt;
        if (IsKeyDown(KeyboardKey.R)) pitch += dt;
        if (IsKeyDown(KeyboardKey.F)) pitch -= dt;
        if (IsKeyPressed(KeyboardKey.Home)) ResetCamera();
        bool overWorld = GetMouseX() > (showBuildPanel ? 290 : 0) && GetMouseY() > 114 && GetMouseY() < H - 76
            && !(showInfoPanel && Hit(new(W - 354, 124, 340, 397)));
        bool shift = IsKeyDown(KeyboardKey.LeftShift) || IsKeyDown(KeyboardKey.RightShift);
        rightDrag.Update(IsMouseButtonDown(MouseButton.Right), IsMouseButtonPressed(MouseButton.Right), IsWindowFocused(), overWorld, shift);
        if (rightDrag.Active)
        {
            Vector2 delta = GetMouseDelta();
            if (rightDrag.Panning) focus += CameraDrag.Pan(delta, angle, pitch, zoom, H);
            else { angle -= delta.X * .005f; pitch += delta.Y * .005f; }
        }
        if (overWorld)
        {
            float wheel = GetMouseWheelMove();
            if (shift) pitch += wheel * .06f;
            else zoom = Math.Clamp(zoom - wheel * 2, 12, 48);
        }
        pitch = Math.Clamp(pitch, 10 * MathF.PI / 180, 85 * MathF.PI / 180);
        bool middlePan = overWorld && !rightDrag.Active && IsMouseButtonDown(MouseButton.Middle);
        if (middlePan) focus += CameraDrag.Pan(GetMouseDelta(), angle, pitch, zoom, H);
        ClampAndRebaseCamera();
        UpdateCamera();
        RefreshView();
        hasHover = false; hoverLand = null;
        if (overWorld && !rightDrag.Active && !middlePan)
        {
            Ray ray = GetScreenToWorldRay(GetMousePosition(), camera);
            if (Math.Abs(ray.Direction.Y) > .0001f)
            {
                float t = -ray.Position.Y / ray.Direction.Y;
                Vector3 p = ray.Position + ray.Direction * t;
                int x = ClampWorldCoordinate(originX + Math.Floor(p.X + .5)), z = ClampWorldCoordinate(originZ + Math.Floor(p.Z + .5));
                if (t > 0)
                {
                    if (landMode && city.CanPurchase(ChunkPos.FromTile(x, z))) hoverLand = ChunkPos.FromTile(x, z);
                    else if (!landMode && city.Inside(x, z)) { hoverX = x; hoverZ = z; hasHover = true; }
                }
            }
        }
        if (!IsMouseButtonDown(MouseButton.Left)) painted = null;
        if (hoverLand.HasValue && IsMouseButtonPressed(MouseButton.Left))
        {
            purchaseConfirm = hoverLand; purchaseJustOpened = true; rightDrag.Cancel(); return;
        }
        if (hasHover && selected != TileKind.Empty && !IsMouseButtonDown(MouseButton.Right) && IsMouseButtonDown(MouseButton.Left))
        {
            var cell = (hoverX, hoverZ);
            if (painted != cell && (selected is TileKind.Road or TileKind.Water || IsMouseButtonPressed(MouseButton.Left)))
            { message = city.Build(hoverX, hoverZ, selected); painted = cell; }
        }
        elapsed += dt * speed; smokeTime += dt * speed;
        while (elapsed >= 5) { elapsed -= 5; city.Tick(); }
    }
    private void ResetCamera()
    {
        originX = originZ = 0; focus = new(11, 0, 12); zoom = 30; angle = .78f; pitch = DefaultPitch;
        previousView = null; rightDrag.Cancel(); painted = null; hasHover = false; hoverLand = null;
    }
    private void ClampAndRebaseCamera()
    {
        double wx = originX + (double)focus.X, wz = originZ + (double)focus.Z;
        var key = ChunkPos.FromTile((int)Math.Floor(wx), (int)Math.Floor(wz));
        bool near = false;
        for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            near |= city.Chunks.ContainsKey(new(key.X + dx, key.Z + dz));
        if (!near)
        {
            double best = double.MaxValue, bestX = wx, bestZ = wz;
            foreach (var owned in city.Chunks.Keys)
            {
                double x = Math.Clamp(wx, owned.OriginX - 23.5, owned.OriginX + 47.49);
                double z = Math.Clamp(wz, owned.OriginZ - 23.5, owned.OriginZ + 47.49);
                double distance = (wx - x) * (wx - x) + (wz - z) * (wz - z);
                if (distance < best) { best = distance; bestX = x; bestZ = z; }
            }
            focus.X = (float)(bestX - originX); focus.Z = (float)(bestZ - originZ);
        }
        // Keep rendering and input near zero, even when world coordinates are large.
        int sx = ChunkPos.Divide((int)Math.Floor(focus.X)) * City.Size;
        int sz = ChunkPos.Divide((int)Math.Floor(focus.Z)) * City.Size;
        originX += sx; originZ += sz; focus.X -= sx; focus.Z -= sz;
    }
    private static int ClampWorldCoordinate(double value) => (int)Math.Clamp(value, -(double)City.MaxChunkCoordinate * City.Size, (double)City.MaxChunkCoordinate * City.Size + City.Size - 1);
    private void RefreshView(bool force = false)
    {
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var point in new[] { new Vector2(showBuildPanel ? 290 : 0, 108), new Vector2(W, 108), new Vector2(W, H - 76), new Vector2(showBuildPanel ? 290 : 0, H - 76) })
        {
            var ray = GetScreenToWorldRay(point, camera);
            foreach (float height in new[] { WaterRenderer.WaterHeight, 11f })
            {
                var p = ray.Position + ray.Direction * ((height - ray.Position.Y) / ray.Direction.Y);
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            }
        }
        viewMinX = ClampWorldCoordinate(originX + Math.Floor(minX) - 3); viewMaxX = ClampWorldCoordinate(originX + Math.Ceiling(maxX) + 1);
        viewMinZ = ClampWorldCoordinate(originZ + Math.Floor(minZ) - 3); viewMaxZ = ClampWorldCoordinate(originZ + Math.Ceiling(maxZ) + 1);
        var bounds = (ChunkPos.Divide(viewMinX), ChunkPos.Divide(viewMaxX), ChunkPos.Divide(viewMinZ), ChunkPos.Divide(viewMaxZ));
        if (!force && previousView == bounds) return;
        previousView = bounds;
        visibleChunks.Clear();
        var active = new List<ChunkPos>();
        for (int z = bounds.Item3 - 1; z <= bounds.Item4 + 1; z++)
            for (int x = bounds.Item1 - 1; x <= bounds.Item2 + 1; x++)
            {
                var key = new ChunkPos(x, z);
                if (!city.Chunks.TryGetValue(key, out var chunk)) continue;
                active.Add(key);
                if (x >= bounds.Item1 && x <= bounds.Item2 && z >= bounds.Item3 && z <= bounds.Item4) visibleChunks.Add(chunk);
            }
        city.SetActiveChunks(active);
    }
    private void UpdateCamera()
    {
        // Shift the target so the playable board is centered in the space beside the sidebar.
        Vector3 right = new(MathF.Cos(angle), 0, -MathF.Sin(angle));
        Vector3 target = focus - right * (zoom * 140 / H);
        float distance = MathF.Sqrt(28 * 28 + 32 * 32);
        float horizontal = distance * MathF.Cos(pitch);
        camera = new Camera3D { Position = target + new Vector3(MathF.Sin(angle) * horizontal, distance * MathF.Sin(pitch), MathF.Cos(angle) * horizontal), Target = target, Up = Vector3.UnitY, FovY = zoom, Projection = CameraProjection.Orthographic };
    }
    private Color KindColor(TileKind kind) => kind switch
    {
        TileKind.UltraCity => new(115, 224, 211, 255), TileKind.SuperHome => new(153, 210, 219, 255),
        TileKind.Road => new(122, 142, 155, 255), TileKind.Home => new(161, 216, 124, 255),
        TileKind.SuperIndustryCommerce => new(120, 183, 196, 255), TileKind.SuperMixed => new(126, 202, 179, 255), TileKind.SuperFactory => new(211, 151, 81, 255), TileKind.Shop => new(104, 189, 218, 255), TileKind.Factory => new(231, 171, 92, 255),
        TileKind.LogisticsCenter => new(134, 176, 210, 255), TileKind.Supermarket => new(235, 177, 93, 255), TileKind.GrandPark => new(152, 209, 115, 255), TileKind.Water => new(76, 161, 188, 255), TileKind.Park => new(100, 187, 153, 255), TileKind.Bulldoze => new(233, 131, 117, 255), _ => paper
    };
    private static void Box(float x, float y, float z, float w, float h, float d, Color color) => DrawCube(new(x, y + h / 2, z), w, h, d, color);
    private void Tree(float x, float z, float size = 1)
    {
        Box(x, 0, z, .09f, .45f * size, .09f, new(116, 100, 76, 255));
        DrawSphere(new(x, .58f * size, z), .3f * size, new(73, 135, 99, 255));
        DrawSphere(new(x - .07f, .77f * size, z), .23f * size, new(106, 164, 114, 255));
    }
    private void DrawFactorySmoke(float x, float z, float chimneyTop, int worldX, int worldZ, bool connected)
    {
        if (!connected) return;
        // Three small cartoon puffs, emitted every 0.95 simulation seconds.
        // Solid low-poly ellipsoids share the world's normal depth-tested batch:
        // no texture, transparent pass, forced flush, or second scan of the map.
        double phase = smokeTime / 2.85 + worldX * .37 + worldZ * .61;
        for (int puff = 0; puff < 3; puff++)
        {
            double cycle = phase + puff / 3.0;
            float age = (float)(cycle - Math.Floor(cycle));
            float grow = Math.Clamp(age / .10f, 0, 1);
            float disappear = Math.Clamp((1 - age) / .28f, 0, 1);
            // Ease into existence and shrink away, keeping a crisp, readable outline.
            float scale = grow * grow * (3 - 2 * grow) * disappear * disappear * (3 - 2 * disappear);
            float radius = (.075f + age * .045f) * scale;
            if (radius < .002f) continue;
            Rlgl.PushMatrix();
            Rlgl.Translatef(x - .21f + age * .20f, chimneyTop + .025f + age * .82f, z - .19f + age * .08f);
            Rlgl.Scalef(1.25f, .85f, 1);
            DrawSphereEx(Vector3.Zero, radius, 4, 8, new Color(200, 211, 207, 255));
            Rlgl.PopMatrix();
        }
    }
    private void DrawWorld()
    {
        traffic.Update(city, visibleChunks, viewMinX, viewMaxX, viewMinZ, viewMaxZ, smokeTime);
        BeginMode3D(camera);
        terrain.Draw(city, visibleChunks, originX, originZ);
        foreach (var chunk in visibleChunks)
        {
            int minX = Math.Max(viewMinX, chunk.Position.OriginX), maxX = Math.Min(viewMaxX, chunk.Position.OriginX + 23);
            int minZ = Math.Max(viewMinZ, chunk.Position.OriginZ), maxZ = Math.Min(viewMaxZ, chunk.Position.OriginZ + 23);
            for (int wz = minZ; wz <= maxZ; wz++) for (int wx = minX; wx <= maxX; wx++)
            {
                int x = wx - originX, z = wz - originZ;
                Tile tile = city[wx, wz];
                if (tile.Kind == TileKind.Empty)
                {
                    if (city.HasNaturalTree(wx, wz)) Tree(x, z, .7f);
                    continue;
                }
                if (!tile.IsAnchor) continue;
                Color c = KindColor(tile.Kind);
                if (tile.Kind == TileKind.Water) continue;
                if (tile.Kind == TileKind.Road)
                {
                    int bridge = city.BridgeAxis(wx, wz);
                    if (bridge == 0) Box(x, .02f, z, 1, .045f, 1, new(88, 105, 112, 255));
                    else
                    {
                        bool alongX = bridge == 1;
                        Box(x, RoadRenderer.DeckTop - .10f, z, 1, .10f, 1, new(164, 160, 143, 255));
                        for (int side = -1; side <= 1; side += 2)
                        {
                            float rx = x + (alongX ? 0 : side * .46f), rz = z + (alongX ? side * .46f : 0);
                            Box(rx, RoadRenderer.DeckTop + .15f, rz, alongX ? 1 : .04f, .045f, alongX ? .04f : 1, new(222, 218, 193, 255));
                            Box(rx, RoadRenderer.DeckTop, rz, .055f, .17f, .055f, new(179, 181, 167, 255));
                        }
                    }
                }
                else if (tile.Kind == TileKind.Park)
                {
                    ParkRenderer.Draw(city, wx, wz, x, z);
                }
                else if (tile.Kind == TileKind.LogisticsCenter)
                {
                    LogisticsRenderer.Draw(x, z, tile);
                }
                else if (tile.Kind == TileKind.Supermarket)
                {
                    SupermarketRenderer.Draw(x, z, tile);
                }
                else if (tile.Kind == TileKind.GrandPark)
                {
                    GrandParkRenderer.Draw(x, z, tile);
                }
                else if (tile.Kind == TileKind.UltraCity)
                {
                    UltraCityRenderer.Draw(x + 1, z + 1, tile, smokeTime);
                }
                else if (City.Footprint(tile.Kind) == 2)
                {
                    DrawSuperBuilding(x + .5f, z + .5f, tile, wx, wz);
                }
                else
                {
                    Box(x, .03f, z, .93f, .06f, .93f, new(207, 211, 187, 255));
                    float height = BuildingFacades.Height(tile);
                    Box(x + .09f, .09f, z + .09f, .76f, .018f, .76f, new(124, 145, 122, 255));
                    float story = BuildingFacades.FloorHeight(tile.Kind);
                    for (int floor = 0; floor < tile.Level; floor++)
                        Box(x, .09f + floor * story, z, .68f, story, .68f, c);
                    Box(x, height + .09f, z, .76f, .09f, .76f, tile.Kind == TileKind.Home ? new(74, 111, 102, 255) : new(230, 234, 216, 255));
                    if (tile.Kind == TileKind.Factory)
                    {
                        Box(x - .21f, height, z - .19f, .15f, .57f, .15f, new(133, 101, 84, 255));
                        Box(x - .21f, height + .5f, z - .19f, .18f, .1f, .18f, new(219, 200, 163, 255));
                        DrawFactorySmoke(x, z, height + .6f, wx, wz, tile.Connected);
                    }
                }
                if (!tile.Connected && tile.Kind != TileKind.Park)
                    DrawCubeWires(new(x + (City.Footprint(tile.Kind) - 1) * .5f, .15f, z + (City.Footprint(tile.Kind) - 1) * .5f), City.Footprint(tile.Kind) - .06f, .3f, City.Footprint(tile.Kind) - .06f, new(222, 98, 88, 255));
            }
        }
        terrain.DrawWater(visibleChunks, originX, originZ, smokeTime);
        roads.Draw(city, visibleChunks, originX, originZ);
        traffic.Draw(originX, originZ);
        buildingFacades.Draw(visibleChunks, camera, originX, originZ);
        if (landMode)
        {
            for (int cz = ChunkPos.Divide(viewMinZ); cz <= ChunkPos.Divide(viewMaxZ); cz++)
                for (int cx = ChunkPos.Divide(viewMinX); cx <= ChunkPos.Divide(viewMaxX); cx++)
                {
                    var key = new ChunkPos(cx, cz);
                    if (!city.CanPurchase(key)) continue;
                    float x = key.OriginX - originX + 11.5f, z = key.OriginZ - originZ + 11.5f;
                    Color color = hoverLand == key ? accent : muted;
                    Box(x, -.07f, z, 23.9f, .04f, 23.9f, new Color(color.R, color.G, color.B, (byte)(hoverLand == key ? 110 : 45)));
                    DrawCubeWires(new(x, .015f, z), 24, .03f, 24, color);
                }
        }
        // A fixed inlet remains visible after purchasing western land.
        if (viewMinX <= 0 && viewMaxX >= 0 && viewMinZ <= 12 && viewMaxZ >= 12)
        {
            Box(-.39f - originX, .08f, 12 - originZ, .14f, .06f, .66f, accent);
        }
        if (hasHover)
        {
            var anchor = selected == TileKind.Bulldoze ? city.AnchorAt(hoverX, hoverZ) : (X: hoverX, Z: hoverZ);
            int size = City.Footprint(selected == TileKind.Bulldoze ? city[hoverX, hoverZ].Kind : selected);
            float x = anchor.X - originX + (size - 1) * .5f, z = anchor.Z - originZ + (size - 1) * .5f;
            bool valid = city.CanBuild(hoverX, hoverZ, selected);
            Color c = selected == TileKind.Empty ? paper : valid ? accent : KindColor(TileKind.Bulldoze);
            DrawCubeWires(new(x, .16f, z), size, .30f, size, c);
            if (valid && selected != TileKind.Bulldoze) Box(x, .06f, z, size - .12f, .1f, size - .12f, new Color(c.R, c.G, c.B, (byte)110));
        }
        EndMode3D();
    }
    private void DrawSuperBuilding(float x, float z, Tile tile, int wx, int wz)
    {
        if (tile.Kind == TileKind.SuperHome) { DrawResidentialTower(x, z, tile); return; }
        bool industryCommerce = tile.Kind == TileKind.SuperIndustryCommerce;
        bool factory = tile.Kind == TileKind.SuperFactory || industryCommerce;
        float height = BuildingFacades.Height(tile), story = BuildingFacades.FloorHeight(tile.Kind);
        Box(x, .03f, z, 1.94f, .06f, 1.94f, new(207, 211, 187, 255));
        for (int floor = 0; floor < tile.Level; floor++)
            Box(x, .09f + floor * story, z, 1.68f, story, 1.68f, !factory && floor == 0 ? KindColor(TileKind.Shop) : KindColor(tile.Kind));
        Box(x, height + .09f, z, 1.78f, .10f, 1.78f, factory ? new(104, 115, 119, 255) : new(74, 111, 102, 255));
        if (industryCommerce)
        {
            // Production plant on the roof, storefront canopy and an external freight lift.
            Box(x - .35f, height + .19f, z - .26f, .75f, .32f, .86f, new(202, 153, 91, 255));
            Box(x - .35f, height + .51f, z - .26f, .83f, .08f, .94f, new(219, 222, 204, 255));
            Box(x - .58f, height + .59f, z - .51f, .18f, .52f, .18f, new(132, 109, 90, 255));
            DrawFactorySmoke(x - .37f, z - .32f, height + 1.11f, wx, wz, tile.Connected);
            Box(x, .62f, z + .88f, 1.72f, .08f, .18f, new(43, 117, 143, 255));
            // Loading bay on the east side. All details stay inside the 2x2 footprint.
            Box(x + .846f, .10f, z + .12f, .016f, .48f, .66f, new(61, 79, 86, 255));
            for (int slat = 0; slat < 4; slat++)
                Box(x + .858f, .16f + slat * .10f, z + .12f, .022f, .026f, .60f, new(165, 184, 185, 255));
            Box(x + .88f, .58f, z + .12f, .18f, .08f, .77f, new(230, 173, 71, 255));
            for (int side = -1; side <= 1; side += 2)
                Box(x + .88f, .09f, z - .57f + side * .15f, .06f, height + .30f, .045f, new(64, 93, 105, 255));
            for (int floor = 0; floor <= tile.Level; floor++)
                Box(x + .88f, .09f + floor * story, z - .57f, .07f, .045f, .34f, new(230, 173, 71, 255));
            Box(x + .40f, height + .19f, z + .32f, .55f, .08f, .80f, new(57, 78, 88, 255));
            for (int cargo = 0; cargo < 2; cargo++)
                Box(x + .40f, height + .27f, z + .09f + cargo * .37f, .32f, .23f, .28f, new(211, 162, 98, 255));
        }
        else if (factory)
        {
            for (int i = 0; i < 3; i++)
            {
                Box(x - .55f + i * .55f, height + .19f, z + .25f, .36f, .22f, .7f, new(154, 176, 181, 255));
                Box(x - .55f + i * .55f, height + .19f, z - .56f, .19f, .85f, .19f, new(137, 101, 82, 255));
                Box(x - .55f + i * .55f, height + .85f, z - .56f, .23f, .12f, .23f, new(232, 215, 177, 255));
            }
            DrawFactorySmoke(x - .34f, z - .37f, height + 1.03f, wx, wz, tile.Connected);
        }
        else
        {
            // Commercial podium below residential floors.
            Box(x, .62f, z + .87f, 1.72f, .08f, .20f, new(65, 145, 164, 255));
            Box(x + .87f, .62f, z, .20f, .08f, 1.72f, new(65, 145, 164, 255));
            Box(x - .48f, height + .19f, z - .48f, .42f, .25f, .42f, new(151, 182, 161, 255));
            Box(x + .48f, height + .19f, z + .48f, .38f, .12f, .38f, new(101, 164, 99, 255));
        }
    }
    private void DrawResidentialTower(float x, float z, Tile tile)
    {
        float story = BuildingFacades.FloorHeight(tile.Kind), height = BuildingFacades.Height(tile);
        Color ivory = new(225, 233, 221, 255), silver = new(176, 199, 200, 255);
        Box(x, .03f, z, 1.94f, .08f, 1.94f, ivory);
        for (int floor = 0; floor < tile.Level; floor++)
        {
            int blue = 155 + tile.FaceStyles[floor * 4] * 3;
            Box(x, .11f + floor * story, z, 1.36f, story, 1.36f, new(82, blue, 192, 255));
            // Two glazed balcony tiers per level emphasize the tower's height.
            for (int tier = 0; tier < 2; tier++)
            {
                float y = .11f + floor * story + tier * story / 2;
                Box(x, y, z, 1.62f, .055f, 1.62f, ivory);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(x, y + .08f, z + side * .77f, 1.55f, .14f, .025f, silver);
                    Box(x + side * .77f, y + .08f, z, .025f, .14f, 1.55f, silver);
                }
            }
        }
        for (int side = -1; side <= 1; side += 2)
            for (int column = -1; column <= 1; column++)
            {
                Box(x + column * .62f, .11f, z + side * .69f, .035f, height, .035f, ivory);
                Box(x + side * .69f, .11f, z + column * .62f, .035f, height, .035f, ivory);
            }
        Box(x, height + .11f, z, 1.70f, .10f, 1.70f, ivory);
        Box(x, height + .21f, z, .80f, .32f, .80f, new(111, 160, 175, 255));
        Box(x, height + .53f, z, .96f, .06f, .96f, silver);
        Box(x, .11f, z + .70f, .42f, .42f, .05f, new(42, 84, 100, 255));
        Box(x, .55f, z + .80f, .70f, .055f, .33f, ivory);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(x + side * .78f, .11f, z + .80f, .24f, .14f, .24f, silver);
            Box(x + side * .78f, .25f, z + .80f, .23f, .16f, .23f, new(94, 155, 112, 255));
        }
    }
    private void Text(string s, float x, float y, float size = 18, Color? color = null) => DrawTextEx(font, s, new(x, y), size, 1, color ?? paper);
    private void Panel(Rectangle r, Color? color = null) => DrawRectangleRounded(r, .12f, 6, color ?? panel);
    private bool Button(Rectangle r, string label, bool active = false, Color? color = null, float textSize = 18, bool available = true)
    {
        bool enabled = available && !help && !resetConfirm && !purchaseConfirm.HasValue && !specialOpen && !rotatingView;
        bool hot = enabled && Hit(r);
        Panel(r, !available ? new Color(30, 43, 52, 255) : active ? color ?? accent : hot ? new Color(57, 78, 89, 255) : new Color(40, 59, 71, 255));
        Text(label, r.X + 12, r.Y + (r.Height - textSize) / 2, textSize, !available ? muted : active ? ink : paper);
        return enabled && hot && IsMouseButtonPressed(MouseButton.Left);
    }
    private void Metric(string name, string value, float x, Color? color = null)
    {
        Text(name, x, 26, 15, muted); Text(value, x, 53, 27, color ?? paper);
    }
    private void DrawUi()
    {
        DrawRectangle(0, 0, W, 108, ink);
        Text("GROW UP", 26, 20, 27, accent); Text("T O W N", 27, 57, 25);
        Text("町建設シミュレーション", 185, 70, 12, muted);
        float step = (W - 365) / 5f;
        Metric("資金", $"¥ {city.Money:N0}", 350, city.Money < 0 ? KindColor(TileKind.Bulldoze) : accent);
        Metric("人口 / 入居可能", $"{city.Population} / {city.Capacity}", 350 + step);
        Metric("雇用 / 労働人口", $"{city.Employed} / {city.Workers}", 350 + step * 2);
        Metric("幸福度", $"{city.Happiness}%", 350 + step * 3);
        Metric("月間収支", $"{city.Balance:+#,0;-#,0;0}", 350 + step * 4, city.Balance < 0 ? KindColor(TileKind.Bulldoze) : accent);
        if (Button(new(26, 83, 126, 21), showBuildPanel ? "F2 建設を隠す" : "F2 建設を表示", textSize: 13)) showBuildPanel = !showBuildPanel;
        if (Button(new(160, 83, 126, 21), showInfoPanel ? "F3 情報を隠す" : "F3 情報を表示", textSize: 13)) showInfoPanel = !showInfoPanel;
        if (showBuildPanel) DrawBuildPanel();
        if (showInfoPanel) DrawInfoPanel();
        DrawBottomUi();
    }
    private void DrawBuildPanel()
    {
        Panel(new(14, 122, 270, H - 208), ink);
        Text("BUILD / 建設", 30, 140, 21);
        Text("道路入口 [0, 12] から町を広げよう", 30, 173, 14, muted);
        for (int i = 0; i < Tools.Length; i++)
        {
            var kind = Tools[i];
            if (Button(new(28, 203 + i * 23, 242, 21), !city.IsUnlocked(kind) ? $"{(i == 11 ? "U" : i == 10 ? "T" : ((i + 1) % 10).ToString())}  ？  ¥？" : $"{(i == 11 ? "U" : i == 10 ? "T" : ((i + 1) % 10).ToString())}  {(kind == TileKind.UltraCity ? "ultra複合都市" : City.Name(kind))}  ¥{City.Cost(kind)}", !landMode && selected == kind, KindColor(kind), 14, available: city.IsUnlocked(kind))) { selected = kind; landMode = false; }
        }
        if (Button(new(28, 482, 242, 23), city.SpecialsUnlocked ? "スペシャル  …" : "？  ¥？", !landMode && Specials.Contains(selected), KindColor(TileKind.GrandPark), 15, available: city.SpecialsUnlocked))
        {
            specialOpen = specialJustOpened = true; specialPage = 0;
            hasHover = false; rightDrag.Cancel(); painted = null;
        }
        if (Button(new(28, 510, 242, 32), $"L   土地購入  ¥{city.LandPrice:N0}", landMode)) { landMode = !landMode; selected = TileKind.Empty; }
        Text($"町の運営  /  {city.Chunks.Count}区画", 30, 555, 19);
        Text($"税率  {city.TaxRate}%", 30, 592, 20);
        if (Button(new(166, 585, 45, 36), "−")) city.TaxRate = Math.Max(5, city.TaxRate - 1);
        if (Button(new(220, 585, 45, 36), "+")) city.TaxRate = Math.Min(20, city.TaxRate + 1);
        Text($"税収     ¥ {city.LastIncome:N0}", 30, 633, 16, muted);
        Text($"維持費  ¥ {city.LastExpense:N0}", 30, 659, 16, muted);
        Text($"仕事の定員  {city.Jobs}人", 30, 685, 16, muted);
        if (H >= 910)
        {
            Text("発展のヒント", 30, 723, 18, accent);
            Text("住宅と職場を道路沿いに。\n公園は近隣の幸福度を改善。\n工場は住宅から離しましょう。", 30, 756, 15, muted);
        }
    }
    private void DrawInfoPanel()
    {
        Panel(new(W - 354, 124, 340, 54), ink);
        Text($"{(city.Month - 1) / 12 + 1}年 {(city.Month - 1) % 12 + 1}月", W - 340, 141, 20);
        int[] speeds = [0, 1, 2, 4];
        for (int i = 0; i < speeds.Length; i++) if (Button(new(W - 219 + i * 51, 133, 46, 36), i == 0 ? "Ⅱ" : $"{speeds[i]}x", speed == speeds[i])) speed = speeds[i];
        DrawRectangle(W - 340, 172, (int)(312 * elapsed / 5), 2, accent);
        Panel(new(W - 354, 188, 340, 43), ink);
        Text($"目標：人口 500人     {Math.Min(100, city.Population * 100 / 500)}%", W - 337, 201, 16, city.Population >= 500 ? accent : paper);
        DrawTownNeeds();
    }
    private void DrawBottomUi()
    {
        if (showBuildPanel && hasHover)
        {
            Tile t = city.BuildingAt(hoverX, hoverZ);
            Panel(new(306, H - 159, Math.Min(610, W - 325), 68), ink);
            Text($"{City.Name(t.Kind)}   [{hoverX}, {hoverZ}]" + (FacadeStyles.IsBuilding(t.Kind) ? $"  Lv.{t.Level}" : ""), 324, H - 147, 18, accent);
            string detail = t.Kind switch
            {
                TileKind.Empty => "左クリックで建設 / 道路・水域はドラッグで連続配置",
                TileKind.Park => t.Connected ? "道路接続あり（公園経由を含む） / 住環境＋10" : "道路未接続 / 効果30％：住環境＋3",
                TileKind.GrandPark => $"外観 {t.ParkStyle + 1}/16・固定 / " + (t.Connected ? "道路接続あり（公園経由を含む） / 住環境＋20" : "道路未接続 / 効果30％：住環境＋6"),
                TileKind.LogisticsCenter => $"外観 {t.LogisticsStyle + 1}/16・固定 / " + (t.Connected ? "工業・商業税収＋20％" : "効果には道路接続が必要"),
                TileKind.Supermarket => $"外観 {t.MarketStyle + 1}/16・固定 / " + (t.Connected ? "住宅＋10・商業税収＋20％" : "効果には道路接続が必要"),
                TileKind.Water => "水辺が近隣の住環境を改善",
                _ => t.Connected ? "道路接続あり" : "道路未接続"
            };
            detail += (City.HasHomes(t.Kind) ? $"   住民 {t.Residents}/{City.Housing(t)}人   住環境 {t.Comfort:+0;-0;0}" : "")
                + (t.MarketAccess && t.Connected ? "   商業＋20％" : "") + (t.LogisticsAccess && t.Connected ? "   物流＋20％" : "");
            float detailSize = Math.Min(15, 15 * (Math.Min(610, W - 325) - 36) / Math.Max(1, MeasureTextEx(font, detail, 15, 1).X));
            Text(detail, 324, H - 117, detailSize, muted);
        }
        if (showBuildPanel && landMode)
        {
            Panel(new(306, H - 159, Math.Min(650, W - 325), 68), ink);
            Text(hoverLand is { } key ? $"土地 [{key.X}, {key.Z}]  24×24タイル / ¥{city.LandPrice:N0}" : "土地購入：所有地に接する枠をクリック", 324, H - 147, 18, accent);
            Text(city.Money >= city.LandPrice ? "クリックで購入内容を確認 / Escで終了" : "購入資金が不足しています / Shift＋右ドラッグで移動", 324, H - 117, 15, muted);
        }
        DrawRectangle(0, H - 76, W, 76, ink);
        Text(message, 24, H - 64, 16, accent);
        Text("右ドラッグ：回転 / Shift＋右ドラッグ：移動 / L 土地購入 / Tab パネル表示切替 / F1 操作", 24, H - 34, 13, muted);
        if (Button(new(W - 410, H - 56, 94, 37), "F5 保存")) Save();
        if (Button(new(W - 307, H - 56, 94, 37), "F9 読込")) Load();
        if (Button(new(W - 204, H - 56, 94, 37), "新しい町")) resetConfirm = true;
        if (Button(new(W - 101, H - 56, 87, 37), "F1 操作")) help = true;
    }
    private void DrawTownNeeds()
    {
        float x = W - 354;
        Panel(new(x, 243, 340, 278), ink);
        Text("今、町に必要なもの", x + 17, 258, 20, accent);
        var advice = TownNeeds.Evaluate(city);
        for (int i = 0; i < advice.Length; i++)
        {
            float y = 291 + i * 73;
            var item = advice[i];
            Color color = item.Attention ? new Color(242, 194, 111, 255) : accent;
            Text(item.Title, x + 17, y, 17, color);
            float detailSize = Math.Min(14, 14 * 306 / Math.Max(1, MeasureTextEx(font, item.Detail, 14, 1).X));
            Text(item.Detail, x + 17, y + 25, detailSize, paper);
            Text(item.Action, x + 17, y + 45, 14, muted);
        }
    }
    private void DrawModal()
    {
        if (specialOpen) { DrawSpecialModal(); return; }
        if (purchaseConfirm is { } land) { DrawLandModal(land); return; }
        DrawRectangle(0, 0, W, H, new Color(9, 20, 29, 205));
        float x = (W - 740) / 2f, y = (H - 540) / 2f;
        Panel(new(x, y, 740, 540), ink);
        Text(resetConfirm ? "新しい町を始めますか？" : "小さな町を、大きな暮らしへ。", x + 35, y + 30, 28, accent);
        string body = resetConfirm ? "現在の未保存の進行は失われます。\n保存した町は F9 で読み込めます。" :
            "1. 道路を最初の入口 [0, 12] へつなげます。\n2. 道路に隣接して住宅・商業地・工業地を建設します。\n3. 職場を増やすと住民が増え、毎月税金が入ります。\n4. 公園と低い税率で幸福度を保ちましょう。\n5. 条件を6か月満たすと成長（通常Lv.3 / super Lv.6 / ultra Lv.9）。\n\n1〜9・0・T・U 建設選択 / U：ultra（3×3） / 左クリック 建設\n右ドラッグ 回転 / Shift＋右ドラッグ 移動\nWASD・中ドラッグ 移動 / Q・E 回転 / L 土地購入\nR・F / Shift＋ホイール 俯角調整（10〜85度）\nホイール 拡大縮小 / Home カメラ初期化 / Alt＋Enter 全画面\nSPACE 停止・再開 / F5 保存 / F9 読込 / F1 操作\n\n5秒で1か月。土地は24×24、初回100万円・購入ごとに50万円増。\n目標は人口500人。達成後も自由に町を育てられます。";
        Text(body, x + 35, y + 91, 18);
        Rectangle close = new(x + 35, y + 467, 310, 44);
        Panel(close, accent); Text(resetConfirm ? "続ける（キャンセル）" : "町づくりを続ける", x + 52, y + 480, 18, ink);
        if (Hit(close) && IsMouseButtonPressed(MouseButton.Left)) { help = false; resetConfirm = false; }
        if (resetConfirm)
        {
            Rectangle confirm = new(x + 370, y + 467, 330, 44);
            Panel(confirm, KindColor(TileKind.Bulldoze)); Text("新しい町を作る", x + 390, y + 480, 18, ink);
            if (Hit(confirm) && IsMouseButtonPressed(MouseButton.Left)) { city = new(); elapsed = 0; speed = 1; selected = TileKind.Empty; specialOpen = false; resetConfirm = false; landMode = false; ResetCamera(); UpdateCamera(); RefreshView(true); message = "新しい町を作りました"; }
        }
    }
    private void DrawSpecialModal()
    {
        if (!city.SpecialsUnlocked) { specialOpen = false; return; }
        DrawRectangle(0, 0, W, H, new Color(9, 20, 29, 205));
        float x = (W - 720) / 2f, y = (H - 550) / 2f;
        Panel(new(x, y, 720, 550), ink);
        Text("スペシャル", x + 30, y + 26, 28, accent);
        Text("施設を選んでから、町の空き地をクリックして設置", x + 30, y + 72, 18, muted);
        const int pageSize = 4;
        int pages = (Specials.Length + pageSize - 1) / pageSize;
        specialPage = Math.Clamp(specialPage, 0, pages - 1);
        bool click = !specialJustOpened && IsMouseButtonPressed(MouseButton.Left);
        specialJustOpened = false;
        for (int row = 0; row < pageSize; row++)
        {
            int index = specialPage * pageSize + row;
            if (index >= Specials.Length) break;
            TileKind kind = Specials[index];
            Rectangle card = new(x + 30, y + 116 + row * 84, 660, 74);
            Panel(card, Hit(card) ? new Color(57, 78, 89, 255) : panel);
            Text($"{City.Name(kind)}   {City.Footprint(kind)}×{City.Footprint(kind)}   ¥{City.Cost(kind):N0}", card.X + 18, card.Y + 10, 22, accent);
            Text(kind == TileKind.GrandPark ? "Lv.1固定 / 16種 / 月120円 / 5マス内：接続＋20・未接続＋6（公園経由可）" : kind == TileKind.Supermarket ? "Lv.1固定 / 16種 / 維持費160円・月 / 接続で5マス内：住宅＋10・商業税収＋20％" : kind == TileKind.LogisticsCenter ? "Lv.1固定 / 16種 / 維持費180円・月 / 接続で5マス内の工業・商業税収＋20％" : "クリックして建設を選択", card.X + 18, card.Y + 43, 15, paper);
            if (click && Hit(card))
            {
                selected = kind; landMode = false; specialOpen = false; hasHover = false; painted = null;
                message = $"スペシャル：{City.Name(kind)} / 空いた{City.Footprint(kind)}×{City.Footprint(kind)}マスを選択";
            }
        }
        Rectangle previous = new(x + 30, y + 474, 110, 44), next = new(x + 260, y + 474, 110, 44), close = new(x + 490, y + 474, 200, 44);
        Panel(previous, panel); Text("前のページ", previous.X + 12, previous.Y + 13, 16, specialPage > 0 ? paper : muted);
        Text($"{specialPage + 1} / {pages}", x + 177, y + 487, 18);
        Panel(next, panel); Text("次のページ", next.X + 12, next.Y + 13, 16, specialPage + 1 < pages ? paper : muted);
        Panel(close, accent); Text("閉じる / Esc", close.X + 22, close.Y + 13, 18, ink);
        if (click)
        {
            if (Hit(close)) specialOpen = false;
            if (Hit(previous)) specialPage = Math.Max(0, specialPage - 1);
            if (Hit(next)) specialPage = Math.Min(pages - 1, specialPage + 1);
        }
    }
    private void DrawLandModal(ChunkPos land)
    {
        DrawRectangle(0, 0, W, H, new Color(9, 20, 29, 205));
        float x = (W - 650) / 2f, y = (H - 310) / 2f;
        Panel(new(x, y, 650, 310), ink);
        Text("土地を購入しますか？", x + 30, y + 28, 27, accent);
        Text($"区画 [{land.X}, {land.Z}]  /  24×24タイル", x + 30, y + 87, 21);
        Text($"価格 ¥{city.LandPrice:N0}   所持金 ¥{city.Money:N0}", x + 30, y + 130, 20);
        bool allowed = city.Money >= city.LandPrice && city.CanPurchase(land);
        Text(allowed ? "購入後は撤回・売却できません。" : "購入に必要な資金が足りません。", x + 30, y + 170, 17, muted);
        Rectangle cancel = new(x + 30, y + 235, 270, 44), buy = new(x + 350, y + 235, 270, 44);
        Panel(cancel, paper); Text("キャンセル", x + 52, y + 248, 18, ink);
        Panel(buy, allowed ? accent : panel); Text($"¥{city.LandPrice:N0} で購入", x + 370, y + 248, 18, allowed ? ink : muted);
        if (purchaseJustOpened) { purchaseJustOpened = false; return; }
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            if (Hit(cancel)) purchaseConfirm = null;
            else if (allowed && Hit(buy)) { message = city.Purchase(land); purchaseConfirm = null; hoverLand = null; RefreshView(true); }
        }
    }
    private void Save()
    {
        try { city.Save(savePath); message = "町を保存しました / F9 で読込"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { message = "保存に失敗しました。保存先の権限と空き容量を確認してください"; }
    }
    private void Load()
    {
        try { city = City.Load(savePath); elapsed = 0; landMode = false; specialOpen = false; selected = TileKind.Empty; ResetCamera(); UpdateCamera(); RefreshView(true); message = "保存した町を読み込みました"; }
        catch (InvalidDataException ex) { message = ex.Message; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { message = "読込できませんでした。保存データの存在と形式を確認してください"; }
    }
    public void Dispose() { roads.Dispose(); terrain.Dispose(); buildingFacades.Dispose(); UnloadFont(font); CloseWindow(); }
}
