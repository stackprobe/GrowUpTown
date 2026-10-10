using System.Numerics;

namespace GrowUpTown;

public sealed partial class Game
{
    private void SetupNineCellPreview(string mode)
    {
        city = NewPreviewCity(); city.Money = 100_000_000; city.AppearanceSeed = 42;
        TileKind kind = mode switch { "casino" => TileKind.CasinoResort, "energy" => TileKind.FutureEnergyBase, _ => TileKind.SafariPark };
        if (mode is "menu" or "menu-first" or "locked-menu")
        {
            city = City.NewEmpty(); city.Money = 1_000_000;
            while (city.Month < (mode == "locked-menu" ? 648 : 649)) city.Tick();
            specialOpen = true; specialPage = mode == "menu-first" ? 3 : 4;
        }
        else if (mode == "scale")
        {
            city.Purchase(new(1, 0));
            for (int x = 0; x < 32; x++) city.Build(x, 13, TileKind.Road);
            city.Build(0, 12, TileKind.Road);
            TileKind[] kinds = [TileKind.SafariPark, TileKind.CasinoResort, TileKind.FutureEnergyBase];
            for (int i = 0; i < kinds.Length; i++)
            {
                int x = 1 + i * 10; city.Build(x, 4, kinds[i]); city[x, 4].LandmarkStyle = city[x, 4].LandmarkRotation = 0;
                city.Build(x, 14, TileKind.Home); city.Build(x + 1, 14, TileKind.Shop);
                city.Build(x + 2, 14, TileKind.Park); city.Build(x + 3, 14, TileKind.Factory);
            }
            zoom = 29; angle = .2f; pitch = .75f; focus = new(16.5f, 0, 9.0f);
        }
        else
        {
            city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
            for (int z = 0; z < 40; z++) city.Build(0, z, TileKind.Road);
            for (int row = 0; row < 4; row++) for (int x = 1; x < 41; x++) city.Build(x, row * 10 + 9, TileKind.Road);
            for (int id = 0; id < 16; id++)
            {
                int x = 1 + id % 4 * 10, z = id / 4 * 10; city.Build(x, z, kind);
                city[x, z].LandmarkStyle = id; city[x, z].LandmarkRotation = 0;
            }
            zoom = 59; angle = .15f; pitch = .85f; focus = new(20 + zoom * 140 / H, 0, 19);
        }
        speed = 0; selected = kind; showBuildPanel = showInfoPanel = false;
        message = mode == "scale" ? "55年解放の9×9スペシャル・通常の建物との縮尺比較" : $"{City.Name(kind)}：16種類・55年解放・9×9・周辺支援施設";
        UpdateCamera(); RefreshView(true);
        if (mode == "scale")
        {
            trafficPreview = true; traffic.Update(city, visibleChunks, viewMinX, viewMaxX, viewMinZ, viewMaxZ, 0, trains, false);
            for (int i = 0; i < 3; i++) traffic.AddCar(6 + i * 10, 13, 3, .5f);
        }
    }
}
