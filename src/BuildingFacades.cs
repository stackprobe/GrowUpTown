using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// All 48 designs (plus ground-floor entrances) share one immutable texture.
// Detailed frames/signs are baked once, not submitted as hundreds of tiny cubes.
internal sealed class BuildingFacades : IDisposable
{
    private const int Cell = 128, Inset = 4, ArtSize = 120, Columns = 8, Rows = 12;
    private readonly Texture2D atlas;
    private static readonly Vector3[] Normals = [Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitX];
    private static readonly Vector3[] Across = [Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitZ];

    internal static float FloorHeight(TileKind kind) => kind switch { TileKind.UltraCity => .90f, TileKind.SuperHome => 1.02f, TileKind.SuperIndustryCommerce => .70f, TileKind.SuperMixed => .72f, TileKind.SuperFactory => .65f, TileKind.Home => .52f, TileKind.Shop => .70f, _ => .55f };
    internal static float Height(Tile tile) => FloorHeight(tile.Kind) * tile.Level;

    internal BuildingFacades()
    {
        var image = GenImageColor(Columns * Cell, Rows * Cell, Color.White);
        for (int kind = 0; kind < 3; kind++) for (int ground = 0; ground < 2; ground++)
            for (int style = 0; style < FacadeStyles.Count; style++)
            {
                int index = kind * 32 + ground * 16 + style;
                var painter = new Painter(image, index % Columns * Cell, index / Columns * Cell);
                painter.Paint(kind, style, ground == 1);
            }
        atlas = LoadTextureFromImage(image);
        UnloadImage(image);
        SetTextureFilter(atlas, TextureFilter.Bilinear);
    }

