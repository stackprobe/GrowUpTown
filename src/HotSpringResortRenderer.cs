using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class HotSpringResortRenderer
{
    private static void Bath(float x, float z, float w, float d, bool covered = false)
    {
        Box(x, .105f, z, w + .18f, .07f, d + .18f, Steel);
        Box(x, .176f, z, w, .015f, d, new(121, 198, 190, 255));
        foreach (int side in new[] { -1, 1 })
        {
            for (float p = -w / 2; p <= w / 2; p += .3f)
                DrawSphereEx(new(x + p, .18f, z + side * (d / 2 + .04f)), .09f, 4, 6, White);
            Box(x + side * (w / 2 + .13f), .1f, z, .06f, .23f, d + .3f, Wood);
        }
        // Static pale steam curls preserve a completely fixed appearance.
        for (int i = -1; i <= 1; i++)
        {
            float px = x + i * w * .22f;
            Beam(new(px, .23f, z), new(px + .035f, .34f, z), .012f, White);
            Beam(new(px + .035f, .34f, z), new(px, .44f, z), .009f, White);
        }
        if (covered)
        {
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Box(x + sx * (w / 2 + .12f), .1f, z + sz * (d / 2 + .12f), .035f, .65f, .035f, Wood);
            Box(x, .75f, z, w + .4f, .04f, d + .4f, Wood);
        }
    }
    private static void Lodge(float x, float z, float w, float d, float h, Color c)
    {
        Gable(x, z, w, d, h, c, .22f);
        Box(x, .12f, z + d / 2 + .16f, w + .08f, .04f, .32f, Wood);
        for (float px = -w / 2 + .2f; px < w / 2; px += .45f)
            Box(x + px, .2f, z + d / 2 + .08f, .025f, .29f, .025f, Wood);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Lodge(0, -2.1f, 5.8f, 1.3f, .65f, c); Bath(-1.65f, .1f, 2.0f, 1.6f); Bath(1.55f, .6f, 1.8f, 2.3f); break;
            case 1: Lodge(-2.1f, -.8f, 1.4f, 3.7f, .65f, c); Lodge(2.1f, -.8f, 1.4f, 3.7f, .65f, c); Bath(0, -1.4f, 1.7f, 1.5f); Bath(0, 1.0f, 1.7f, 1.2f, true); break;
            case 2: Dome(0, -1.85f, 4.5f, 2.0f, .7f, c); Bath(-1.6f, .85f, 2.1f, 1.8f); Bath(1.6f, .85f, 2.1f, 1.8f); break;
            case 3: Lodge(-1.55f, -1.7f, 2.5f, 2.0f, .95f, c); Bath(1.5f, -1.2f, 1.8f, 2.0f); Bath(-1.5f, 1.15f, 2.0f, 1.0f); Pool(1.5f, 1.35f, .65f); break;
            case 4: Lodge(0, -2.2f, 5.8f, 1.1f, .6f, Wood); Bath(-1.85f, .2f, 1.55f, 2.2f); Bath(1.85f, .2f, 1.55f, 2.2f); Tree(0, -.5f); Tree(0, 1.3f); break;
            case 5: Lodge(-1.7f, -1.85f, 2.1f, 1.5f, .6f, Wood); Lodge(1.7f, -1.85f, 2.1f, 1.5f, .6f, Wood); Bath(0, .7f, 3.8f, 1.5f); Tree(-2.65f, 1.2f); Tree(2.65f, 1.2f); break;
            case 6: for (int i = -1; i <= 1; i++) { Lodge(i * 1.95f, -1.8f, 1.55f, 1.5f, .5f, Wood); Bath(i * 1.95f, .8f, 1.2f, 1.4f); } break;
            case 7: Lodge(-1.85f, -.9f, 1.6f, 3.4f, .6f, Wood); Bath(.3f, -1.5f, 1.6f, 1.6f, true); Bath(1.85f, 1.0f, 1.7f, 1.5f); Tree(.0f, 1.3f); break;
            case 8: Hall(0, -1.85f, 5.7f, 1.9f, 1.35f, c); Bath(-1.6f, .8f, 2.1f, 1.65f); Bath(1.6f, .8f, 2.1f, 1.65f, true); break;
            case 9: Hall(-1.9f, -1.2f, 1.6f, 3.0f, 1.3f, c); Hall(1.9f, -1.2f, 1.6f, 3.0f, .9f, c); Atrium(0, -1.8f, 2.0f, .9f, .65f); Bath(0, 1.15f, 3.1f, 1.35f); break;
            case 10: Dome(-1.45f, -1.6f, 2.5f, 2.0f, .65f, c); Dome(1.45f, -1.6f, 2.5f, 2.0f, .65f, c); Bath(-1.5f, 1.0f, 1.9f, 1.4f); Bath(1.5f, 1.0f, 1.9f, 1.4f); break;
            case 11: Hall(0, -2.1f, 5.7f, 1.2f, .85f, c); Hall(-2.35f, -.5f, 1.0f, 1.7f, .65f, c); Hall(2.35f, -.5f, 1.0f, 1.7f, .65f, c); Bath(0, .8f, 3.6f, 1.8f); break;
            case 12: Lodge(-1.65f, -1.3f, 2.2f, 2.8f, .75f, c); Solar(-2.45f, 1.13f, -1.3f, 6); Bath(1.55f, -1.0f, 1.9f, 2.7f); Bath(0, 1.5f, 2.5f, .85f); break;
            case 13: Lodge(0, -2.0f, 5.7f, 1.4f, .6f, c); Bath(-1.8f, .65f, 1.5f, 1.7f, true); Bath(.4f, .1f, 1.3f, 1.3f); Bath(2.1f, 1.35f, 1.1f, 1.1f); break;
            case 14: Lodge(-2.0f, -1.4f, 1.5f, 2.5f, .65f, c); Lodge(2.0f, -1.4f, 1.5f, 2.5f, .65f, c); Bath(0, -1.3f, 1.5f, 1.5f); Bath(-1.4f, 1.3f, 1.7f, 1.0f); Bath(1.4f, 1.3f, 1.7f, 1.0f); break;
            case 15: Hall(-1.5f, -1.6f, 2.5f, 2.1f, 1.0f, c); Lodge(1.6f, -1.6f, 2.1f, 2.1f, .6f, c); Bath(-1.6f, 1.1f, 1.9f, 1.35f, true); Bath(1.6f, 1.1f, 1.9f, 1.35f); break;
        }
        Arcade(0, 2.7f, 2.0f, Wood); Hall(-2.6f, 2.8f, .6f, .55f, .35f, c);
        Rlgl.PopMatrix();
    }
}
