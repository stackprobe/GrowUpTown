using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;

namespace GrowUpTown;

internal static class LargeSpecialDrawing
{
    internal static void Gable(float x, float z, float w, float d, float h, Color c, float rise = .4f)
    {
        Hall(x, z, w, d, h, c, roofDetails: false);
        float y = h + .155f;
        Vector3 a = new(x - w / 2, y, z - d / 2), b = new(x + w / 2, y, z - d / 2);
        Vector3 e = new(x - w / 2, y, z + d / 2), f = new(x + w / 2, y, z + d / 2);
        Vector3 p = new(x, y + rise, z - d / 2), q = new(x, y + rise, z + d / 2);
        SpecialBuildingDetails.RoofSlope(a, e, q, p, c);
        SpecialBuildingDetails.RoofSlope(b, p, q, f, White);
        DrawTriangle3D(e, f, q, c); DrawTriangle3D(a, p, b, c);
        for (int i = -2; i <= 2; i++) Beam(new(x + i * w / 6, y + rise * (1 - Math.Abs(i) / 3f), z - d / 2),
            new(x + i * w / 6, y + rise * (1 - Math.Abs(i) / 3f), z + d / 2), .012f, Steel);
    }
    internal static void Dome(float x, float z, float w, float d, float h, Color c)
    {
        Hall(x, z, w, d, h, c, roofDetails: false);
        // Build only the upper shell in facility coordinates, with its rim on the roof.
        Vector3 Point(float elevation, float angle) => new(x + MathF.Sin(elevation) * MathF.Cos(angle) * w / 2,
            h + .155f + MathF.Cos(elevation) * .65f, z + MathF.Sin(elevation) * MathF.Sin(angle) * d / 2);
        for (int ring = 0; ring < 8; ring++) for (int segment = 0; segment < 24; segment++)
        {
            float a = ring * MathF.PI / 16, b = (ring + 1) * MathF.PI / 16;
            float p = segment * MathF.Tau / 24, q = (segment + 1) * MathF.Tau / 24;
            Color shade = segment < 12 ? c : new((int)(c.R * .88f), (int)(c.G * .88f), (int)(c.B * .88f), 255);
            DrawTriangle3D(Point(a, p), Point(a, q), Point(b, q), shade);
            DrawTriangle3D(Point(a, p), Point(b, q), Point(b, p), shade);
            if (segment % 4 == 0) Beam(Point(a, p), Point(b, p), .012f, White);
        }
        Hall(x, z + d / 2 + .24f, Math.Min(w, 2.3f), .48f, .45f, c);
    }
    internal static void Atrium(float x, float z, float w, float d, float h)
    {
        Box(x, .1f, z, w, h, d, Glass);
        for (float px = -w / 2; px <= w / 2 + .01f; px += .25f)
            Box(x + px, .1f, z, .018f, h + .025f, d + .02f, White);
        Box(x, .1f + h, z, w + .04f, .025f, d + .04f, Steel);
    }
    internal static void Parking(float z, Color c)
    {
        Box(0, .103f, z, 3.7f, .009f, .52f, Steel);
        for (int i = -5; i <= 5; i++)
        {
            if (i == 0) continue;
            Box(i * .32f, .114f, z, .012f, .006f, .40f, White);
            if (i is -5 or -3 or 2 or 4)
            {
                Box(i * .32f + .15f, .116f, z, .14f, .12f, .27f, i < 0 ? c : White);
                Box(i * .32f + .15f, .236f, z, .115f, .045f, .13f, Glass);
            }
        }
    }
    internal static void Arcade(float x, float z, float w, Color c)
    {
        for (float px = -w / 2; px <= w / 2; px += .35f) Box(x + px, .1f, z, .028f, .36f, .028f, White);
        Box(x, .46f, z, w + .08f, .035f, .4f, c);
    }
}
