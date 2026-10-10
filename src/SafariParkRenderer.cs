using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class SafariParkRenderer
{
    private static void Reserve(float x, float z, float w, float d, int species, bool pond = false)
    {
        Box(x, .105f, z, w, .015f, d, species == 0 ? new(186, 184, 121, 255) : Green);
        foreach (int side in new[] { -1, 1 })
        {
            Box(x, .13f, z + side * d / 2, w, .065f, .035f, Wood);
            Box(x + side * w / 2, .13f, z, .035f, .065f, d, Wood);
            for (float p = -w / 2; p <= w / 2; p += .4f) Box(x + p, .12f, z + side * d / 2, .025f, .16f, .025f, Wood);
        }
        if (pond) Pool(x + w * .25f, z - d * .2f, Math.Min(w, d) * .23f);
        else { Tree(x + w * .28f, z - d * .25f); Tree(x - w * .28f, z - d * .25f); }
        for (int i = 0; i < 3; i++) ZooRenderer.Animal(x - w * .22f + i * .3f, z + .15f + i % 2 * .32f, species);
        Gable(x - w * .3f, z - d * .28f, .45f, .38f, .28f, Wood, .13f);
    }
    private static void Bus(float x, float z, Color c)
    {
        Box(x, .13f, z, .18f, .14f, .43f, c); Box(x, .27f, z, .16f, .075f, .35f, White);
        foreach (int side in new[] { -1, 1 })
        {
            for (int i = -1; i <= 1; i++) Box(x + side * .085f, .235f, z + i * .1f, .015f, .09f, .07f, Glass);
            Box(x + side * .10f, .105f, z - .14f, .03f, .07f, .07f, Steel);
            Box(x + side * .10f, .105f, z + .14f, .03f, .07f, .07f, Steel);
        }
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        // Loop roads surround differently arranged reserves; buses keep road-vehicle scale.
        Box(0, .103f, -.3f, 7.3f, .006f, 6.2f, new(172, 167, 136, 255));
        Box(0, .110f, -.3f, 6.5f, .004f, 5.4f, PathColor);
        switch (tile.LandmarkStyle)
        {
            case 0: Reserve(-1.6f, -.3f, 2.7f, 4.8f, 0); Reserve(1.6f, -1.6f, 2.7f, 2.1f, 1, true); Reserve(1.6f, 1.0f, 2.7f, 2.1f, 2); break;
            case 1: Reserve(0, -1.6f, 6.0f, 2.1f, 1, true); Reserve(-1.6f, 1, 2.7f, 2.1f, 0); Reserve(1.6f, 1, 2.7f, 2.1f, 2); break;
            case 2: Reserve(-1.65f, -.3f, 2.6f, 4.8f, 1, true); Reserve(1.6f, -.3f, 2.6f, 4.8f, 0, true); break;
            case 3: Reserve(0, -.8f, 5.8f, 3.7f, 0, true); Pool(-1.7f, 1.75f, .4f); Pool(1.7f, 1.75f, .4f); break;
            case 4: Reserve(0, -.3f, 6.0f, 4.8f, 0); for (int i = -1; i <= 1; i++) Tree(i * 1.5f, 1.8f); break;
            case 5: for (int i = -1; i <= 1; i++) Reserve(i * 2.1f, -.3f, 1.7f, 4.8f, i + 1); break;
            case 6: for (int i = -1; i <= 1; i++) { Reserve(i * 2.1f, -1.7f, 1.7f, 1.9f, i + 1); Reserve(i * 2.1f, 1.0f, 1.7f, 2.0f, 2 - i); } break;
            case 7: Reserve(-1.4f, -.3f, 3.2f, 4.8f, 1); Dome(1.9f, -1.5f, 1.8f, 2.0f, .55f, c); Reserve(1.9f, 1.1f, 1.8f, 1.8f, 2); break;
            case 8: Hall(0, -2.15f, 5.8f, .9f, .7f, c); Reserve(-1.6f, .6f, 2.7f, 2.7f, 0); Reserve(1.6f, .6f, 2.7f, 2.7f, 1); break;
            case 9: Reserve(-1.7f, -.3f, 2.3f, 4.8f, 0); Reserve(1.7f, -.3f, 2.3f, 4.8f, 2); Hall(0, -.7f, .65f, 2.0f, .75f, c); break;
            case 10: Reserve(0, -1.7f, 5.8f, 1.9f, 2); Reserve(0, .9f, 5.8f, 2.0f, 0); break;
            case 11: Dome(-1.6f, -1.5f, 2.4f, 2.0f, .6f, c); Reserve(1.6f, -.3f, 2.6f, 4.8f, 1, true); Reserve(-1.6f, 1.1f, 2.4f, 1.8f, 0); break;
            case 12: Reserve(0, -1.4f, 5.8f, 2.5f, 0); Reserve(-1.6f, 1.3f, 2.7f, 1.5f, 2); Hall(1.6f, 1.2f, 2.2f, 1.3f, .55f, c); break;
            case 13: Reserve(-1.6f, -.3f, 2.7f, 4.8f, 1, true); Hall(1.6f, -1.5f, 2.2f, 2.0f, .6f, c); Solar(.7f, .76f, -1.5f, 6); Reserve(1.6f, 1.1f, 2.5f, 1.8f, 0); break;
            case 14: Reserve(-2.05f, -.3f, 1.7f, 4.8f, 2); Reserve(2.05f, -.3f, 1.7f, 4.8f, 0); Reserve(0, -1.4f, 1.6f, 2.5f, 1, true); Tree(0, 1.4f); break;
            case 15: Reserve(0, -1.6f, 5.8f, 2.1f, 0, true); Reserve(-1.65f, 1.0f, 2.5f, 2.0f, 1); Dome(1.65f, 1.0f, 2.2f, 1.8f, .55f, c); break;
        }
        Bus(-3.45f, -.7f, c); Bus(3.45f, 1.0f, Wood);
        Hall(-2.3f, 3.2f, 1.15f, .65f, .4f, c); Hall(2.3f, 3.2f, 1.15f, .65f, .4f, c); Arcade(0, 3.2f, 1.8f, Wood);
        Rlgl.PopMatrix();
    }
    private static readonly Color PathColor = new(216, 211, 170, 255);
}
