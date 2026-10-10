using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;

namespace GrowUpTown;

internal static class FerrisWheelRenderer
{
    internal static void Wheel(float x, float z, float radius, int cabins, Color c, int frame)
    {
        float cy = radius + .55f;
        foreach (int side in new[] { -1, 1 })
        {
            Beam(new(x - radius * .48f, .10f, z + side * .35f), new(x, cy, z + side * .13f), .045f, White);
            Beam(new(x + radius * .48f, .10f, z + side * .35f), new(x, cy, z + side * .13f), .045f, White);
            for (int i = 0; i < 48; i++)
            {
                float a = i * MathF.Tau / 48, b = (i + 1) * MathF.Tau / 48;
                Beam(new(x + MathF.Cos(a) * radius, cy + MathF.Sin(a) * radius, z + side * .075f),
                    new(x + MathF.Cos(b) * radius, cy + MathF.Sin(b) * radius, z + side * .075f), .022f, c);
                if (frame == 1)
                    Beam(new(x + MathF.Cos(a) * radius * .82f, cy + MathF.Sin(a) * radius * .82f, z + side * .075f),
                        new(x + MathF.Cos(b) * radius * .82f, cy + MathF.Sin(b) * radius * .82f, z + side * .075f), .014f, White);
            }
        }
        if (frame == 2) Box(x, .10f, z, .22f, cy - .1f, .20f, Steel);
        Beam(new(x, cy, z - .30f), new(x, cy, z + .30f), .085f, Steel);
        for (int i = 0; i < cabins; i++)
        {
            float a = i * MathF.Tau / cabins - MathF.PI / 2;
            float px = x + MathF.Cos(a) * radius, py = cy + MathF.Sin(a) * radius;
            Beam(new(x, cy, z), new(px, py, z), .015f, White);
            Beam(new(px, py, z - .12f), new(px, py, z + .12f), .018f, Steel);
            // Cabins keep a fixed small size even on the largest wheel.
            Box(px, py - .22f, z, .17f, .19f, .16f, i % 3 == 0 ? new(225, 175, 84, 255) : c);
            Box(px, py - .16f, z + .086f, .125f, .085f, .012f, Glass);
            Box(px, py - .035f, z, .19f, .025f, .18f, White);
        }
        Box(x, .10f, z + .46f, radius * 1.2f, .06f, .44f, White);
        for (int i = -2; i <= 2; i++) Box(x + i * .17f, .1f, z + .83f, .018f, .13f, .25f, Steel);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        // Wheel proportions, count, supports and placement form sixteen fixed designs.
        switch (tile.LandmarkStyle)
        {
            case 0: Wheel(0, -.35f, 1.65f, 16, c, 0); Pool(-1.65f, 1.05f, .30f); Pool(1.65f, 1.05f, .30f); break;
            case 1: Wheel(0, -.65f, 1.40f, 12, c, 1); Pool(0, 1.18f, .52f); break;
            case 2: Wheel(-.85f, -.65f, .95f, 10, c, 0); Wheel(1.10f, .1f, .70f, 8, c, 0); break;
            case 3: Wheel(0, -.75f, 1.55f, 14, c, 2); Box(0, .105f, .95f, 3.5f, .025f, .48f, Wood); break;
            case 4: Wheel(0, -.55f, 1.45f, 12, c, 0); Tree(-1.65f, .9f); Tree(1.65f, .9f); break;
            case 5: Wheel(-.85f, -.45f, .92f, 10, c, 1); Wheel(.98f, -.45f, .82f, 8, c, 1); break;
            case 6: Wheel(0, -.9f, 1.2f, 10, c, 2); for (int i = -1; i <= 1; i++) Tree(i * 1.2f, 1.05f); break;
            case 7: Wheel(.25f, -.5f, 1.48f, 16, c, 1); Hall(-1.48f, 1.0f, .65f, .50f, .36f, c); break;
            case 8: Wheel(0, -.3f, 1.78f, 16, c, 1); break;
            case 9: Wheel(0, -.7f, 1.50f, 12, c, 2); Hall(-1.3f, 1.15f, .85f, .55f, .42f, c); Hall(1.3f, 1.15f, .85f, .55f, .42f, c); break;
            case 10: Wheel(-1.03f, -.7f, .83f, 8, c, 2); Wheel(1.03f, -.7f, .83f, 8, c, 2); Hall(0, 1, .8f, .6f, .55f, c); break;
            case 11: Wheel(0, -.8f, 1.65f, 18, c, 0); for (int i = -1; i <= 1; i++) Hall(i * 1.05f, 1.10f, .65f, .5f, .4f, c); break;
            case 12: Wheel(0, -.45f, 1.58f, 14, c, 2); Hall(-1.6f, 1.05f, .55f, .7f, .60f, c); break;
            case 13: Wheel(-.55f, -.6f, 1.17f, 12, c, 1); Hall(1.42f, -.5f, .7f, 1.7f, .6f, c); Solar(1.18f, .76f, -.5f, 2); break;
            case 14: Wheel(-.9f, -.8f, .85f, 8, c, 0); Wheel(.95f, -.15f, .85f, 12, c, 2); break;
            case 15: Wheel(0, -.55f, 1.38f, 16, c, 1); Hall(0, 1.1f, 2.8f, .55f, .40f, c); Solar(-1.05f, .56f, 1.1f, 7); break;
        }
        // Ticket kiosk and fixed queue lanes at ordinary storefront scale.
        Hall(-1.7f, 1.65f, .42f, .35f, .36f, c);
        Rlgl.PopMatrix();
    }
}
