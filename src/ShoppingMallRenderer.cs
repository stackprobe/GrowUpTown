using Raylib_cs;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class ShoppingMallRenderer
{
    private static void Shops(float x, float z, float width, Color c)
    {
        Hall(x, z, width, .65f, .48f, c);
        for (float px = -width / 2 + .22f; px < width / 2; px += .44f)
        {
            Box(x + px, .1f, z + .335f, .17f, .25f, .02f, Glass);
            Box(x + px, .39f, z + .37f, .35f, .06f, .10f, c);
        }
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: // Waterfront promenade mall
                Hall(0, -.8f, 4.15f, 2.2f, .8f, c); Shops(0, .65f, 3.7f, c); Pool(-2.1f, 1.35f, .30f); Pool(2.1f, 1.35f, .30f); break;
            case 1: // Twin sail-roof market halls
                Gable(-1.03f, -.7f, 1.85f, 2.6f, .75f, c); Gable(1.03f, -.7f, 1.85f, 2.6f, .75f, c);
                Atrium(0, -.7f, .4f, 2.55f, .55f); break;
            case 2: // Canal courtyard
                Hall(-1.58f, -.55f, .95f, 2.7f, .7f, c); Hall(1.58f, -.55f, .95f, 2.7f, .7f, c);
                Shops(0, -1.6f, 2.2f, c); Pool(0, -.1f, .72f); break;
            case 3: // Terraced harbor shopping center
                Hall(0, -.6f, 4.2f, 2.7f, .7f, c); Hall(-.6f, -.85f, 2.4f, 1.75f, 1.15f, c);
                Arcade(1.1f, .65f, 1.0f, c); break;
            case 4: // Open-air garden mall
                for (int i = -1; i <= 1; i++) { Shops(i * 1.35f, -1.55f, 1.05f, c); Shops(i * 1.35f, .25f, 1.05f, c); }
                Tree(-.65f, -.65f); Tree(.65f, -.65f); break;
            case 5: // Green roof and central atrium
                Hall(0, -.65f, 4.1f, 2.7f, .8f, c); Box(0, .96f, -.65f, 3.8f, .04f, 2.4f, Green);
                Atrium(0, -.65f, .9f, 2.5f, 1.2f); break;
            case 6: // Village gables and shopping street
                for (int i = -1; i <= 1; i++) Gable(i * 1.35f, -.95f, 1.1f, 1.75f, .65f, c);
                Shops(-1.1f, .55f, 1.65f, c); Shops(1.1f, .55f, 1.65f, c); break;
            case 7: // Circular garden rotunda
                Dome(0, -.85f, 3.0f, 2.6f, .75f, c); Shops(-1.72f, .65f, .7f, c); Shops(1.72f, .65f, .7f, c); break;
            case 8: // Metropolitan glass gallery
                Hall(0, -.65f, 4.2f, 2.8f, 1.05f, c); Atrium(0, -.65f, 1.2f, 2.75f, 1.5f); break;
            case 9: // Two anchor stores with connecting concourse
                Hall(-1.3f, -.65f, 1.5f, 2.7f, 1.2f, c); Hall(1.3f, -.65f, 1.5f, 2.7f, 1.2f, c);
                Atrium(0, -.65f, 1.1f, 2.5f, .65f); break;
            case 10: // Three stepped retail blocks
                for (int i = -1; i <= 1; i++) Hall(i * 1.35f, -.65f, 1.2f, 2.55f, .65f + (i + 1) * .28f, c); break;
            case 11: // Enclosed courtyard with arcaded entrance
                Hall(0, -1.35f, 4.1f, 1.15f, 1.0f, c); Hall(-1.55f, .1f, 1.0f, 1.65f, .75f, c);
                Hall(1.55f, .1f, 1.0f, 1.65f, .75f, c); Arcade(0, .95f, 2.15f, c); break;
            case 12: // Reused sawtooth market sheds
                for (int i = -1; i <= 1; i++) Gable(i * 1.35f, -.65f, 1.2f, 2.65f, .8f, c, .25f); break;
            case 13: // Solar regional mall
                Hall(0, -.65f, 4.1f, 2.7f, .8f, c); Solar(-1.55f, .96f, -1.3f, 10); Solar(-1.55f, .96f, -.5f, 10); break;
            case 14: // Outlet campus
                for (int i = -1; i <= 1; i++) { Hall(i * 1.35f, -1.3f, 1.15f, 1.0f, .6f, c); Shops(i * 1.35f, .35f, 1.15f, c); } break;
            case 15: // Food hall with raised skylights
                Hall(0, -.7f, 4.0f, 2.55f, .85f, c);
                for (int i = -1; i <= 1; i++) Atrium(i * 1.1f, -.7f, .7f, 1.9f, 1.12f);
                Shops(0, .9f, 3.6f, c); break;
        }
        Parking(1.85f, c);
        Rlgl.PopMatrix();
    }
}
