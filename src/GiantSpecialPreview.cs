using System.Numerics;

namespace GrowUpTown;

public sealed partial class Game
{
    private void SetupGiantSpecialPreview(string mode)
    {
        city = NewPreviewCity(); city.Money = 100_000_000; city.AppearanceSeed = 42;
        TileKind kind = mode switch { "park" => TileKind.AmusementPark, "launch" => TileKind.RocketLaunchSite, _ => TileKind.Zoo };
        if (mode is "menu" or "locked-menu")
        {
            city = City.NewEmpty(); city.Money = 1_000_000;
            while (city.Month < (mode == "locked-menu" ? 528 : 529)) city.Tick();
            specialOpen = true; specialPage = 2;
        }
        else if (mode == "scale")
        {
            for (int x = 0; x < 24; x++) city.Build(x, 13, TileKind.Road);
            city.Build(0, 12, TileKind.Road);
            TileKind[] kinds = [TileKind.Zoo, TileKind.AmusementPark, TileKind.RocketLaunchSite];
            for (int i = 0; i < kinds.Length; i++)
            {
                int x = 1 + i * 8; city.Build(x, 6, kinds[i]); city[x, 6].LandmarkStyle = city[x, 6].LandmarkRotation = 0;
                city.Build(x, 14, TileKind.Home); city.Build(x + 1, 14, TileKind.Shop);
                city.Build(x + 2, 14, TileKind.Park); city.Build(x + 3, 14, TileKind.Factory);
            }
            zoom = 22; angle = .2f; pitch = .75f; focus = new(13, 0, 10.0f);
        }
        else
        {
            city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
            for (int z = 0; z < 32; z++) city.Build(0, z, TileKind.Road);
            for (int row = 0; row < 4; row++) for (int x = 1; x < 33; x++) city.Build(x, row * 8 + 7, TileKind.Road);
            for (int id = 0; id < 16; id++)
            {
                int x = 1 + id % 4 * 8, z = id / 4 * 8; city.Build(x, z, kind);
                city[x, z].LandmarkStyle = id; city[x, z].LandmarkRotation = 0;
            }
            zoom = 47; angle = .15f; pitch = .85f; focus = new(16 + zoom * 140 / H, 0, 15);
        }
        speed = 0; selected = kind; showBuildPanel = showInfoPanel = false;
        message = mode == "scale" ? "45年解放の7×7スペシャル・通常の建物との縮尺比較" : $"{City.Name(kind)}：16種類・45年解放・7×7・周辺支援施設";
        UpdateCamera(); RefreshView(true);
        if (mode == "scale")
        {
            trafficPreview = true; traffic.Update(city, visibleChunks, viewMinX, viewMaxX, viewMinZ, viewMaxZ, 0, trains, false);
            for (int i = 0; i < 3; i++) traffic.AddCar(6 + i * 8, 13, 3, .5f);
        }
    }
}
