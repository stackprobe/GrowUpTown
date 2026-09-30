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
    private readonly string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrowUpTown", "city.json");
    private City city = new();
    private TileKind selected = TileKind.Home;
    private Camera3D camera;
    private Vector3 focus = new(11, 0, 12);
    private static readonly float DefaultPitch = MathF.Atan2(32, 28);
    private float pitch = DefaultPitch;
    private float zoom = 30, angle = .78f, elapsed;
    private int speed = 1, hoverX, hoverZ;
    private bool help, resetConfirm;
    private string message = "町づくりへようこそ。道路沿いに住宅を建ててみましょう。";
    private static readonly TileKind[] Tools = [TileKind.Road, TileKind.Home, TileKind.Shop, TileKind.Factory, TileKind.Park, TileKind.Bulldoze, TileKind.Water];
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
    public void Run(bool smokeTest)
    {
        int frames = 0;
        while (!WindowShouldClose())
        {
            Update();
            BeginDrawing();
            ClearBackground(new Color(192, 213, 211, 255));
            DrawWorld();
            DrawUi();
            if (help || resetConfirm || purchaseConfirm.HasValue) DrawModal();
            EndDrawing();
            if (smokeTest && ++frames >= 8)
            {
                string? screenshot = Environment.GetEnvironmentVariable("GROWUPTOWN_SCREENSHOT");
                if (!string.IsNullOrEmpty(screenshot))
                {
                    var capture = LoadImageFromScreen();
                    ExportImage(capture, screenshot);
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
        if (IsKeyPressed(KeyboardKey.F1) && !purchaseConfirm.HasValue) help = !help;
        if (IsKeyPressed(KeyboardKey.Escape)) { help = false; resetConfirm = false; purchaseConfirm = null; landMode = false; selected = TileKind.Empty; }
        if (help || resetConfirm || purchaseConfirm.HasValue) { rightDrag.Cancel(); return; }
        float dt = Math.Min(GetFrameTime(), .1f);
        if (IsKeyPressed(KeyboardKey.Space)) speed = speed == 0 ? 1 : 0;
        if (IsKeyPressed(KeyboardKey.F5)) Save();
        if (IsKeyPressed(KeyboardKey.F9)) Load();
        if (IsKeyPressed(KeyboardKey.L)) { landMode = !landMode; selected = TileKind.Empty; }
        for (int i = 0; i < Tools.Length; i++)
            if (IsKeyPressed((KeyboardKey)((int)KeyboardKey.One + i))) { selected = Tools[i]; landMode = false; }
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
        bool overWorld = GetMouseX() > 290 && GetMouseY() > 114 && GetMouseY() < H - 76
            && !Hit(new(W - 354, 124, 340, 107));
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
        foreach (var point in new[] { new Vector2(290, 108), new Vector2(W, 108), new Vector2(W, H - 76), new Vector2(290, H - 76) })
        {
            var ray = GetScreenToWorldRay(point, camera);
            foreach (float height in new[] { WaterRenderer.WaterHeight, 4f })
            {
                var p = ray.Position + ray.Direction * ((height - ray.Position.Y) / ray.Direction.Y);
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            }
        }
        viewMinX = ClampWorldCoordinate(originX + Math.Floor(minX) - 1); viewMaxX = ClampWorldCoordinate(originX + Math.Ceiling(maxX) + 1);
        viewMinZ = ClampWorldCoordinate(originZ + Math.Floor(minZ) - 1); viewMaxZ = ClampWorldCoordinate(originZ + Math.Ceiling(maxZ) + 1);
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
        TileKind.Road => new(122, 142, 155, 255), TileKind.Home => new(161, 216, 124, 255),
        TileKind.Shop => new(104, 189, 218, 255), TileKind.Factory => new(231, 171, 92, 255),
        TileKind.Water => new(76, 161, 188, 255), TileKind.Park => new(100, 187, 153, 255), TileKind.Bulldoze => new(233, 131, 117, 255), _ => paper
    };
    private static void Box(float x, float y, float z, float w, float h, float d, Color color) => DrawCube(new(x, y + h / 2, z), w, h, d, color);
    private void Tree(float x, float z, float size = 1)
    {
        Box(x, 0, z, .09f, .45f * size, .09f, new(116, 100, 76, 255));
        DrawSphere(new(x, .58f * size, z), .3f * size, new(73, 135, 99, 255));
        DrawSphere(new(x - .07f, .77f * size, z), .23f * size, new(106, 164, 114, 255));
    }
    private void DrawFactorySmoke(int x, int z, float chimneyTop, int worldX, int worldZ, bool connected)
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
                    DrawCubeWires(new(x, .15f, z), .94f, .3f, .94f, new(222, 98, 88, 255));
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
            int x = hoverX - originX, z = hoverZ - originZ;
            bool valid = selected != TileKind.Empty && city.Money >= City.Cost(selected) && (selected == TileKind.Bulldoze ? city[hoverX, hoverZ].Kind != TileKind.Empty : (city[hoverX, hoverZ].Kind == TileKind.Empty || selected == TileKind.Road && city[hoverX, hoverZ].Kind == TileKind.Water));
            Color c = selected == TileKind.Empty ? paper : valid ? accent : KindColor(TileKind.Bulldoze);
            DrawCubeWires(new(x, .16f, z), 1, .30f, 1, c);
            if (valid && selected != TileKind.Bulldoze) Box(x, .06f, z, .88f, .1f, .88f, new Color(c.R, c.G, c.B, (byte)110));
        }
        EndMode3D();
    }
    private void Text(string s, float x, float y, float size = 18, Color? color = null) => DrawTextEx(font, s, new(x, y), size, 1, color ?? paper);
    private void Panel(Rectangle r, Color? color = null) => DrawRectangleRounded(r, .12f, 6, color ?? panel);
    private bool Button(Rectangle r, string label, bool active = false, Color? color = null)
    {
        bool enabled = !help && !resetConfirm && !purchaseConfirm.HasValue && !rotatingView;
        bool hot = enabled && Hit(r);
        Panel(r, active ? color ?? accent : hot ? new Color(57, 78, 89, 255) : new Color(40, 59, 71, 255));
        Text(label, r.X + 12, r.Y + (r.Height - 18) / 2, 18, active ? ink : paper);
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
        Panel(new(14, 122, 270, H - 208), ink);
        Text("BUILD / 建設", 30, 140, 21);
        Text("道路入口 [0, 12] から町を広げよう", 30, 173, 14, muted);
        for (int i = 0; i < Tools.Length; i++)
        {
            var kind = Tools[i];
            if (Button(new(28, 203 + i * 37, 242, 32), $"{i + 1}   {City.Name(kind)}     ¥{City.Cost(kind)}", !landMode && selected == kind, KindColor(kind))) { selected = kind; landMode = false; }
        }
        if (Button(new(28, 469, 242, 37), "L   土地購入  ¥1,000,000", landMode)) { landMode = !landMode; selected = TileKind.Empty; }
        Text($"町の運営  /  {city.Chunks.Count}区画", 30, 514, 19);
        Text($"税率  {city.TaxRate}%", 30, 551, 20);
        if (Button(new(166, 544, 45, 36), "−")) city.TaxRate = Math.Max(5, city.TaxRate - 1);
        if (Button(new(220, 544, 45, 36), "+")) city.TaxRate = Math.Min(20, city.TaxRate + 1);
        Text($"税収     ¥ {city.LastIncome:N0}", 30, 597, 16, muted);
        Text($"維持費  ¥ {city.LastExpense:N0}", 30, 625, 16, muted);
        Text($"仕事の定員  {city.Jobs}人", 30, 653, 16, muted);
        if (H >= 880)
        {
            Text("発展のヒント", 30, 700, 18, accent);
            Text("住宅と職場を道路沿いに。\n公園は近隣の幸福度を改善。\n工場は住宅から離しましょう。", 30, 733, 15, muted);
        }
        Panel(new(W - 354, 124, 340, 54), ink);
        Text($"{(city.Month - 1) / 12 + 1}年 {(city.Month - 1) % 12 + 1}月", W - 340, 141, 20);
        int[] speeds = [0, 1, 2, 4];
        for (int i = 0; i < speeds.Length; i++) if (Button(new(W - 219 + i * 51, 133, 46, 36), i == 0 ? "Ⅱ" : $"{speeds[i]}x", speed == speeds[i])) speed = speeds[i];
        DrawRectangle(W - 340, 172, (int)(312 * elapsed / 5), 2, accent);
        Panel(new(W - 354, 188, 340, 43), ink);
        Text($"目標：人口 500人     {Math.Min(100, city.Population * 100 / 500)}%", W - 337, 201, 16, city.Population >= 500 ? accent : paper);
        if (hasHover)
        {
            Tile t = city[hoverX, hoverZ];
            Panel(new(306, H - 159, Math.Min(610, W - 325), 68), ink);
            Text($"{City.Name(t.Kind)}   [{hoverX}, {hoverZ}]" + (t.Kind is TileKind.Home or TileKind.Shop or TileKind.Factory ? $"  Lv.{t.Level}" : ""), 324, H - 147, 18, accent);
            Text(t.Kind == TileKind.Empty ? "左クリックで建設 / 道路・水域はドラッグで連続配置" : (t.Kind == TileKind.Water ? "水辺が近隣の住環境を改善" : $"{(t.Connected ? "道路接続あり" : "道路未接続")}") + (t.Kind == TileKind.Home ? $"   住民 {t.Residents}/{t.Level * 24}人   住環境 {city.LocalComfort(hoverX, hoverZ):+0;-0;0}" : ""), 324, H - 117, 15, muted);
        }
        if (landMode)
        {
            Panel(new(306, H - 159, Math.Min(650, W - 325), 68), ink);
            Text(hoverLand is { } key ? $"土地 [{key.X}, {key.Z}]  24×24タイル / ¥1,000,000" : "土地購入：所有地に接する枠をクリック", 324, H - 147, 18, accent);
            Text(city.Money >= City.LandPrice ? "クリックで購入内容を確認 / Escで終了" : "購入資金が不足しています / Shift＋右ドラッグで移動", 324, H - 117, 15, muted);
        }
        DrawRectangle(0, H - 76, W, 76, ink);
        Text(message, 24, H - 64, 16, accent);
        Text("右ドラッグ：回転 / Shift＋右ドラッグ：移動 / L 土地購入 / F1 操作", 24, H - 34, 13, muted);
        if (Button(new(W - 410, H - 56, 94, 37), "F5 保存")) Save();
        if (Button(new(W - 307, H - 56, 94, 37), "F9 読込")) Load();
        if (Button(new(W - 204, H - 56, 94, 37), "新しい町")) resetConfirm = true;
        if (Button(new(W - 101, H - 56, 87, 37), "F1 操作")) help = true;
    }
    private void DrawModal()
    {
        if (purchaseConfirm is { } land) { DrawLandModal(land); return; }
        DrawRectangle(0, 0, W, H, new Color(9, 20, 29, 205));
        float x = (W - 740) / 2f, y = (H - 540) / 2f;
        Panel(new(x, y, 740, 540), ink);
        Text(resetConfirm ? "新しい町を始めますか？" : "小さな町を、大きな暮らしへ。", x + 35, y + 30, 28, accent);
        string body = resetConfirm ? "現在の未保存の進行は失われます。\n保存した町は F9 で読み込めます。" :
            "1. 道路を最初の入口 [0, 12] へつなげます。\n2. 道路に隣接して住宅・商業地・工業地を建設します。\n3. 職場を増やすと住民が増え、毎月税金が入ります。\n4. 公園と低い税率で幸福度を保ちましょう。\n5. 条件を6か月満たすと建物が成長します（最大 Lv.3）。\n\n1〜7 建設選択 / 左クリック 建設 / 道路・水域はドラッグ可\n右ドラッグ 回転 / Shift＋右ドラッグ 移動\nWASD・中ドラッグ 移動 / Q・E 回転 / L 土地購入\nR・F / Shift＋ホイール 俯角調整（10〜85度）\nホイール 拡大縮小 / Home カメラ初期化 / Alt＋Enter 全画面\nSPACE 停止・再開 / F5 保存 / F9 読込 / F1 操作\n\n5秒で1か月。土地は24×24、1区画100万円。\n目標は人口500人。達成後も自由に町を育てられます。";
        Text(body, x + 35, y + 91, 18);
        Rectangle close = new(x + 35, y + 467, 310, 44);
        Panel(close, accent); Text(resetConfirm ? "続ける（キャンセル）" : "町づくりを続ける", x + 52, y + 480, 18, ink);
        if (Hit(close) && IsMouseButtonPressed(MouseButton.Left)) { help = false; resetConfirm = false; }
        if (resetConfirm)
        {
            Rectangle confirm = new(x + 370, y + 467, 330, 44);
            Panel(confirm, KindColor(TileKind.Bulldoze)); Text("新しい町を作る", x + 390, y + 480, 18, ink);
            if (Hit(confirm) && IsMouseButtonPressed(MouseButton.Left)) { city = new(); elapsed = 0; speed = 1; resetConfirm = false; landMode = false; ResetCamera(); UpdateCamera(); RefreshView(true); message = "新しい町を作りました"; }
        }
    }
    private void DrawLandModal(ChunkPos land)
    {
        DrawRectangle(0, 0, W, H, new Color(9, 20, 29, 205));
        float x = (W - 650) / 2f, y = (H - 310) / 2f;
        Panel(new(x, y, 650, 310), ink);
        Text("土地を購入しますか？", x + 30, y + 28, 27, accent);
        Text($"区画 [{land.X}, {land.Z}]  /  24×24タイル", x + 30, y + 87, 21);
        Text($"価格 ¥1,000,000   所持金 ¥{city.Money:N0}", x + 30, y + 130, 20);
        bool allowed = city.Money >= City.LandPrice && city.CanPurchase(land);
        Text(allowed ? "購入後は撤回・売却できません。" : "購入に必要な資金が足りません。", x + 30, y + 170, 17, muted);
        Rectangle cancel = new(x + 30, y + 235, 270, 44), buy = new(x + 350, y + 235, 270, 44);
        Panel(cancel, paper); Text("キャンセル", x + 52, y + 248, 18, ink);
        Panel(buy, allowed ? accent : panel); Text("¥1,000,000 で購入", x + 370, y + 248, 18, allowed ? ink : muted);
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
        try { city = City.Load(savePath); elapsed = 0; landMode = false; ResetCamera(); UpdateCamera(); RefreshView(true); message = "保存した町を読み込みました"; }
        catch (InvalidDataException ex) { message = ex.Message; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { message = "読込できませんでした。保存データの存在と形式を確認してください"; }
    }
    public void Dispose() { roads.Dispose(); terrain.Dispose(); buildingFacades.Dispose(); UnloadFont(font); CloseWindow(); }
}
