using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;

namespace GrowUpTown;

internal static class AquariumRenderer
{
    private static void Dome(float x, float z, float r, Color c)
    {
        Disk(x, .10f, z, r, .55f, White);
        DrawSphereEx(new(x, .63f, z), r * .92f, 10, 20, c);
        // The lower half of the dome is enclosed by the cylindrical hall.
        Disk(x, .10f, z, r + .015f, .56f, White);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16;
            Beam(new(x + MathF.Cos(a) * r, .30f, z + MathF.Sin(a) * r),
                new(x + MathF.Cos(a) * r, .48f, z + MathF.Sin(a) * r), .035f, Glass);
        }
    }
    private static void Tunnel(float x, float z, float length)
    {
        Box(x, .10f, z, .50f, .30f, length, Glass);
        for (float p = -length / 2; p <= length / 2; p += .24f)
            Box(x, .10f, z + p, .53f, .34f, .02f, White);
    }
    private static void WaveRoof(float x, float z, float w, float d, float h, Color c)
    {
        Hall(x, z, w, d, h, c);
        for (int i = 0; i < 5; i++)
        {
            float px = x - w * .4f + i * w * .2f;
            Box(px, h + .155f, z, w * .19f, .08f + .10f * (1 + MathF.Sin(i * 1.3f)), d, i % 2 == 0 ? c : Glass);
        }
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: // Ocean dome and entrance gallery
                Dome(0, -.55f, 1.20f, Glass); Hall(0, 1.0f, 2.2f, .65f, .55f, c); break;
            case 1: // Two marine domes joined by a viewing tunnel
                Dome(-1, -.65f, .82f, Glass); Dome(1, -.65f, .82f, c);
                Hall(0, .70f, 3.3f, .70f, .60f, c); Tunnel(0, -.55f, 1.7f); break;
            case 2: // Wave-front ocean hall
                WaveRoof(0, -.60f, 3.45f, 2.05f, .85f, c); Pool(-1.3f, 1.15f, .43f); Pool(1.3f, 1.15f, .43f); break;
            case 3: // Lighthouse wing and sheltered lagoon
                Hall(-.75f, -.55f, 1.85f, 2.5f, .75f, c); Pool(1.10f, -.5f, .72f);
                Disk(-1.28f, .9f, -1.30f, .22f, .75f, White); Disk(-1.28f, 1.65f, -1.30f, .27f, .18f, Glass); break;
            case 4: // Garden courtyard aquarium
                Hall(-1.18f, -.45f, .95f, 2.5f, .65f, c); Hall(1.18f, -.45f, .95f, 2.5f, .65f, c);
                Hall(0, -1.35f, 1.45f, .70f, .65f, c); Pool(0, -.25f, .55f); break;
            case 5: // Terraced planted reef hall
                Hall(0, -.4f, 3.4f, 2.2f, .60f, c); Hall(-.5f, -.7f, 2.15f, 1.4f, 1.02f, c);
                Box(.95f, .755f, .2f, .65f, .035f, .8f, Green); break;
            case 6: // Three greenhouse reef pavilions
                for (int i = -1; i <= 1; i++) Dome(i * 1.15f, -.8f, .51f, Glass);
                Hall(0, .4f, 3.35f, .9f, .55f, c); break;
            case 7: // Wetland walk with a narrow glass gallery
                Hall(0, -.75f, 3.4f, 1.25f, .70f, c); Pool(-1.0f, .7f, .65f); Pool(1, .7f, .65f); Tunnel(0, .65f, 1.35f); break;
            case 8: // Urban aquarium with a central skylight
                Hall(0, -.4f, 3.6f, 2.5f, .85f, c); Box(0, 1.005f, -.4f, 1.4f, .32f, 1.1f, Glass); break;
            case 9: // Twin stepped exhibition wings
                Hall(-.92f, -.6f, 1.6f, 2.3f, 1.10f, c); Hall(.92f, -.3f, 1.6f, 1.75f, .65f, c);
                Hall(0, 1.05f, 1.0f, .6f, .5f, c); break;
            case 10: // Fan of sawtooth galleries
                for (int i = -1; i <= 1; i++) WaveRoof(i * 1.10f, -.35f + Math.Abs(i) * .20f, .95f, 2.5f, .7f + (i + 1) * .12f, c); break;
            case 11: // Circular city rotunda and ticket court
                Dome(0, -.7f, 1.0f, c); Hall(-1.25f, .7f, .7f, 1.2f, .65f, c); Hall(1.25f, .7f, .7f, 1.2f, .65f, c); break;
            case 12: // Marine research annex
                Hall(-.7f, -.5f, 1.8f, 2.4f, .85f, c); Hall(1.1f, -.8f, .95f, 1.8f, 1.2f, c);
                for (int i = 0; i < 3; i++) Disk(.6f + i * .48f, .1f, .8f, .20f, .36f, Glass); break;
            case 13: // Solar sea-life campus
                Hall(0, -.4f, 3.3f, 2.35f, .75f, c); Solar(-1.25f, .91f, -.65f, 8); Solar(-1.25f, .91f, .1f, 8); break;
            case 14: // Deep-sea drum with a low gallery
                Hall(0, .45f, 3.3f, 1.0f, .6f, c); Disk(0, .1f, -.8f, .9f, 1.25f, c);
                Disk(0, 1.35f, -.8f, .92f, .08f, Glass); break;
            case 15: // Four reef tanks under connected pavilions
                foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Dome(a * .9f, -.3f + b * .8f, .62f, Glass);
                Hall(0, -.3f, .60f, 2.9f, .5f, c); break;
        }
        // Small fish emblem on a freestanding entrance board.
        Box(-.42f, .1f, 1.62f, .025f, .32f, .025f, Steel);
        Box(-.42f, .33f, 1.62f, .34f, .15f, .025f, c);
        DrawSphereEx(new(-.44f, .40f, 1.65f), .052f, 4, 6, White);
        Box(-.35f, .37f, 1.65f, .04f, .065f, .012f, White);
        Rlgl.PopMatrix();
    }
}