    internal void Draw(IReadOnlyList<WorldChunk> chunks, Camera3D camera, int originX, int originZ)
    {
        Vector3 towardCamera = camera.Position - camera.Target;
        Rlgl.SetTexture(atlas.Id);
        Rlgl.Begin(DrawMode.Quads);
        for (int face = 0; face < 4; face++)
        {
            if (Vector3.Dot(Normals[face], towardCamera) <= 0) continue;
            byte shade = face switch { 0 => 255, 1 => 226, 2 => 237, _ => 218 };
            Rlgl.Color4ub(shade, shade, shade, 255);
            foreach (var chunk in chunks)
            foreach (var (cell, tile) in chunk.Cells)
            {
                if (!tile.IsAnchor || !FacadeStyles.IsBuilding(tile.Kind)) continue;
                if (tile.Kind == TileKind.SuperHome) continue; // Glass tower geometry is drawn in Game.
                if (tile.Kind == TileKind.UltraCity)
                {
                    float ux = chunk.Position.OriginX - originX + cell % City.Size + 1;
                    float uz = chunk.Position.OriginZ - originZ + cell / City.Size + 1;
                    for (int floor = 0; floor < tile.Level; floor++)
                    {
                        float start = -1.16f;
                        for (int panel = 0; panel < 3; panel++)
                        {
                            float width = UltraAppearance.PanelWidth(tile, face, panel);
                            int category = UltraAppearance.Category(tile, face, panel);
                            int style = UltraAppearance.Style(tile, floor, face, panel);
                            Vector3 center = new Vector3(ux, .12f + floor * FloorHeight(tile.Kind), uz)
                                + Normals[face] * 1.162f + Across[face] * (start + width / 2);
                            DrawPanel(category * 32 + (floor == 0 ? 16 : 0) + style, center,
                                Across[face] * (width / 2 - .012f), FloorHeight(tile.Kind));
                            start += width;
                        }
                    }
                    continue;
                }
                bool large = City.Footprint(tile.Kind) == 2;
                float halfWidth = large ? .84f : .34f;
                Vector3 across = Across[face] * halfWidth;
                float x = chunk.Position.OriginX - originX + cell % City.Size + (large ? .5f : 0);
                float z = chunk.Position.OriginZ - originZ + cell / City.Size + (large ? .5f : 0);
                float story = FloorHeight(tile.Kind);
                for (int floor = 0; floor < tile.Level; floor++)
                {
                    int category = tile.Kind == TileKind.SuperMixed && floor == 0 ? 1 : FacadeStyles.Category(tile.Kind);
                    // Storefront and distribution offices face south; production occupies the other faces.
                    if (tile.Kind == TileKind.SuperIndustryCommerce && (face == 0 || face == 1 && floor == 0)) category = 1;
                    int index = category * 32 + (floor == 0 ? 16 : 0) + tile.FaceStyles[floor * 4 + face];
                    float u0 = (index % Columns * Cell + Inset) / (float)(Columns * Cell);
                    float v0 = (index / Columns * Cell + Inset) / (float)(Rows * Cell);
                    float u1 = u0 + ArtSize / (float)(Columns * Cell);
                    float v1 = v0 + ArtSize / (float)(Rows * Cell);
                    Vector3 center = new Vector3(x, .09f + floor * story, z) + Normals[face] * (halfWidth + .001f);
                    Vector3 left = center - across, right = center + across;
                    // Counter-clockwise viewed from outside; image origin is top-left.
                    Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(left.X, left.Y, left.Z);
                    Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(right.X, right.Y, right.Z);
                    Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(right.X, right.Y + story, right.Z);
                    Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(left.X, left.Y + story, left.Z);
                }
            }
        }
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public void Dispose() => UnloadTexture(atlas);

    private static void DrawPanel(int index, Vector3 center, Vector3 across, float height)
    {
        float u0 = (index % Columns * Cell + Inset) / (float)(Columns * Cell);
        float v0 = (index / Columns * Cell + Inset) / (float)(Rows * Cell);
        float u1 = u0 + ArtSize / (float)(Columns * Cell);
        float v1 = v0 + ArtSize / (float)(Rows * Cell);
        Vector3 left = center - across, right = center + across;
        Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(left.X, left.Y, left.Z);
        Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(right.X, right.Y, right.Z);
        Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(right.X, right.Y + height, right.Z);
        Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(left.X, left.Y + height, left.Z);
    }

    private sealed class Painter(Image image, int cellX, int cellY)
    {
        private Image image = image;
        private static readonly Color Frame = new(239, 234, 213, 255), Glass = new(53, 89, 106, 255);
        private static readonly Color Light = new(140, 192, 200, 255), Green = new(68, 128, 94, 255);
        private static readonly Color Wood = new(166, 111, 76, 255), Steel = new(87, 103, 108, 255);
        private static readonly Color Blue = new(49, 117, 143, 255), Rust = new(167, 109, 66, 255);
        private static readonly Color[] Homes = [new(169, 204, 143, 255), new(181, 211, 158, 255), new(163, 197, 143, 255), new(193, 216, 165, 255)];
        private static readonly Color[] Shops = [new(122, 189, 208, 255), new(139, 195, 210, 255), new(114, 179, 201, 255), new(153, 204, 212, 255)];
        private static readonly Color[] Factories = [new(219, 166, 100, 255), new(229, 184, 123, 255), new(208, 159, 103, 255), new(225, 175, 107, 255)];

        private void R(int x, int y, int w, int h, Color color) => ImageDrawRectangle(ref image, cellX + Inset + x, cellY + Inset + y, w, h, color);
        private void Window(int x, int y, int w, int h, bool grid = false)
        {
            R(x, y, w, h, Frame); R(x + 3, y + 3, w - 6, h - 6, Glass);
            R(x + 5, y + 5, 3, h - 10, Light);
            if (w >= 28) R(x + w / 2 - 1, y + 3, 2, h - 6, Frame);
            if (grid) R(x + 3, y + h / 2 - 1, w - 6, 2, Frame);
            R(x - 1, y + h, w + 2, 3, Frame);
        }
        private void Pair(bool grid = false) { Window(13, 28, 34, 43, grid); Window(73, 28, 34, 43, grid); }
        private void Planter(int x, int y, int w) { R(x, y, w, 8, Wood); R(x + 2, y - 5, w - 4, 7, Green); }
        private void Louvers(int x, int y, int w, int h)
        {
            R(x, y, w, h, Steel);
            for (int line = y + 4; line < y + h - 2; line += 6) R(x + 3, line, w - 6, 2, Frame);
        }
        private void Sign(int x, int y, int w, Color color)
        {
            R(x, y, w, 16, color); R(x + 6, y + 5, w - 12, 3, Frame);
            R(x + 9, y + 10, Math.Max(4, w / 2), 2, Frame);
        }
        private void Awning(int x, int y, int w, Color color)
        {
            R(x, y, w, 11, color);
            for (int stripe = x + 4; stripe < x + w - 2; stripe += 12) R(stripe, y, 5, 11, Frame);
            R(x, y + 11, w, 3, Steel);
        }

        internal void Paint(int kind, int style, bool ground)
        {
            Color wall = (kind == 0 ? Homes : kind == 1 ? Shops : Factories)[style % 4];
            ImageDrawRectangle(ref image, cellX, cellY, Cell, Cell, wall);
            if (kind == 0) Home(style, ground);
            else if (kind == 1) Shop(style, ground);
            else Factory(style, ground);
            // This cornice belongs to this floor and never moves on later growth.
            R(0, 0, ArtSize, 5, kind == 2 ? Rust : Frame);
            R(0, 115, ArtSize, 5, kind == 2 ? Steel : Frame);
        }

        private void Home(int style, bool ground)
        {
            switch (style)
            {
                case 0: Pair(); break; // Twin casements
                case 1: // Shutters
                    Pair(); foreach (int x in new[] { 6, 49, 66, 109 }) R(x, 28, 5, 43, Green); break;
                case 2: Pair(); Planter(12, 81, 36); Planter(72, 81, 36); break;
                case 3: Window(12, 36, 96, 34); R(44, 39, 3, 28, Frame); R(76, 39, 3, 28, Frame); break;
                case 4: // Bay window with an outlined apron
                    R(22, 24, 76, 56, Wood); Window(26, 28, 68, 42); R(26, 73, 68, 4, Frame); break;
                case 5: for (int x = 14; x < 110; x += 34) Window(x, 25, 24, 50); break;
                case 6: // Balcony / garden railing
                    Window(17, 23, 86, 49); R(12, 78, 96, 4, Frame);
                    for (int x = 16; x <= 104; x += 11) R(x, 80, 3, 16, Frame);
                    R(12, 96, 96, 4, Wood); break;
                case 7: Window(12, 26, 50, 49); Window(78, 38, 29, 29); break;
                case 8: // Brick skirt
                    R(0, 85, 120, 29, Wood); R(0, 98, 120, 2, Frame);
                    for (int x = 12; x < 120; x += 24) { R(x, 86, 2, 12, Frame); R(x + 10, 100, 2, 14, Frame); }
                    Pair(true); break;
                case 9: R(51, 5, 18, 110, Frame); Window(13, 22, 29, 58); Window(78, 22, 29, 58); break;
                case 10: Window(12, 20, 96, 22); Window(12, 53, 96, 22); break;
                case 11: Window(16, 34, 88, 42); Awning(11, 20, 98, Green); Planter(34, 83, 52); break;
                case 12: // Stairwell window and contrasting side panels
                    R(14, 20, 22, 66, Wood); R(84, 20, 22, 66, Wood);
                    Window(47, 16, 26, 31); Window(47, 55, 26, 31); break;
                case 13: Window(12, 24, 52, 57); Window(79, 20, 28, 25); Window(79, 56, 28, 25); break;
                case 14: // Recessed loggia
                    R(10, 18, 100, 79, Green); Window(18, 24, 35, 45); Window(67, 24, 35, 45);
                    R(13, 77, 94, 17, Frame); R(20, 82, 80, 6, Green); break;
                case 15: // Asymmetric vertical siding
                    R(10, 16, 26, 83, Wood); for (int x = 14; x < 35; x += 7) R(x, 17, 2, 81, Frame);
                    Window(49, 28, 57, 45, true); break;
            }
            if (ground) { R(51, 85, 18, 30, Frame); R(55, 88, 10, 27, Wood); R(62, 101, 2, 3, Frame); }
        }

        private void Shop(int style, bool ground)
        {
            switch (style)
            {
                case 0: Window(10, 36, 100, 65); Sign(10, 12, 100, Blue); break;
                case 1: Window(14, 42, 92, 59); Awning(8, 26, 104, Blue); break;
                case 2: Window(12, 35, 40, 61); Window(68, 35, 40, 61); Planter(13, 103, 38); Planter(69, 103, 38); Sign(24, 10, 72, Green); break;
                case 3: // Office grid
                    for (int y = 22; y <= 65; y += 43) for (int x = 12; x < 110; x += 34) Window(x, y, 27, 33); break;
                case 4: Window(9, 23, 102, 74); for (int x = 29; x < 110; x += 22) R(x, 26, 3, 68, Frame); break;
                case 5: Window(10, 33, 74, 68); Sign(88, 18, 25, Blue); R(91, 39, 19, 43, Blue); R(96, 45, 8, 27, Frame); break;
                case 6: Window(10, 38, 100, 58); Awning(7, 23, 106, Wood); R(9, 99, 102, 8, Wood); break;
                case 7: Window(12, 18, 96, 29); Window(12, 67, 96, 29); R(0, 52, 120, 9, Blue); break;
                case 8: Window(10, 39, 42, 62); Window(68, 39, 42, 62); Sign(10, 12, 42, Blue); Sign(68, 12, 42, Wood); break;
                case 9: R(8, 20, 104, 83, Blue); Window(16, 27, 88, 64); R(5, 101, 110, 5, Frame); break;
                case 10: for (int x = 10; x < 115; x += 27) Window(x, 24, 21, 72); break;
                case 11: Window(12, 25, 43, 67); R(65, 20, 42, 79, Green); for (int y = 28; y < 98; y += 17) R(71, y, 30, 6, Light); break;
                case 12: // Display with decorative poster panel
                    Window(8, 38, 69, 61); R(84, 42, 28, 45, Frame); R(88, 46, 20, 18, Wood); R(88, 70, 20, 4, Blue); Sign(8, 12, 104, Blue); break;
                case 13: Window(10, 21, 40, 80); Window(70, 21, 40, 80); R(55, 6, 10, 108, Frame); break;
                case 14: Window(13, 20, 94, 49); R(10, 81, 100, 4, Frame); for (int x = 17; x < 109; x += 13) R(x, 84, 3, 18, Frame); Planter(19, 104, 82); break;
                case 15: Window(12, 22, 96, 57, true); Louvers(14, 91, 92, 19); R(0, 7, 120, 6, Steel); break;
            }
            if (ground) { R(52, 79, 16, 36, Frame); R(55, 82, 10, 30, Glass); R(62, 96, 2, 5, Frame); }
        }

        private void Factory(int style, bool ground)
        {
            switch (style)
            {
                case 0: for (int x = 10; x < 115; x += 36) Window(x, 23, 28, 32, true); R(8, 78, 104, 7, Rust); break;
                case 1: Window(10, 18, 100, 29, true); R(10, 75, 100, 8, Steel); break;
                case 2: Window(12, 22, 37, 43, true); Louvers(66, 23, 42, 61); break;
                case 3: Window(9, 15, 30, 28); Window(81, 15, 30, 28); Louvers(29, 54, 62, 52); break;
                case 4: // Exposed pipework
                    Window(12, 23, 50, 42, true); R(82, 12, 8, 88, Steel); R(67, 37, 38, 7, Steel); R(78, 24, 16, 5, Frame); R(78, 80, 16, 5, Frame); break;
                case 5: Window(11, 21, 98, 60, true); R(35, 24, 3, 54, Frame); R(82, 24, 3, 54, Frame); break;
                case 6: // Braced bays
                    R(9, 6, 7, 108, Rust); R(57, 6, 6, 108, Rust); R(104, 6, 7, 108, Rust);
                    Window(22, 27, 28, 44, true); Window(70, 27, 28, 44, true); break;
                case 7: Window(12, 24, 39, 44, true); Window(69, 24, 39, 44, true); Planter(12, 87, 96); break;
                case 8: Window(11, 19, 98, 30, true); R(11, 64, 98, 40, Steel); for (int x = 13; x < 108; x += 14) R(x, 98, 7, 6, Frame); break;
                case 9: Window(12, 23, 42, 35, true); Louvers(69, 18, 39, 33); Louvers(69, 66, 39, 33); break;
                case 10: for (int x = 12; x < 111; x += 34) Window(x, 20, 25, 68, true); break;
                case 11: Window(10, 19, 100, 24); Window(10, 60, 100, 24); R(0, 98, 120, 6, Rust); break;
                case 12: Window(10, 25, 40, 48, true); Window(70, 25, 40, 48, true); Awning(7, 13, 106, Steel); break;
                case 13: R(8, 84, 104, 25, Rust); Window(13, 20, 94, 46, true); R(10, 92, 100, 3, Frame); break;
                case 14: Window(15, 20, 90, 55, true); R(12, 89, 96, 12, Green); R(19, 92, 82, 5, Light); break;
                case 15: Window(10, 22, 59, 44, true); R(84, 17, 19, 74, Steel); R(79, 30, 29, 7, Frame); R(79, 71, 29, 7, Frame); break;
            }
            if (ground) { R(48, 86, 24, 29, Steel); R(51, 89, 18, 22, Rust); R(64, 99, 3, 3, Frame); }
        }
    }
}
