using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// Sixteen reusable connection patterns, including all rotations of bends/T-junctions.
internal sealed class RoadRenderer : IDisposable
{
    internal const float SurfaceHeight = .071f;
    internal const float DeckTop = SurfaceHeight - .005f;
    private const int Cell = 128, Inset = 4, Art = 120, AtlasSize = Cell * 4;
    private readonly Texture2D atlas;
    internal static bool IsCurve(int mask) => mask is 3 or 6 or 9 or 12;
    internal static bool IsIntersection(int mask) => BitOperations.PopCount((uint)mask) >= 3;

    internal RoadRenderer()
    {
        Color paving = new(179, 188, 177, 255), edge = new(139, 156, 153, 255);
        Color asphalt = new(88, 105, 112, 255), lane = new(221, 216, 174, 255), crossing = new(228, 232, 213, 255);
        var image = GenImageColor(AtlasSize, AtlasSize, paving);
        for (int mask = 0; mask < 16; mask++)
        {
            bool south = (mask & 1) != 0, east = (mask & 2) != 0, north = (mask & 4) != 0, west = (mask & 8) != 0;
            bool curve = IsCurve(mask), junction = IsIntersection(mask);
            for (int py = -Inset; py < Art + Inset; py++) for (int px = -Inset; px < Art + Inset; px++)
            {
                // Extend border texels into gutters for clean bilinear sampling.
                float x = (Math.Clamp(px, 0, Art - 1) + .5f) / Art - .5f;
                float z = (Math.Clamp(py, 0, Art - 1) + .5f) / Art - .5f;
                float ax = Math.Abs(x), az = Math.Abs(z);
                bool road, curb, marking = false, zebra = false;
                if (curve)
                {
                    float cx = east ? .5f : -.5f, cz = south ? .5f : -.5f;
                    float radius = MathF.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
                    road = Math.Abs(radius - .5f) <= .32f;
                    curb = Math.Abs(radius - .5f) <= .35f;
                    float arc = MathF.Atan2(Math.Abs(z - cz), Math.Abs(x - cx)) * .5f;
                    marking = Math.Abs(radius - .5f) < .013f && arc % .20f < .115f;
                }
                else
                {
                    bool Contains(float half) => (ax <= half && az <= half)
                        || (south && z >= 0 && ax <= half) || (north && z <= 0 && ax <= half)
                        || (east && x >= 0 && az <= half) || (west && x <= 0 && az <= half);
                    road = Contains(.32f); curb = Contains(.35f);
                    bool vertical = ax < .013f && ((south && z > 0) || (north && z < 0));
                    bool horizontal = az < .013f && ((east && x > 0) || (west && x < 0));
                    marking = (vertical && (z + .5f) % .25f < .14f) || (horizontal && (x + .5f) % .25f < .14f);
                    if (mask == 0) marking = ax < .15f && az < .013f;
                    if (junction)
                    {
                        // Clear the center, with crosswalks only on connected arms.
                        marking &= ax > .42f || az > .42f;
                        bool onVertical = (south && z > .32f && z < .41f) || (north && z < -.32f && z > -.41f);
                        bool onHorizontal = (east && x > .32f && x < .41f) || (west && x < -.32f && x > -.41f);
                        zebra = (onVertical && ax < .27f && (x + .3f) % .10f < .055f)
                            || (onHorizontal && az < .27f && (z + .3f) % .10f < .055f);
                    }
                }
                Color color = road ? zebra ? crossing : marking ? lane : asphalt : curb ? edge : paving;
                ImageDrawPixel(ref image, mask % 4 * Cell + Inset + px, mask / 4 * Cell + Inset + py, color);
            }
        }
        atlas = LoadTextureFromImage(image); UnloadImage(image);
        SetTextureFilter(atlas, TextureFilter.Bilinear);
    }
    internal void Draw(City city, IReadOnlyList<WorldChunk> chunks, int originX, int originZ)
    {
        Rlgl.SetTexture(atlas.Id); Rlgl.Begin(DrawMode.Quads); Rlgl.Color4ub(255, 255, 255, 255);
        foreach (var chunk in chunks) foreach (var (cell, tile) in chunk.Cells)
        {
            if (tile.Kind != TileKind.Road) continue;
            int wx = chunk.Position.OriginX + cell % City.Size, wz = chunk.Position.OriginZ + cell / City.Size;
            float x = wx - originX - .5f, z = wz - originZ - .5f;
            int mask = city.RoadMask(wx, wz);
            float y = SurfaceHeight;
            float width = 1, depth = 1;
            float u0 = (mask % 4 * Cell + Inset) / (float)AtlasSize, v0 = (mask / 4 * Cell + Inset) / (float)AtlasSize;
            float u1 = u0 + Art / (float)AtlasSize, v1 = v0 + Art / (float)AtlasSize;
            Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(x, y, z);
            Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(x, y, z + depth);
            Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(x + width, y, z + depth);
            Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(x + width, y, z);
        }
        Rlgl.End(); Rlgl.SetTexture(0);
    }
    public void Dispose() => UnloadTexture(atlas);
}
