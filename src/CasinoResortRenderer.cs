using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class CasinoResortRenderer
{
    private static void Hotel(float x, float z, float w, float d, float h, Color c)
    {
        Hall(x, z, w, d, h, c);
        for (float px = -w / 2 + .12f; px <= w / 2; px += .45f) Box(x + px, .1f, z + d / 2 + .026f, .025f, h, .025f, White);
        Box(x, h + .16f, z, w * .55f, .15f, d * .7f, c);
    }
    private static void Fountain(float x, float z, float r)
    {
        Pool(x, z, r); Disk(x, .14f, z, .08f, .19f, White);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8;
            Beam(new(x, .4f, z), new(x + MathF.Cos(a) * r * .6f, .18f, z + MathF.Sin(a) * r * .6f), .012f, White);
        }
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Hotel(0, -2.0f, 6.3f, 1.8f, 1.5f, c); Dome(0, .2f, 3.2f, 2.0f, .65f, c); Fountain(-2.4f, 1.5f, .6f); Fountain(2.4f, 1.5f, .6f); break;
            case 1: Hotel(-2.0f, -1.4f, 1.7f, 3.0f, 1.9f, c); Hotel(2.0f, -1.4f, 1.7f, 3.0f, 1.9f, c); Hall(0, 1.05f, 5.7f, 1.2f, .6f, c); Fountain(0, -1.1f, .75f); break;
            case 2: Dome(0, -1.4f, 4.8f, 3.0f, .8f, c); Hotel(-2.45f, 1.05f, 1.2f, 1.4f, 1.4f, c); Hotel(2.45f, 1.05f, 1.2f, 1.4f, 1.4f, c); Fountain(0, 1.75f, .6f); break;
            case 3: Hotel(-1.6f, -1.3f, 2.4f, 3.3f, 2.1f, c); Hall(1.55f, -1.8f, 2.4f, 2.3f, .8f, c); Pool(1.55f, 1.0f, 1.0f); break;
            case 4: Hotel(0, -2.25f, 6.1f, 1.2f, 1.05f, c); Hall(-2.45f, -.3f, 1.1f, 2.4f, .6f, c); Hall(2.45f, -.3f, 1.1f, 2.4f, .6f, c); Fountain(0, .1f, .8f); Tree(-1.0f, 1.7f); Tree(1.0f, 1.7f); break;
            case 5: Hotel(-1.7f, -1.8f, 2.6f, 2.0f, 1.3f, c); Hotel(1.7f, -1.8f, 2.6f, 2.0f, 1.3f, c); Hall(0, .8f, 5.9f, 1.5f, .55f, c); Box(0, .71f, .8f, 5.6f, .04f, 1.25f, Green); break;
            case 6: for (int i = -1; i <= 1; i++) Gable(i * 2.15f, -1.4f, 1.8f, 2.9f, .85f, Wood); Hall(0, 1.25f, 5.8f, .8f, .5f, c); break;
            case 7: Dome(0, -1.25f, 3.8f, 3.1f, .9f, c); for (int i = -1; i <= 1; i++) { Tree(i * 2.3f, 1.65f); if (i != 0) Hotel(i * 2.7f, -1.3f, .8f, 2.8f, 1.25f, c); } break;
            case 8: Hotel(-1.6f, -1.2f, 2.6f, 3.4f, 2.4f, c); Hotel(1.75f, -1.5f, 2.1f, 2.8f, 1.8f, c); Atrium(0, 1.2f, 5.8f, .7f, .75f); break;
            case 9: Hotel(-2.1f, -1.3f, 1.55f, 3.1f, 2.2f, c); Hotel(2.1f, -1.3f, 1.55f, 3.1f, 2.2f, c); Box(0, 1.2f, -1.3f, 2.7f, .4f, .9f, Glass); Fountain(0, 1.25f, .8f); break;
            case 10: for (int i = -1; i <= 1; i++) Hotel(i * 2.05f, -1.3f, 1.7f, 3.1f, 1.2f + (i + 1) * .5f, c); Arcade(0, 1.4f, 5.8f, c); break;
            case 11: Hall(0, -1.2f, 6.0f, 3.2f, .7f, c); Hotel(0, -1.6f, 3.7f, 2.3f, 1.3f, c); Hotel(0, -1.9f, 2.0f, 1.6f, 2.0f, c); Fountain(-2.1f, 1.65f, .5f); Fountain(2.1f, 1.65f, .5f); break;
            case 12: Hotel(-1.6f, -1.3f, 2.5f, 3.2f, 1.5f, c); Dome(1.7f, -1.5f, 2.4f, 2.7f, .65f, c); Hall(0, 1.45f, 5.8f, .75f, .45f, c); break;
            case 13: Hotel(0, -1.8f, 6.0f, 2.1f, 1.2f, c); Solar(-2.6f, 1.52f, -1.8f, 16); Hall(-1.8f, .8f, 2.3f, 1.6f, .6f, c); Fountain(1.7f, .9f, .9f); break;
            case 14: Hotel(-2.2f, -1.4f, 1.3f, 2.9f, 1.8f, c); Hotel(2.2f, -1.4f, 1.3f, 2.9f, 1.8f, c); Dome(0, -.25f, 2.6f, 2.5f, .8f, c); break;
            case 15: Hotel(-1.65f, -1.35f, 2.6f, 3.2f, 2.0f, c); Hotel(1.7f, -1.7f, 2.1f, 2.5f, 1.2f, c); Dome(1.7f, 1.2f, 2.1f, 1.7f, .55f, c); Fountain(-1.6f, 1.55f, .6f); break;
        }
        Parking(3.2f, c); Arcade(0, 2.4f, 2.5f, c);
        // A small diamond sign identifies the casino without an oversized prop.
        Box(.6f, .1f, 2.75f, .025f, .45f, .025f, Steel);
        Rlgl.PushMatrix(); Rlgl.Translatef(.6f, .53f, 2.75f); Rlgl.Rotatef(45, 0, 0, 1); Box(0, -.075f, 0, .15f, .15f, .025f, c); Rlgl.PopMatrix();
        Rlgl.PopMatrix();
    }
}
