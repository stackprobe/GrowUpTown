using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class ForestRenderer
{
    // Four species families, each with four distinct arrangements. Individual
    // trees also vary in height, foliage and position without changing over time.
    private static readonly Vector2[][] Layouts =
    [
        [new(-.27f, -.27f), new(.26f, -.22f), new(-.22f, .27f), new(.28f, .28f), new(0, 0)],
        [new(-.28f, -.25f), new(.22f, -.28f), new(-.27f, .24f), new(.24f, .21f)],
        [new(-.30f, -.28f), new(.05f, -.30f), new(.29f, -.05f), new(.22f, .28f), new(-.17f, .29f), new(-.29f, .03f)],
        [new(-.25f, -.22f), new(.27f, -.25f), new(-.27f, .26f), new(.24f, .29f), new(.05f, .05f)]
    ];
    internal static void Draw(City city, int wx, int wz, float x, float z, Tile tile)
    {
        int family = tile.ForestStyle / 4, variant = tile.ForestStyle % 4;
        uint seed = ForestAppearance.Hash(city.AppearanceSeed, wx, wz);
        DrawCube(new(x, .035f, z), 1, .045f, 1, new(83 + (int)(seed % 12), 124 + (int)(seed % 18), 76, 255));
        // Place the trees at the top of the ground cube (.035 + .045 / 2).
        Rlgl.PushMatrix(); Rlgl.Translatef(x, .0575f, z); Rlgl.Rotatef(tile.ForestRotation * 90, 0, 1, 0);
        for (int i = 0; i < Layouts[variant].Length; i++)
        {
            uint h = ForestAppearance.Hash(city.AppearanceSeed, wx, wz, i + 91);
            var p = Layouts[variant][i] + new Vector2((int)(h % 9) * .009f - .036f, (int)((h >> 8) % 9) * .009f - .036f);
            float scale = .82f + (h >> 16) % 28 / 100f;
            int species = family == 3 ? i % 3 : family;
            Tree(p, scale * (variant == 1 && i == 0 ? 1.18f : 1), species, (int)(h % 18));
        }
        // Low undergrowth and scattered stones, rather than paths or park borders.
        for (int i = 0; i < 3; i++)
        {
            uint h = ForestAppearance.Hash(city.AppearanceSeed, wx, wz, i + 51);
            float px = (int)(h % 83) / 100f - .41f, pz = (int)((h >> 8) % 83) / 100f - .41f;
            DrawSphereEx(new(px, .045f, pz), .065f, 3, 5, variant == 3 ? new(157, 163, 135, 255) : new(104, 148, 74, 255));
        }
        Rlgl.PopMatrix();
    }
    private static void Tree(Vector2 p, float scale, int species, int shade)
    {
        Color trunk = species == 2 ? new(218, 218, 190, 255) : new(115, 83, 52, 255);
        // DrawCylinder takes the bottom position, unlike DrawCube's center.
        DrawCylinder(new(p.X, 0, p.Y), .024f, .034f, .58f * scale, 5, trunk);
        if (species == 1)
        {
            for (int layer = 0; layer < 3; layer++)
                DrawCylinder(new(p.X, (.40f + layer * .20f) * scale, p.Y), 0, (.22f - layer * .035f) * scale,
                    .40f * scale, 7, new(42 + shade, 101 + layer * 12 + shade, 68 + layer * 7, 255));
        }
        else if (species == 2)
        {
            for (int i = 0; i < 3; i++) DrawCube(new(p.X, (.13f + i * .13f) * scale, p.Y), .052f, .018f, .052f, new(85, 87, 68, 255));
            DrawSphereEx(new(p.X, .56f * scale, p.Y), .18f * scale, 4, 6, new(111 + shade, 161 + shade, 72, 255));
            DrawSphereEx(new(p.X + .02f, .74f * scale, p.Y), .14f * scale, 4, 6, new(159 + shade, 189 + shade / 2, 104, 255));
        }
        else
        {
            DrawSphereEx(new(p.X, .47f * scale, p.Y), .22f * scale, 4, 7, new(55 + shade, 121 + shade, 62, 255));
            DrawSphereEx(new(p.X - .045f, .67f * scale, p.Y + .02f), .18f * scale, 4, 7, new(98 + shade, 157 + shade, 76, 255));
        }
    }
}
