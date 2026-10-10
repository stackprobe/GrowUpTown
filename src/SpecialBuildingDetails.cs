using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// Repeating materials are baked once. A hall needs four wall quads and one
// roof quad, independent of its footprint or number of windows.
internal static class SpecialBuildingDetails
{
    private static readonly Texture2D[] facades = new Texture2D[4];
    private static Texture2D roof, paving;
    internal static int Style { get; set; }

    internal static void Initialize()
    {
        for (int style = 0; style < facades.Length; style++)
        {
            Color wall = style switch
            {
                1 => new(224, 225, 206, 255),
                2 => new(210, 220, 225, 255),
                3 => new(206, 211, 204, 255),
                _ => new(232, 235, 226, 255)
            };
            Image image = GenImageColor(128, 128, wall);
            // Stone courses / timber slats / metal panels, with recessed sills.
            int step = style == 1 ? 16 : 32;
            for (int y = 0; y < 128; y += step)
            {
                ImageDrawRectangle(ref image, 0, y, 128, 2, Shade(wall, .90f));
                if (style != 1)
                    ImageDrawRectangle(ref image, y % 64 == 0 ? 32 : 96, y, 2, step, Shade(wall, .94f));
            }
            ImageDrawRectangle(ref image, 0, 114, 128, 14, Shade(wall, .83f));
            ImageDrawRectangle(ref image, 0, 111, 128, 3, new(247, 247, 234, 255));
            for (int x = 10; x < 128; x += 64)
            {
                ImageDrawRectangle(ref image, x - 3, 29, 50, 67, new(139, 161, 169, 255));
                ImageDrawRectangle(ref image, x, 32, 44, 58, new(65, 119, 147, 255));
                ImageDrawRectangle(ref image, x + 3, 35, 38, 23, new(114, 181, 199, 255));
                ImageDrawRectangle(ref image, x + 3, 61, 38, 26, new(81, 148, 174, 255));
                ImageDrawRectangle(ref image, x + 20, 32, 3, 58, new(228, 235, 229, 255));
                ImageDrawRectangle(ref image, x - 4, 94, 52, 4, new(248, 246, 231, 255));
            }
            facades[style] = Upload(ref image);
        }
        Image top = GenImageColor(128, 128, new(218, 226, 229, 255));
        for (int x = 0; x < 128; x += 32)
        {
            ImageDrawRectangle(ref top, x, 0, 2, 128, new(162, 182, 191, 255));
            ImageDrawRectangle(ref top, x + 2, 0, 2, 128, new(239, 244, 242, 255));
        }
        ImageDrawRectangle(ref top, 0, 0, 128, 2, new(176, 194, 201, 255));
        roof = Upload(ref top);
        Image ground = GenImageColor(128, 128, new(216, 219, 202, 255));
        for (int y = 0; y < 128; y += 32)
        for (int x = 0; x < 128; x += 32)
        {
            int variation = (x / 32 * 7 + y / 32 * 3) % 5;
            ImageDrawRectangle(ref ground, x + 1, y + 1, 30, 30,
                new(223 - variation * 2, 226 - variation * 2, 209 - variation * 2, 255));
        }
        paving = Upload(ref ground);
    }

    private static Color Shade(Color c, float factor) => new((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor), 255);
    private static Texture2D Upload(ref Image image)
    {
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        GenTextureMipmaps(ref texture);
        SetTextureFilter(texture, TextureFilter.Trilinear);
        SetTextureWrap(texture, TextureWrap.Repeat);
        return texture;
    }

    internal static void Walls(float x, float z, float w, float d, float h)
    {
        Rlgl.SetTexture(facades[Math.Clamp(Style / 4, 0, 3)].Id);
        Rlgl.Begin(DrawMode.Quads);
        for (int face = 0; face < 4; face++)
        {
            Vector3 normal = face switch { 0 => Vector3.UnitZ, 1 => Vector3.UnitX, 2 => -Vector3.UnitZ, _ => -Vector3.UnitX };
            Vector3 across = new(normal.Z, 0, -normal.X);
            float width = face % 2 == 0 ? w : d, depth = face % 2 == 0 ? d : w;
            Vector3 center = new Vector3(x, .10f, z) + normal * (depth / 2 + .002f);
            byte shade = face switch { 1 => 224, 2 => 237, 3 => 218, _ => 255 };
            Rlgl.Color4ub(shade, shade, shade, 255);
            Quad(center - across * width / 2, center + across * width / 2,
                center + across * width / 2 + Vector3.UnitY * h,
                center - across * width / 2 + Vector3.UnitY * h, width / .48f, h / .36f);
        }
        Rlgl.End(); Rlgl.SetTexture(0);
    }

    internal static void Roof(float x, float y, float z, float w, float d, Color tint) =>
        Surface(roof, x, y, z, w, d, .8f, tint);
    internal static void RoofSlope(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color tint)
    {
        Rlgl.SetTexture(roof.Id); Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(tint.R, tint.G, tint.B, 255);
        Quad(a, b, c, d, Vector3.Distance(a, b) / .8f, Vector3.Distance(a, d) / .8f);
        Rlgl.End(); Rlgl.SetTexture(0);
    }
    internal static void Paving(float x, float y, float z, float w, float d) =>
        Surface(paving, x, y, z, w, d, .8f, Color.White);

    private static void Surface(Texture2D texture, float x, float y, float z, float w, float d, float repeat, Color tint)
    {
        Rlgl.SetTexture(texture.Id); Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(tint.R, tint.G, tint.B, 255);
        Quad(new(x - w / 2, y, z + d / 2), new(x + w / 2, y, z + d / 2),
            new(x + w / 2, y, z - d / 2), new(x - w / 2, y, z - d / 2), w / repeat, d / repeat);
        Rlgl.End(); Rlgl.SetTexture(0);
    }
    private static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
    {
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, d - a));
        Rlgl.Normal3f(normal.X, normal.Y, normal.Z);
        Rlgl.TexCoord2f(0, v); Rlgl.Vertex3f(a.X, a.Y, a.Z);
        Rlgl.TexCoord2f(u, v); Rlgl.Vertex3f(b.X, b.Y, b.Z);
        Rlgl.TexCoord2f(u, 0); Rlgl.Vertex3f(c.X, c.Y, c.Z);
        Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(d.X, d.Y, d.Z);
    }

    internal static void Shutdown()
    {
        foreach (Texture2D texture in facades) UnloadTexture(texture);
        UnloadTexture(roof); UnloadTexture(paving);
    }
}
