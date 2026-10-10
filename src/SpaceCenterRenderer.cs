using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class SpaceCenterRenderer
{
    private static void Rocket(float x, float z, float height = 1.15f)
    {
        Disk(x, .1f, z, .35f, .035f, Steel);
        Disk(x, .135f, z, .12f, height, White);
        Disk(x, .135f + height * .55f, z, .122f, .12f, Steel);
        DrawCylinder(new(x, .135f + height, z), 0, .12f, .3f, 12, White);
        foreach (int side in new[] { -1, 1 })
        {
            Box(x + side * .15f, .135f, z, .07f, .25f, .07f, Steel);
            Box(x, .135f, z + side * .15f, .07f, .25f, .07f, Steel);
        }
    }
    private static void Dish(float x, float z, float y = .1f)
    {
        Disk(x, y, z, .18f, .06f, Steel);
        Beam(new(x, y + .06f, z), new(x, y + .45f, z), .04f, White);
        Rlgl.PushMatrix(); Rlgl.Translatef(x, y + .47f, z); Rlgl.Rotatef(25, 1, 0, 0);
        DrawCylinder(Vector3.Zero, .32f, .06f, .13f, 16, White);
        Beam(new(0, .13f, 0), new(0, .37f, 0), .013f, Steel); Rlgl.PopMatrix();
    }
    private static void Assembly(float x, float z, float w, float d, float h, Color c)
    {
        Hall(x, z, w, d, h, c);
        // Equipment door, distinct from the ordinary pedestrian entrance.
        Box(x + w * .25f, .1f, z + d / 2 + .02f, .32f, .40f, .025f, Steel);
        for (int i = 0; i < 6; i++) Box(x + w * .25f, .14f + i * .055f, z + d / 2 + .038f, .29f, .012f, .015f, White);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: // Coastal satellite campus
                Assembly(-.8f, -.65f, 2.25f, 2.7f, 1.0f, c); Dish(1.35f, -.9f); Rocket(1.35f, .65f); break;
            case 1: // Marine recovery laboratory
                Hall(0, -1.1f, 4.0f, 1.7f, .85f, c); Pool(-1.05f, .55f, .75f); Assembly(1.2f, .4f, 1.2f, 1.1f, .65f, c); break;
            case 2: // Twin telemetry halls
                Hall(-1.1f, -.7f, 1.75f, 2.5f, .85f, c); Hall(1.1f, -.7f, 1.75f, 2.5f, .85f, c);
                Dish(-1.1f, -.7f, 1.005f); Dish(1.1f, -.7f, 1.005f); break;
            case 3: // Waterfront mission control and test yard
                Hall(-1.0f, -.75f, 1.85f, 2.6f, 1.4f, c); Pool(1.25f, -1.25f, .6f); Rocket(1.15f, .4f, 1.5f); break;
            case 4: // Garden research court
                Hall(0, -1.4f, 3.9f, 1.0f, .85f, c); Hall(-1.55f, -.05f, .8f, 1.65f, .65f, c);
                Hall(1.55f, -.05f, .8f, 1.65f, .65f, c); Tree(0, -.3f); Dish(0, 1.15f); break;
            case 5: // Green roof space sciences institute
                Hall(0, -.65f, 4.1f, 2.7f, .85f, c); Box(0, 1.005f, -.65f, 3.8f, .04f, 2.35f, Green);
                Dish(-1, -.65f, 1.05f); Dish(1, -.65f, 1.05f); break;
            case 6: // Small satellite pavilions
                foreach (int side in new[] { -1, 1 }) { Gable(side * 1.05f, -1.15f, 1.7f, 1.3f, .65f, c); Hall(side * 1.05f, .45f, 1.25f, .9f, .5f, c); }
                Rocket(0, -.35f, .8f); break;
            case 7: // Solar research campus
                Assembly(-.8f, -.65f, 2.3f, 2.7f, .9f, c); Solar(-1.6f, 1.06f, -.65f, 5);
                for (int i = 0; i < 3; i++) Dish(1.35f, -1.3f + i * .85f); break;
            case 8: // Urban mission-control tower
                Hall(-.85f, -.75f, 2.0f, 2.6f, 1.8f, c); Assembly(1.15f, -.55f, 1.5f, 2.2f, .75f, c); Dish(-.85f, -.75f, 1.955f); break;
            case 9: // Glass communications atrium
                Hall(0, -.65f, 4.0f, 2.7f, 1.0f, c); Atrium(0, -.65f, 1.2f, 2.2f, 1.4f); Dish(-1.35f, 1.1f); Dish(1.35f, 1.1f); break;
            case 10: // Three-tier data and control campus
                for (int i = -1; i <= 1; i++) Hall(i * 1.3f, -.75f, 1.1f, 2.5f, .7f + (i + 1) * .35f, c);
                Rocket(0, 1.25f, .9f); break;
            case 11: // Circular planet-science institute
                Dome(0, -.85f, 3.2f, 2.8f, .85f, c); Dish(-1.85f, 1.1f); Dish(1.85f, 1.1f); break;
            case 12: // Rocket assembly and static test display
                Assembly(-.8f, -.7f, 2.3f, 2.75f, 1.5f, c); Rocket(1.35f, -.7f, 1.8f);
                Box(1.8f, .1f, -.7f, .08f, 2.0f, .08f, Steel);
                for (int i = 1; i < 6; i++) Beam(new(1.35f, .1f + i * .3f, -.7f), new(1.8f, .1f + i * .3f, -.7f), .017f, Steel); break;
            case 13: // Twin integration sheds
                Gable(-1.05f, -.7f, 1.85f, 2.65f, 1.0f, c); Gable(1.05f, -.7f, 1.85f, 2.65f, 1.0f, c); Dish(0, 1.1f); break;
            case 14: // Antenna and clean-room campus
                Assembly(0, -1.3f, 4.0f, 1.3f, .9f, c);
                for (int i = -1; i <= 1; i++) Dish(i * 1.3f, .35f); break;
            case 15: // Consolidated aerospace development center
                Assembly(-.7f, -.65f, 2.6f, 2.75f, 1.1f, c); Hall(1.35f, -.9f, 1.1f, 2.15f, .7f, c);
                Solar(-1.7f, 1.26f, -.75f, 6); Rocket(1.35f, .8f); break;
        }
        // Static research equipment only: no launches or changes to the chosen design.
        Box(-.55f, .1f, 1.9f, .025f, .3f, .025f, Steel);
        Box(-.55f, .33f, 1.9f, .28f, .15f, .025f, c);
        Rlgl.PopMatrix();
    }
}
