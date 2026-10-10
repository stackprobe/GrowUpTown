using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// Generated once, then shared by every rail tile; no per-stone draw calls.
internal sealed class RailwayBallast : IDisposable
{
    private readonly Texture2D texture;
    private static uint Hash(int x, int y)
    {
        uint h = unchecked((uint)x * 73856093u ^ (uint)y * 19349663u ^ 0x517cc1b7u);
        h ^= h >> 16; h *= 2246822519u; return h ^ (h >> 13);
    }
    internal RailwayBallast()
    {
        const int size = 128, stoneSize = 7;
        var image = GenImageColor(size, size, new Color(101, 96, 84, 255));
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float nearest = float.MaxValue, second = float.MaxValue, stoneX = 0, stoneY = 0;
            uint chosen = 0;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                int gx = x / stoneSize + dx, gy = y / stoneSize + dy;
                uint seed = Hash(gx, gy);
                float px = gx * stoneSize + 1 + seed % 50 / 10f;
                float py = gy * stoneSize + 1 + (seed >> 8) % 50 / 10f;
                float distance = (x - px) * (x - px) + (y - py) * (y - py);
                if (distance < nearest)
                {
                    second = nearest; nearest = distance; chosen = seed; stoneX = px; stoneY = py;
                }
                else if (distance < second) second = distance;
            }
            int tone = 112 + (int)(chosen % 58);
            // Dark seams, pale lit facets, and fine grain make irregular crushed stones.
            if (second - nearest < 3) tone -= 39;
            else tone += x + y < stoneX + stoneY ? 18 : -10;
            tone += (int)(Hash(x, y) % 13) - 6;
            bool warm = (chosen & 3) == 0;
            ImageDrawPixel(ref image, x, y, new Color(tone + (warm ? 9 : 0),
                tone, tone - (warm ? 15 : 3), 255));
        }
        texture = LoadTextureFromImage(image); UnloadImage(image);
        GenTextureMipmaps(ref texture);
        SetTextureFilter(texture, TextureFilter.Trilinear);
    }
    internal void Draw(IReadOnlyList<WorldChunk> chunks, int originX, int originZ)
    {
        Rlgl.SetTexture(texture.Id); Rlgl.Begin(DrawMode.Quads); Rlgl.Color4ub(255, 255, 255, 255);
        foreach (var chunk in chunks) foreach (var (cell, tile) in chunk.Cells)
        {
            // Keep the asphalt surface at level crossings.
            if (tile.Kind != TileKind.Rail) continue;
            int wx = chunk.Position.OriginX + cell % City.Size, wz = chunk.Position.OriginZ + cell / City.Size;
            float x = wx - originX - .5f, z = wz - originZ - .5f;
            uint variation = Hash(wx, wz);
            float u = (variation & 1) == 0 ? 0 : 1, v = (variation & 2) == 0 ? 0 : 1;
            Rlgl.TexCoord2f(u, v); Rlgl.Vertex3f(x, .078f, z);
            Rlgl.TexCoord2f(u, 1 - v); Rlgl.Vertex3f(x, .078f, z + 1);
            Rlgl.TexCoord2f(1 - u, 1 - v); Rlgl.Vertex3f(x + 1, .078f, z + 1);
            Rlgl.TexCoord2f(1 - u, v); Rlgl.Vertex3f(x + 1, .078f, z);
        }
        Rlgl.End(); Rlgl.SetTexture(0);
    }
    public void Dispose() => UnloadTexture(texture);
}
