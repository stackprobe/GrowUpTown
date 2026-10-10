using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class ConcertHallRenderer
{
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: // Waterfront shell auditorium
                Dome(0, -.8f, 3.8f, 2.5f, .85f, c); Pool(-1.8f, 1.25f, .40f); Pool(1.8f, 1.25f, .40f); break;
            case 1: // Three sail-roof recital halls
                for (int i = -1; i <= 1; i++) Gable(i * 1.25f, -.55f, 1.1f, 2.8f, .75f, c, .6f - Math.Abs(i) * .15f);
                Arcade(0, 1.20f, 3.9f, c); break;
            case 2: // Twin shells around a reflecting court
                Dome(-1.12f, -.9f, 1.9f, 2.0f, .8f, c); Dome(1.12f, -.9f, 1.9f, 2.0f, .65f, c); Pool(0, 1.25f, .55f); break;
            case 3: // Waterside glass foyer and recital tower
                Hall(-.65f, -.65f, 2.8f, 2.7f, 1.05f, c); Hall(1.35f, -.9f, .95f, 2.1f, 1.6f, c);
                Atrium(-.65f, 1.05f, 2.5f, .6f, .55f); break;
            case 4: // Garden concert court
                Gable(0, -1.15f, 3.5f, 1.6f, .75f, c); Hall(-1.55f, .35f, .7f, 1.35f, .55f, c);
                Hall(1.55f, .35f, .7f, 1.35f, .55f, c); Tree(0, .6f); break;
            case 5: // Green stepped auditorium
                Hall(0, -.5f, 4.0f, 2.8f, .7f, c); Hall(-.6f, -.85f, 2.2f, 1.7f, 1.15f, c);
                Box(1.22f, .855f, -.5f, .95f, .035f, 2.2f, Green); break;
            case 6: // Timber music village
                for (int i = -1; i <= 1; i++) Gable(i * 1.35f, -.85f + Math.Abs(i) * .4f, 1.1f, 2.0f, .6f, Wood);
                Hall(0, 1.05f, 3.5f, .5f, .4f, c); break;
            case 7: // Circular garden stage hall
                Dome(0, -.8f, 3.1f, 2.8f, .65f, c); Tree(-1.9f, 1); Tree(1.9f, 1); Arcade(0, 1.25f, 2.5f, c); break;
            case 8: // Metropolitan concert hall with lantern
                Hall(0, -.55f, 4.1f, 2.9f, 1.0f, c); Atrium(0, -.55f, 1.55f, 1.8f, 1.45f); break;
            case 9: // Tall auditorium with low entrance wing
                Gable(-.8f, -.7f, 2.2f, 2.7f, 1.4f, c); Hall(1.2f, -.35f, 1.5f, 2.0f, .65f, c); break;
            case 10: // Paired recital blocks and skybridge
                Hall(-1.1f, -.55f, 1.75f, 2.6f, 1.2f, c); Hall(1.1f, -.55f, 1.75f, 2.6f, .9f, c);
                Box(0, .5f, -.55f, .55f, .25f, .6f, Glass); break;
            case 11: // Art-deco music hall
                Hall(0, -.65f, 3.9f, 2.6f, .85f, c); Hall(0, -.65f, 2.4f, 1.65f, 1.25f, c);
                for (int i = -4; i <= 4; i++) Box(i * .38f, .1f, .67f, .04f, .95f, .04f, White); break;
            case 12: // Converted industrial music sheds
                for (int i = -1; i <= 1; i++) Gable(i * 1.3f, -.7f, 1.18f, 2.65f, .85f, c, .3f); break;
            case 13: // Solar performing arts center
                Hall(0, -.6f, 4.0f, 2.65f, .9f, c); Solar(-1.4f, 1.06f, -1.2f, 9); Solar(-1.4f, 1.06f, -.3f, 9); break;
            case 14: // Rehearsal campus with central shell
                Dome(0, -.8f, 2.1f, 2.1f, .9f, c); Hall(-1.7f, -.6f, .7f, 2.0f, .55f, c); Hall(1.7f, -.6f, .7f, 2.0f, .55f, c); break;
            case 15: // Broadcast concert complex
                Hall(-.75f, -.7f, 2.45f, 2.7f, 1.05f, c); Hall(1.3f, -.85f, 1.1f, 2.25f, .75f, c);
                Atrium(0, 1.05f, 3.5f, .6f, .5f); break;
        }
        // Music-note sign at street scale, not a giant decorative instrument.
        Box(-.45f, .1f, 1.85f, .025f, .34f, .025f, Steel);
        Box(-.45f, .34f, 1.85f, .3f, .18f, .025f, c);
        Box(-.42f, .39f, 1.872f, .015f, .10f, .012f, White);
        DrawSphereEx(new(-.45f, .39f, 1.88f), .026f, 4, 6, White);
        Rlgl.PopMatrix();
    }
}
