using System.Diagnostics;

namespace GrowUpTown;

internal static class BackgroundNetworkTests
{
    private static void Drain(City city)
    {
        var timeout = Stopwatch.StartNew();
        while (city.NetworkUpdatePending && timeout.Elapsed < TimeSpan.FromSeconds(15))
        { city.ServiceBackgroundUpdates(.05); Thread.Sleep(1); }
        if (city.NetworkUpdatePending || city.NetworkUpdateError is not null) throw new InvalidOperationException("Background update did not finish: " + city.NetworkUpdateError);
    }
    internal static void Run(Action<bool, string> check, string path)
    {
        var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; c.EnableDeferredUpdates();
        for (int x = 0; x < 24; x++) c.Build(x, 12, TileKind.Road);
        c.Build(8, 3, TileKind.CasinoResort); c.Build(17, 11, TileKind.Shop);
        check(c.NetworkUpdatePending && c.NetworkJobsStarted == 0 && !c[17, 11].Connected,
            "Construction schedules one background update without recalculating connections");
        c.ServiceBackgroundUpdates(.2);
        check(c.NetworkJobsStarted == 0, "Continuous placement is debounced before launching a worker");
        Drain(c);
        check(c.NetworkJobsStarted == 1 && c[17, 11].Connected && c[17, 11].CasinoAccess
            && c.Chunks[new(0, 0)].Stats.BusinessIncome == 145, "Batched worker applies road connections, special benefits and cached totals");
        var identity = c[17, 11]; var faces = identity.FaceStyles; long money = c.Money;
        c.Build(2, 10, TileKind.Park); c.ServiceBackgroundUpdates(.5);
        check(c.NetworkJobsStarted == 2, "Worker starts after the quiet period");
        c.Build(0, 12, TileKind.Bulldoze);
        Drain(c);
        check(!c[17, 11].Connected && !c[17, 11].CasinoAccess && c.Jobs == 0,
            "Edits during a worker discard stale connection results and compute the latest topology");
        check(ReferenceEquals(identity, c[17, 11]) && ReferenceEquals(faces, identity.FaceStyles)
            && c.Money == money - City.Cost(TileKind.Park) - City.Cost(TileKind.Bulldoze),
            "Network application preserves live tile identity, appearance and charged funds");
        c.Build(0, 12, TileKind.Road); c.Save(path);
        var restored = City.Load(path);
        check(restored[17, 11].Connected && restored[17, 11].CasinoAccess,
            "Saving while an update is pending preserves edits and loading rebuilds derived state");
        c.StopBackgroundUpdates();
        check(!c.NetworkUpdatePending, "Closing or replacing a city abandons its isolated worker safely");

        var cold = SelfTests.DevelopedCity(); cold.Money = 100_000_000; cold.Purchase(new(1, 0));
        for (int x = 0; x < 35; x++) cold.Build(x, 12, TileKind.Road);
        cold.Build(26, 11, TileKind.Home); cold.SetActiveChunks([]); cold.Tick();
        check(cold.Chunks[new(1, 0)].IsPacked, "Background fixture includes compressed offscreen buildings");
        cold.EnableDeferredUpdates(); cold.Build(0, 12, TileKind.Bulldoze); cold.ServiceBackgroundUpdates(.5);
        check(cold.Chunks[new(1, 0)].IsPacked, "Capturing a background snapshot does not expand cold live chunks");
        Drain(cold);
        check(cold.Chunks[new(1, 0)].IsPacked && cold.Capacity == 0,
            "Worker recompresses offscreen results and applies them without owner-thread expansion");
        cold.Save(path); restored = City.Load(path);
        check(!restored[26, 11].Connected, "Background cold-chunk connections survive save and reload");

        var moving = SelfTests.DevelopedCity(); moving.Money = 100_000_000;
        for (int x = 0; x < 24; x++) moving.Build(x, 12, TileKind.Road);
        moving.Build(4, 11, TileKind.Home); moving.Build(5, 11, TileKind.Shop);
        moving.EnableDeferredUpdates(); moving.Build(7, 11, TileKind.Park); moving.ServiceBackgroundUpdates(.5);
        moving.Tick(); int residents = moving[4, 11].Residents, month = moving.Month; money = moving.Money;
        Drain(moving);
        check(moving[4, 11].Residents == residents && moving.Month == month && moving.Money == money,
            "Worker results never rewind residents, simulation time or finances advanced during calculation");

        var busy = new City(false) { Money = 100_000_000 }; busy.EnableDeferredUpdates();
        for (int i = 0; i < 20; i++) { busy.Build(i, 12, TileKind.Road); busy.ServiceBackgroundUpdates(.1); }
        busy.ServiceBackgroundUpdates(.1);
        check(busy.NetworkJobsStarted > 0, "Continuous editing triggers a bounded-delay job rather than waiting forever for idle time");
        Drain(busy);
        check(busy[19, 12].Connected, "The final continuous-edit result reaches the latest road endpoint");
        foreach (var (source, target) in new[] { (TileKind.MarineResort, TileKind.Home), (TileKind.ExpoVenue, TileKind.Shop), (TileKind.MechaArsenal, TileKind.Factory) })
        {
            var town = SelfTests.DevelopedCity(); town.Money = 100_000_000;
            for (int x = 0; x < 24; x++) town.Build(x, 12, TileKind.Road);
            town.Build(8, 2, source); town.Build(18, 11, target);
            town.Purchase(new(1, 0));
            town.SetActiveChunks([]); town.Tick();
            town.EnableDeferredUpdates(); town.Build(25, 10, TileKind.Park); Drain(town);
            check(town.Chunks[new(0, 0)].Stats.Comfort == (target == TileKind.Home ? 25 : 0)
                && town.Chunks[new(0, 0)].Stats.BusinessIncome == (target == TileKind.Shop ? 150 : target == TileKind.Factory ? 240 : 0),
                $"{source} benefits are carried through compressed background results");
            town.Build(0, 12, TileKind.Bulldoze); Drain(town);
            check(!town[18, 11].ExpoAccess && !town[18, 11].ArsenalAccess && town[18, 11].Comfort == 0,
                $"{source} disconnection clears its benefits through background application");
        }
        Benchmark(check);
    }
    private static void Benchmark(Action<bool, string> check)
    {
        City Fixture()
        {
            var city = SelfTests.DevelopedCity(); city.Money = 100_000_000;
            city.Purchase(new(1, 0)); city.Purchase(new(0, 1)); city.Purchase(new(1, 1));
            for (int z = 0; z < 48; z++) for (int x = 0; x < 48; x++)
            {
                if (x % 4 == 3 && z % 4 == 3) continue;
                var kind = x % 4 == 0 || z % 4 == 0 ? TileKind.Road : (x + z) % 2 == 0 ? TileKind.Home : TileKind.Shop;
                var key = ChunkPos.FromTile(x, z);
                city.Chunks[key].Cells[(z - key.OriginZ) * City.Size + x - key.OriginX] =
                    new Tile { Kind = kind, FaceStyles = kind == TileKind.Road ? [] : [0, 1, 2, 3] };
            }
            city.Reconnect(); return city;
        }
        var synchronous = Fixture(); var deferred = Fixture(); deferred.EnableDeferredUpdates();
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 24; i++) synchronous.Build(3 + i % 12 * 4, 3 + i / 12 * 4, TileKind.Forest);
        double before = timer.Elapsed.TotalMilliseconds; timer.Restart();
        for (int i = 0; i < 24; i++) deferred.Build(3 + i % 12 * 4, 3 + i / 12 * 4, TileKind.Forest);
        double after = timer.Elapsed.TotalMilliseconds;
        check(deferred.NetworkJobsStarted == 0 && deferred.NetworkUpdatePending, "Large-town placement benchmark performs no synchronous network recalculation");
        Drain(deferred);
        check(deferred.Capacity == synchronous.Capacity && deferred.Jobs == synchronous.Jobs
            && deferred.Chunks.Values.Sum(c => c.Stats.Comfort) == synchronous.Chunks.Values.Sum(c => c.Stats.Comfort),
            "Deferred large-town results match the synchronous reference after convergence");
        Console.WriteLine($"Placement benchmark (4 chunks, 24 edits): synchronous {before:F1} ms; deferred {after:F1} ms (recalculation excluded)");
    }
}
