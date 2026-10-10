using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;

namespace GrowUpTown;

internal static class RoboticsLabRenderer
{
    private static void Arm(float x, float z, float y = .10f)
    {
        Color gold = new(225, 170, 78, 255);
        Disk(x, y, z, .11f, .045f, Steel);
        Beam(new(x, y + .045f, z), new(x, y + .23f, z), .035f, gold);
        Beam(new(x, y + .23f, z), new(x + .14f, y + .34f, z), .028f, gold);
        Beam(new(x + .14f, y + .34f, z), new(x + .24f, y + .20f, z), .022f, White);
        foreach (int side in new[] { -1, 1 }) Box(x + .24f, y + .14f, z + side * .035f, .018f, .07f, .018f, Steel);
    }
    private static void Lab(float x, float z, float w, float d, float h, Color c)
    {
        Hall(x, z, w, d, h, c);
        for (float p = -w / 2 + .25f; p < w / 2 - .15f; p += .55f)
            Box(x + p, h + .16f, z - d * .2f, .18f, .10f, .22f, Steel);
    }
    private static void Sensor(float x, float z, float y)
    {
        Disk(x, y, z, .24f, .12f, White);
        DrawSphereEx(new(x, y + .12f, z), .22f, 8, 12, White);
        Beam(new(x, y + .32f, z), new(x, y + .60f, z), .015f, Steel);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: // Marine robotics with a test pool
                Lab(-.85f, -.45f, 1.5f, 2.5f, .9f, c); Pool(.95f, -.55f, .82f); Arm(.7f, .65f); break;
            case 1: // Tank hall and instrument tower
                Lab(0, -.65f, 3.25f, 1.55f, .85f, c); Pool(-1.1f, .8f, .48f); Lab(1.08f, .65f, .8f, .85f, 1.25f, c); break;
            case 2: // Twin waterside research wings
                Lab(-1.0f, -.55f, 1.1f, 2.4f, 1.0f, c); Lab(1.0f, -.55f, 1.1f, 2.4f, .7f, c);
                Pool(0, -.65f, .40f); Hall(0, 1.03f, 2.5f, .4f, .45f, c); break;
            case 3: // Submersible testing campus
                Lab(0, -1.1f, 3.25f, 1.05f, .8f, c); Pool(-.9f, .35f, .63f); Pool(.9f, .35f, .63f); Sensor(0, -1.1f, .955f); break;
            case 4: // Garden courtyard institute
                Lab(-1.2f, -.4f, .85f, 2.4f, .75f, c); Lab(1.2f, -.4f, .85f, 2.4f, .75f, c); Lab(0, -1.25f, 1.5f, .7f, .75f, c);
                Tree(0, -.4f); Arm(-.45f, .55f); Arm(.45f, .55f); break;
            case 5: // Planted stepped laboratory
                Lab(0, -.4f, 3.35f, 2.35f, .65f, c); Lab(-.65f, -.65f, 1.7f, 1.6f, 1.1f, c);
                Box(.95f, .805f, -.4f, .9f, .045f, 1.6f, Green); break;
            case 6: // Distributed garden pavilions
                foreach (int side in new[] { -1, 1 }) { Lab(side * 1.05f, -.95f, 1.35f, 1.2f, .7f, c); Lab(side * 1.05f, .55f, 1.0f, .8f, .5f, c); }
                Tree(0, -.8f); break;
            case 7: // Low energy research hall
                Lab(0, -.4f, 3.35f, 2.2f, .7f, c); Solar(-1.3f, .86f, -.4f, 8); Arm(-.7f, 1.2f); Arm(.7f, 1.2f); break;
            case 8: // Urban twin towers and glass link
                Lab(-.98f, -.5f, 1.45f, 2.0f, 1.6f, c); Lab(.98f, -.5f, 1.45f, 2.0f, 1.25f, c);
                Box(0, .55f, -.5f, .6f, .32f, .65f, Glass); break;
            case 9: // Atrium campus
                Lab(0, -.5f, 3.3f, 2.2f, .85f, c); Box(0, 1.005f, -.5f, 1.1f, .40f, 1.4f, Glass);
                for (int i = -2; i <= 2; i++) Box(i * .22f, 1.005f, -.5f, .02f, .42f, 1.42f, White); break;
            case 10: // Three city research blocks
                for (int i = -1; i <= 1; i++) Lab(i * 1.1f, -.5f, .95f, 2.25f, .75f + (i + 1) * .30f, c); break;
            case 11: // Circular sensor institute
                Disk(0, .1f, -.5f, 1.3f, .75f, White); Disk(0, .85f, -.5f, 1.33f, .08f, c);
                Hall(0, .92f, 1.65f, .65f, .55f, c); Sensor(0, -.5f, .93f);
                for (int i = 0; i < 20; i++) { float a = i * MathF.Tau / 20; Beam(new(MathF.Cos(a) * 1.31f, .35f, -.5f + MathF.Sin(a) * 1.31f), new(MathF.Cos(a) * 1.31f, .53f, -.5f + MathF.Sin(a) * 1.31f), .04f, Glass); } break;
            case 12: // Assembly laboratory and demonstration court
                Lab(0, -.95f, 3.25f, 1.3f, .95f, c);
                for (int i = -2; i <= 2; i++) Arm(i * .6f, .65f); break;
            case 13: // High-bay test hall with offices
                Lab(-.65f, -.5f, 2.15f, 2.35f, 1.3f, c); Lab(1.15f, -.5f, 1.0f, 2.1f, .75f, c);
                Box(-.65f, .1f, .686f, .32f, .36f, .025f, Steel); break;
            case 14: // Sawtooth prototyping workshops
                for (int i = -1; i <= 1; i++)
                {
                    Lab(i * 1.1f, -.6f, 1.0f, 2.1f, .7f, c);
                    Rlgl.PushMatrix(); Rlgl.Translatef(i * 1.1f, .99f, -.6f); Rlgl.Rotatef(16, 0, 0, 1);
                    DrawCube(Vector3.Zero, 1.03f, .05f, 2.12f, c); Rlgl.PopMatrix();
                } break;
            case 15: // Clean-room hall and communications roof
                Lab(0, -.4f, 3.25f, 2.3f, 1.0f, c); Sensor(-.85f, -.7f, 1.155f); Sensor(.85f, -.7f, 1.155f);
                Solar(-.7f, 1.16f, .35f, 5); break;
        }
        // An ordinary-size demonstration robot gives the institute its identity.
        Box(.85f, .1f, 1.65f, .45f, .04f, .36f, Steel); Arm(.73f, 1.65f, .14f);
        Rlgl.PopMatrix();
    }
}
