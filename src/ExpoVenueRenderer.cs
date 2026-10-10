using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class ExpoVenueRenderer
{
    private static void Pavilions(float z, int count, Color c, bool domes = false)
    {
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * 1.7f;
            if (domes) Dome(x, z, 1.3f, 1.2f, .45f, c);
            else Gable(x, z, 1.3f, 1.3f, .55f, c, .27f);
        }
    }
    private static void Tower(float x, float z, Color c)
    {
        Disk(x, .1f, z, .45f, .05f, White); Box(x, .15f, z, .15f, 1.8f, .15f, Steel);
        Disk(x, 1.65f, z, .45f, .15f, Glass); Disk(x, 1.80f, z, .5f, .06f, c);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Dome(0, -2.1f, 4.5f, 2.5f, .85f, c); Pavilions(.8f, 4, c); Pool(-3.0f, -1.65f, .45f); Pool(3.0f, -1.65f, .45f); break;
            case 1: Pavilions(-2.5f, 4, c, true); Hall(-2.3f, .1f, 1.6f, 2.1f, .6f, c); Hall(2.3f, .1f, 1.6f, 2.1f, .6f, c); Pool(0, .1f, .9f); break;
            case 2: Dome(-1.8f, -1.4f, 2.8f, 3.1f, .75f, c); Dome(1.8f, -1.4f, 2.8f, 3.1f, .75f, c); Pavilions(1.8f, 3, c); break;
            case 3: Hall(0, -2.5f, 6.7f, 1.5f, .8f, c); Tower(-2.6f, .15f, c); Dome(.9f, .4f, 3.3f, 2.5f, .6f, c); Pool(-2.6f, 1.75f, .5f); break;
            case 4: Pavilions(-2.4f, 4, Wood); Pavilions(.8f, 4, Wood); Tree(-1.6f, -.8f); Tree(0, -.8f); Tree(1.6f, -.8f); break;
            case 5: Hall(0, -2.5f, 6.5f, 1.5f, .7f, c); Box(0, .855f, -2.5f, 6.2f, .04f, 1.2f, Green); Pavilions(.65f, 4, c, true); break;
            case 6: for (int row = 0; row < 3; row++) Pavilions(-2.7f + row * 2.1f, 4, Wood); break;
            case 7: Dome(0, -1.8f, 3.2f, 3.0f, .75f, c); Hall(-2.8f, -.6f, .9f, 3.4f, .55f, c); Hall(2.8f, -.6f, .9f, 3.4f, .55f, c); Tree(0, 1.7f); break;
            case 8: Hall(-1.6f, -1.6f, 3.4f, 3.1f, 1.2f, c); Atrium(-1.6f, -1.6f, 1.0f, 2.9f, 1.6f); Tower(2.2f, -1.4f, c); Pavilions(1.8f, 4, c); break;
            case 9: Hall(-2.0f, -1.4f, 2.4f, 3.2f, 1.0f, c); Hall(2.0f, -1.4f, 2.4f, 3.2f, 1.0f, c); Box(0, .7f, -1.4f, 1.8f, .3f, .8f, Glass); Pavilions(1.8f, 3, c, true); break;
            case 10: for (int i = -1; i <= 1; i++) Hall(i * 2.3f, -1.7f, 1.9f, 2.9f, .7f + (i + 1) * .3f, c); Tower(0, 1.65f, c); break;
            case 11: Dome(0, -1.4f, 5.5f, 3.4f, .8f, c); Pavilions(1.8f, 4, c); break;
            case 12: for (int i = -1; i <= 1; i++) Gable(i * 2.2f, -1.5f, 1.9f, 3.2f, .95f, c, .4f); Pavilions(1.8f, 4, c); break;
            case 13: Hall(0, -2.35f, 6.5f, 1.8f, .8f, c); Solar(-2.8f, .96f, -2.35f, 18); Pavilions(.8f, 4, c, true); break;
            case 14: Dome(-2.3f, -1.7f, 1.8f, 2.4f, .65f, c); Dome(2.3f, -1.7f, 1.8f, 2.4f, .65f, c); Tower(0, -1.7f, c); Pavilions(1.4f, 4, c); break;
            case 15: Hall(-1.8f, -1.7f, 2.8f, 2.8f, .95f, c); Dome(1.8f, -1.7f, 2.7f, 2.8f, .8f, c); Atrium(0, 1.65f, 6.4f, .9f, .65f); break;
        }
        // Street-sized flags and admission booths give the large site human scale.
        for (int i = -3; i <= 3; i++)
        {
            Box(i * .6f, .1f, 3.25f, .025f, .65f, .025f, Steel);
            Box(i * .6f + .10f, .57f, 3.25f, .2f, .14f, .015f, i % 2 == 0 ? c : White);
        }
        Hall(-3.05f, 3.5f, .7f, .55f, .35f, c); Hall(3.05f, 3.5f, .7f, .55f, .35f, c);
        Arcade(0, 3.75f, 1.8f, c); Rlgl.PopMatrix();
    }
}
