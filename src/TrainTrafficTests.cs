using System.Numerics;

namespace GrowUpTown;

internal static class TrainTrafficTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        City New() { var c = SelfTests.DevelopedCity(); c.Money = 100_000_000; return c; }
        bool straightOnly = true, joins = true, turns = true;
        Vector2[] arms = [new(0, 1), new(1, 0), new(0, -1), new(-1, 0)];
        for (int mask = 1; mask < 16; mask++) for (int entry = 0; entry < 4; entry++)
        {
            if ((mask & (1 << entry)) == 0) continue;
            var exits = new HashSet<int>();
            for (uint seed = 0; seed < 16; seed++)
            {
                int exit = TrainTraffic.ChooseExit(mask, entry, seed); exits.Add(exit);
                straightOnly &= mask != 15 || exit == (entry + 2) % 4;
                var start = TrainTraffic.Pose(entry, exit, 0); var end = TrainTraffic.Pose(entry, exit, 1);
                if (entry != exit) joins &= Vector2.Distance(start.Position, arms[entry] * .5f) < .0001f
                    && Vector2.Distance(end.Position, arms[exit] * .5f) < .0001f
                    && Vector2.Distance(start.Heading, -arms[entry]) < .0001f
                    && Vector2.Distance(end.Heading, arms[exit]) < .0001f;
            }
            if (BitOperations.PopCount((uint)mask) == 3) turns &= exits.Count == 2 && !exits.Contains(entry);
        }
        check(straightOnly && turns, "Trains travel straight across diamonds and can take either exit at T junctions");
        check(joins, "Train routes meet exactly at rail edges with continuous headings");

        var city = New();
        for (int x = 2; x <= 20; x++) city.Build(x, 10, TileKind.Rail);
        for (int z = 2; z <= 20; z++) city.Build(10, z, TileKind.Rail);
        city.Build(3, 11, TileKind.Station); city.Build(11, 3, TileKind.Station);
        check(!city[3, 11].RailActive && !city[11, 3].RailActive, "Diamond crossing does not join perpendicular station networks");
        city.Build(17, 11, TileKind.Station);
        check(city[3, 11].RailActive && city[17, 11].RailActive && !city[11, 3].RailActive,
            "Stations on the same straight route connect through a diamond");
        city.Save(path); city = City.Load(path);
        check(!city[11, 3].RailActive, "Diamond direction separation survives save/load");
        city.Build(10, 11, TileKind.Bulldoze);
        check(city[11, 3].RailActive, "Converting a diamond to a T junction joins its station networks");

        city = New();
        for (int x = 1; x <= 20; x++) city.Build(x, 10, TileKind.Rail);
        city.Build(5, 10, TileKind.Road); city.Build(6, 10, TileKind.Road);
        city.Build(5, 11, TileKind.Station);
        var chunks = city.Chunks.Values.ToArray(); var trains = new TrainTraffic();
        trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        trains.AddTrain(4, 10, 3);
        double time = 0; bool stopped = false, resumed = false, centered = true; float waitDuration = 0;
        check(trains.Carriages.Single().Length == 3, "All three carriages are visible from the moment a train spawns");
        for (int i = 0; i < 180; i++)
        {
            time += .1; trains.Update(city, chunks, 0, 23, 0, 23, time, spawn: false);
            foreach (var p in trains.Positions)
            {
                if (p.Wait > 0)
                {
                    stopped = true; waitDuration += .1f;
                    var carriage = trains.Carriages.Single();
                    centered &= Math.Abs(carriage.Average(c => c.Position.X) - 5.5f) < .001f
                        && carriage.All(c => c.Position.X - .24f >= 4.5f && c.Position.X + .24f <= 6.5f);
                }
                if (stopped && p.X >= 7) resumed = true;
            }
        }
        check(stopped && waitDuration >= 2.9f && waitDuration <= 3.2f && resumed,
            "Train stops once for three seconds at a two-cell platform and then resumes");
        check(centered && stopped, "The entire three-car train stops centered within the platform, including straight crossing rails");
        var paused = trains.Positions.ToArray();
        trains.Update(city, chunks, 0, 23, 0, 23, time, spawn: false);
        check(paused.SequenceEqual(trains.Positions), "Pausing freezes train position and station dwell time");

        city = New();
        for (int x = 1; x <= 22; x++) city.Build(x, 10, TileKind.Rail);
        for (int z = 1; z <= 20; z++) city.Build(10, z, TileKind.Road);
        chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic(); var cars = new TrafficRenderer();
        trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        cars.Update(city, chunks, 0, 23, 0, 23, 0, trains, false);
        check(!trains.StopCars(10, 10) && trains.Closure(10, 10) == 0, "Crossing is normally open and unlit");
        trains.AddTrain(7, 10, 3); cars.AddCar(10, 9, 2);
        bool warning = false, fullyClosed = false, carStopped = false, carResumed = false, cleared = false, safe = true, blinking = true, rearProtected = true;
        float previousCar = -1;
        for (int i = 1; i <= 160; i++)
        {
            time = i * .1;
            trains.Update(city, chunks, 0, 23, 0, 23, time, cars.HasCarOnTile, false);
            cars.Update(city, chunks, 0, 23, 0, 23, time, trains, false);
            warning |= trains.StopCars(10, 10);
            fullyClosed |= trains.Closure(10, 10) >= .999f;
            if (trains.StopCars(10, 10))
                blinking &= trains.LampOn(10, 10, 0, time) != trains.LampOn(10, 10, 1, time);
            foreach (var car in cars.Motion)
            {
                if (car.Z == 9 && car.Progress == previousCar && car.Progress > .5f) carStopped = true;
                previousCar = car.Progress;
                if (carStopped && car.Z > 10) carResumed = true;
            }
            foreach (var p in trains.Positions)
                if (p.X == 10) safe &= trains.Closure(10, 10) >= .999f && !cars.HasCarOnTile(10, 10);
            foreach (var carriages in trains.Carriages)
                if (carriages.Any(c => c.Position.X + .24f >= 9.5f && c.Position.X - .24f <= 10.5f))
                    rearProtected &= trains.Closure(10, 10) >= .999f && trains.StopCars(10, 10);
            if (fullyClosed && !trains.StopCars(10, 10)) cleared = true;
        }
        check(warning && fullyClosed && carStopped && carResumed && cleared && safe && blinking,
            "Approach closes the crossing, stops cars, holds through passage, then reopens and releases traffic");
        check(rearProtected, "The crossing stays fully closed until the last of all three carriages clears it");
        trains = new TrainTraffic(); trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        trains.AddTrain(9, 10, 3);
        for (int i = 1; i <= 40; i++) trains.Update(city, chunks, 0, 23, 0, 23, i * .1, (_, _) => true, false);
        check(trains.Positions.Single().X == 9, "Train waits before a closed crossing while a car is still inside");
        var gate = trains.Closure(10, 10); paused = trains.Positions.ToArray();
        trains.Update(city, chunks, 0, 23, 0, 23, 4, (_, _) => true, false);
        check(trains.Closure(10, 10) == gate && paused.SequenceEqual(trains.Positions), "Pause freezes barrier animation and crossing wait");
        for (int i = 41; i <= 65; i++) trains.Update(city, chunks, 0, 23, 0, 23, i * .1, (_, _) => false, false);
        check(trains.Positions.Single().X >= 10, "Train proceeds when the crossing is clear");
        city.Build(10, 10, TileKind.Bulldoze);
        trains.Update(city, chunks, 0, 23, 0, 23, 6.6, spawn: false);
        check(trains.Count == 0 && !trains.StopCars(10, 10), "Demolition removes affected trains and releases the crossing");
        trains.Update(New(), [], 0, 23, 0, 23, 7);
        check(trains.Count == 0, "New city clears train and crossing state");

        city = New();
        for (int x = 2; x <= 20; x++) city.Build(x, 10, TileKind.Rail);
        for (int z = 2; z <= 20; z++) city.Build(10, z, TileKind.Rail);
        chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
        trains.Update(city, chunks, 0, 23, 0, 23, 0);
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval);
        check(trains.Count == 2, "Automatic spawning treats a diamond as two independent railway routes");
        for (int i = 1; i <= 600; i++) trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval + i * .1);
        check(trains.Count == 2, "A long route retains one train rather than spawning opposing trains");
        trains.Update(city, chunks, 0, 1, 0, 1, TrainTraffic.SpawnInterval + 60.1);
        check(trains.Count == 0, "Camera movement removes trains outside the visible tracks");

        trains = new TrainTraffic(); trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        trains.AddTrain(9, 10, 3); trains.AddTrain(10, 9, 2);
        bool separate = true, bothPassed = false;
        for (int i = 1; i <= 100; i++)
        {
            trains.Update(city, chunks, 0, 23, 0, 23, i * .1, spawn: false);
            var positions = trains.Positions.ToArray();
            separate &= positions.Count(p => p.X == 10 && p.Z == 10) <= 1;
            bothPassed |= positions.Any(p => p.X > 10 && p.Z == 10) && positions.Any(p => p.X == 10 && p.Z > 10);
        }
        check(separate && bothPassed, "Perpendicular trains wait their turn and both clear the diamond crossing");

        city = New(); city.Purchase(new(1, 0));
        foreach (var chunk in city.Chunks.Values)
            for (int z = 1; z < 23; z += 3) for (int x = 1; x < 23; x += 4)
            {
                chunk.Cells[z * City.Size + x] = new() { Kind = TileKind.Rail };
                chunk.Cells[z * City.Size + x + 1] = new() { Kind = TileKind.Rail };
                chunk.Cells[z * City.Size + x + 2] = new() { Kind = TileKind.Rail };
            }
        trains = new TrainTraffic(); trains.Update(city, city.Chunks.Values.ToArray(), 0, 47, 0, 23, 0);
        trains.Update(city, city.Chunks.Values.ToArray(), 0, 47, 0, 23, TrainTraffic.SpawnInterval);
        check(trains.Count == TrainTraffic.MaxTrains, "Many visible railway networks respect the train rendering budget");

        city = New(); chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
        trains.Update(city, chunks, 0, 23, 0, 23, 0);
        for (int x = 1; x <= 20; x++) city.Build(x, 10, TileKind.Rail);
        trains.Update(city, chunks, 0, 23, 0, 23, .5);
        check(trains.Count == 0, "Building track does not immediately spawn a train");
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval - .1);
        check(trains.Count == 0, "Track edits do not advance the independent spawn timer");
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval);
        check(trains.Count == 1 && trains.Carriages.Single().Length == 3,
            "A full train appears within two seconds without any additional player action");
        var unchanged = trains.Carriages.Single();
        city.Build(21, 10, TileKind.Rail);
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval);
        check(unchanged.SequenceEqual(trains.Carriages.Single()), "Unrelated track construction neither creates nor moves the existing train while paused");

        trains = new TrainTraffic(); trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        trains.AddTrain(10, 10, 3); unchanged = trains.Carriages.Single();
        city.Build(12, 10, TileKind.Bulldoze);
        trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
        check(trains.Count == 1 && unchanged.SequenceEqual(trains.Carriages.Single()),
            "Removing future track replans the route without replacing or teleporting intact carriages");

        city = New();
        for (int x = 1; x <= 22; x++) city.Build(x, 10, TileKind.Rail);
        chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
        bool RightScreen(Vector2 p) => p.X >= 15 && p.X <= 22;
        trains.Update(city, chunks, 0, 23, 0, 23, 0, onScreen: RightScreen);
        trains.AddTrain(3, 10, 1);
        check(!trains.Carriages.Any(c => c.Any(car => RightScreen(car.Position))), "Offscreen train fixture lies outside the actual visible screen");
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval, onScreen: RightScreen);
        check(trains.Count == 1 && trains.Carriages.Any(c => c.Any(car => RightScreen(car.Position))),
            "A train hidden in render margins does not prevent spawning on actual visible rails");
        int visibleFrames = 0, emptyFrames = 0, longestEmpty = 0;
        for (int i = 1; i <= 1200; i++)
        {
            trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval + i * .1, onScreen: RightScreen);
            if (trains.Carriages.Any(c => c.Any(car => RightScreen(car.Position)))) { visibleFrames++; emptyFrames = 0; }
            else { emptyFrames++; longestEmpty = Math.Max(longestEmpty, emptyFrames); }
        }
        check(visibleFrames > 1200 * .8 && longestEmpty <= 22,
            "Visible rails retain a visible train most of the time and refill within about two seconds without player input");
        bool LeftScreen(Vector2 p) => p.X >= 1 && p.X <= 8;
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval + 120, onScreen: LeftScreen);
        check(!trains.Carriages.Any(c => c.Any(car => LeftScreen(car.Position))), "Moving the camera while paused does not immediately spawn or teleport a train");
        trains.Update(city, chunks, 0, 23, 0, 23, 2 * TrainTraffic.SpawnInterval + 120, onScreen: LeftScreen);
        check(trains.Count == 1 && trains.Carriages.Any(c => c.Any(car => LeftScreen(car.Position))),
            "The spawn timer restores a visible train after camera movement without construction");

        city = New();
        for (int x = 1; x <= 10; x++) { city.Build(x, 10, TileKind.Rail); if (x > 4 && x < 8) city.Build(x, 10, TileKind.Road); }
        chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
        trains.Update(city, chunks, 0, 23, 0, 23, 0);
        trains.Update(city, chunks, 0, 23, 0, 23, TrainTraffic.SpawnInterval, onScreen: p => p.X >= 8 && p.X <= 10);
        check(trains.Count == 1 && trains.Carriages.Single().All(c => c.Position.X > 7.5f),
            "Automatic spawning tries the opposite direction to fit all carriages away from an open crossing");

        // Both axes and both travel directions, repeated switchbacks. A reversal
        // must only swap the leading cab, with no jump or rotation of the cars.
        for (int direction = 0; direction < 4; direction++)
        {
            city = New(); bool horizontal = direction % 2 == 1;
            for (int i = 2; i <= 20; i++) city.Build(horizontal ? i : 10, horizontal ? 10 : i, TileKind.Rail);
            city.Build(horizontal ? 8 : 11, horizontal ? 11 : 8, TileKind.Station);
            chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
            trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false);
            trains.AddTrain(10, 10, (direction + 2) % 4);
            bool stationCentered = false, switchback = true, fits = true; int reversals = 0; float dwell = 0;
            var previous = trains.Carriages.Single();
            for (int i = 1; i <= 6000; i++)
            {
                trains.Update(city, chunks, 0, 23, 0, 23, i / 60.0, spawn: false);
                var current = trains.Carriages.Single(); var p = trains.Positions.Single();
                float Coord(Vector2 v) => horizontal ? v.X : v.Y;
                fits &= current.All(c => Coord(c.Position) >= 2.16f && Coord(c.Position) <= 19.84f);
                if (p.Wait > 0 && (horizontal ? p.X : p.Z) is not (2 or 20))
                    stationCentered |= Math.Abs(current.Average(c => Coord(c.Position)) - 8.5f) < .001f;
                if (p.Wait > 0 && (horizontal ? p.X : p.Z) is 2 or 20) dwell += 1f / 60;
                if (Vector2.Distance(previous[0].Position, current[^1].Position) < .001f)
                {
                    reversals++;
                    for (int car = 0; car < 3; car++)
                        switchback &= Vector2.Distance(previous[car].Position, current[2 - car].Position) < .001f
                            && Vector2.Distance(previous[car].Heading, current[2 - car].Heading) < .001f;
                }
                else switchback &= current.Zip(previous).All(pair => Vector2.Distance(pair.First.Position, pair.Second.Position) < .02f);
                previous = current;
            }
            check(stationCentered, $"Direction {direction}: train centers all carriages on the platform");
            check(switchback && fits && reversals >= 2 && dwell >= reversals * 1.9f && trains.Count == 1,
                $"Direction {direction}: repeated terminal switchbacks preserve carriage positions and body headings after a two-second dwell");
        }
        city = New();
        for (int x = 3; x <= 10; x++) city.Build(x, 10, TileKind.Rail);
        city.Build(10, 9, TileKind.Rail);
        chunks = city.Chunks.Values.ToArray(); trains = new TrainTraffic();
        trains.Update(city, chunks, 0, 23, 0, 23, 0, spawn: false); trains.AddTrain(8, 10, 3);
        var beforeCurve = trains.Carriages.Single(); bool curveSwitchback = true; int curveReversals = 0;
        for (int i = 1; i <= 3600; i++)
        {
            trains.Update(city, chunks, 0, 23, 0, 23, i / 60.0, spawn: false);
            var current = trains.Carriages.Single();
            if (Vector2.Distance(beforeCurve[0].Position, current[^1].Position) < .001f)
            {
                curveReversals++;
                for (int car = 0; car < 3; car++)
                    curveSwitchback &= Vector2.Distance(beforeCurve[car].Position, current[2 - car].Position) < .001f
                        && Vector2.Distance(beforeCurve[car].Heading, current[2 - car].Heading) < .001f;
            }
            else curveSwitchback &= current.Zip(beforeCurve).All(pair => Vector2.Distance(pair.First.Position, pair.Second.Position) < .02f);
            beforeCurve = current;
        }
        check(curveSwitchback && curveReversals >= 2, "Switchbacks also preserve carriage positions and orientations when the rear cars are on a curve");
    }
}
