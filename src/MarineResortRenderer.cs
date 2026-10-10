using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class MarineResortRenderer
{
    private static void Lagoon(float x, float z, float w, float d)
    {
        Box(x, .103f, z, w + .24f, .022f, d + .24f, new(222, 210, 164, 255));
        Box(x, .126f, z, w, .006f, d, Glass);
        Box(x, .133f, z + d / 2 - .1f, w * .8f, .025f, .2f, Wood);
        for (int i = -1; i <= 1; i++)
        {
            float px = x + i * w * .25f;
            Box(px, .17f, z + d / 2 - .1f, .025f, .24f, .025f, Wood);
            DrawCylinder(new(px, .41f, z + d / 2 - .1f), 0, .15f, .08f, 8, White);
        }
    }
    private static void Boat(float x, float z, Color c)
    {
        Box(x, .14f, z, .24f, .07f, .65f, White);
        Box(x, .21f, z + .12f, .18f, .1f, .24f, c);
        Box(x, .31f, z + .12f, .14f, .04f, .20f, Glass);
    }
    private static void Cottage(float x, float z, Color c) => Gable(x, z, .85f, .85f, .43f, c, .19f);
    private static void Villas(float x, float z, int count, Color c)
    {
        for (int i = 0; i < count; i++) Cottage(x + i * 1.25f, z, c);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Hall(0, -2.8f, 6.8f, 1.5f, 1.15f, c); Lagoon(0, .3f, 5.3f, 3.1f); Boat(-1.7f, -.2f, c); Boat(1.7f, -.2f, Wood); break;
            case 1: Hall(-2.8f, -1.3f, 1.5f, 4.2f, .9f, c); Hall(2.8f, -1.3f, 1.5f, 4.2f, .9f, c); Lagoon(0, -.5f, 3.1f, 4.0f); Boat(.65f, -.8f, c); break;
            case 2: Dome(0, -2.3f, 4.4f, 2.3f, .8f, c); Lagoon(-2.1f, .8f, 2.4f, 2.5f); Lagoon(2.1f, .8f, 2.4f, 2.5f); Boat(-2.1f, .3f, c); break;
            case 3: Hall(-2.1f, -1.6f, 2.5f, 3.6f, 1.4f, c); Lagoon(1.75f, -.5f, 2.8f, 4.2f); Villas(-2.9f, 1.8f, 3, c); break;
            case 4: Villas(-3.1f, -2.5f, 6, Wood); Lagoon(0, .15f, 5.5f, 3.0f); Tree(-3.5f, 1.7f); Tree(3.5f, 1.7f); break;
            case 5: Villas(-3.1f, -2.6f, 3, Wood); Villas(.65f, -2.6f, 3, Wood); Lagoon(-1.8f, .1f, 2.5f, 3.1f); Lagoon(1.8f, .1f, 2.5f, 3.1f); Tree(0, -.8f); break;
            case 6: Villas(-3.1f, -2.6f, 6, Wood); Villas(-3.1f, .7f, 3, Wood); Lagoon(1.7f, .15f, 2.8f, 2.9f); Boat(1.7f, -.3f, c); break;
            case 7: Dome(-1.75f, -1.8f, 3.0f, 2.7f, .7f, c); Lagoon(1.9f, -.4f, 2.4f, 4.0f); Villas(-2.9f, 1.8f, 3, Wood); break;
            case 8: Hall(0, -2.6f, 6.7f, 1.8f, 1.65f, c); Lagoon(0, .55f, 5.4f, 2.6f); Boat(-1.6f, .3f, c); Boat(1.6f, .3f, Wood); break;
            case 9: Hall(-2.4f, -1.75f, 1.9f, 3.0f, 1.7f, c); Hall(2.4f, -1.75f, 1.9f, 3.0f, 1.7f, c); Atrium(0, -2.4f, 2.6f, .8f, .8f); Lagoon(0, 1.0f, 5.6f, 1.8f); break;
            case 10: for (int i = -1; i <= 1; i++) Hall(i * 2.3f, -2.35f, 1.9f, 2.0f, .8f + (i + 1) * .35f, c); Lagoon(0, .7f, 5.6f, 2.3f); break;
            case 11: Hall(0, -2.8f, 6.5f, 1.2f, 1.0f, c); Hall(-2.9f, -.8f, 1.0f, 2.5f, .75f, c); Hall(2.9f, -.8f, 1.0f, 2.5f, .75f, c); Lagoon(0, .8f, 4.0f, 2.0f); break;
            case 12: Hall(-2.05f, -1.4f, 2.5f, 3.7f, .9f, c); Solar(-3.05f, 1.06f, -1.4f, 7); Lagoon(1.8f, -.3f, 2.6f, 4.1f); Boat(1.8f, -.5f, c); break;
            case 13: Villas(-3.1f, -2.6f, 6, c); Lagoon(0, -.15f, 3.8f, 2.1f); Villas(-3.1f, 1.85f, 2, c); Villas(1.8f, 1.85f, 2, c); Boat(0, -.6f, Wood); break;
            case 14: Dome(-1.8f, -2.0f, 2.9f, 2.2f, .75f, c); Dome(1.8f, -2.0f, 2.9f, 2.2f, .75f, c); Lagoon(0, .8f, 5.4f, 2.3f); break;
            case 15: Hall(-2.0f, -2.15f, 2.6f, 2.1f, 1.2f, c); Dome(1.8f, -2.15f, 2.6f, 2.1f, .75f, c); Lagoon(-1.8f, .7f, 2.5f, 2.3f); Lagoon(1.8f, .7f, 2.5f, 2.3f); break;
        }
        Hall(-3.1f, 3.65f, .7f, .55f, .35f, c); Arcade(0, 3.2f, 2.2f, Wood);
        Rlgl.PopMatrix();
    }
}
