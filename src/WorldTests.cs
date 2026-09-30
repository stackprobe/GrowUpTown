using System.Numerics;
using System.Text.Json.Nodes;

namespace GrowUpTown;

internal static class WorldTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        check(ChunkPos.FromTile(-1, -24) == new ChunkPos(-1, -1) && ChunkPos.FromTile(-25, 24) == new ChunkPos(-2, 1), "Negative world coordinates use floor division");
        var city = new City();
        check(city.Chunks.Count == 1 && city.Inside(23, 23) && !city.Inside(24, 0) && !city.Inside(-1, 0), "New towns own exactly one 24 by 24 district");
        long money = city.Money;
        city.Build(24, 12, TileKind.Road);
        check(city.Money == money && city[24, 12].Kind == TileKind.Empty, "Construction on unowned land is rejected without charging");
        city.Money = City.LandPrice - 1;
        city.Purchase(new(1, 0));
        check(city.Chunks.Count == 1 && city.Money == City.LandPrice - 1, "Insufficient land funds leave money and ownership unchanged");
        city.Money = City.LandPrice;
        city.Purchase(new(1, 1));
        check(city.Chunks.Count == 1 && city.Money == City.LandPrice, "Diagonal-only land purchase is rejected");
        city.Purchase(new(1, 0));
        check(city.Chunks.Count == 2 && city.Money == 0 && city.Inside(47, 23) && !city.Inside(48, 23), "Adjacent purchase costs exactly one million and adds 576 tiles");
        city.Money = 10_000_000;
        city.Purchase(new(1, 0));
        check(city.Chunks.Count == 2 && city.Money == 10_000_000, "Duplicate purchase cannot charge again");
        city.Purchase(new(-1, 0)); city.Purchase(new(0, -1));
        check(city.Inside(-24, 0) && city.Inside(0, -24) && city.Money == 8_000_000, "Western and northern land can be purchased");
        for (int x = 17; x <= 26; x++) city.Build(x, 12, TileKind.Road);
        city.Build(25, 11, TileKind.Home);
        city.Build(-1, 12, TileKind.Road); city.Build(-1, 11, TileKind.Home);
        for (int z = 7; z >= -1; z--) city.Build(10, z, TileKind.Road);
        city.Build(11, -1, TileKind.Shop);
        check(city[25, 11].Connected && city[-1, 11].Connected && city[11, -1].Connected, "Roads connect buildings across positive and negative chunk seams");
        city.Build(23, 11, TileKind.Park); city.Build(24, 11, TileKind.Home);
        check(city.LocalComfort(24, 11) > 0, "Park comfort crosses district boundaries");
        city.Build(23, 12, TileKind.Bulldoze);
        check(!city[25, 11].Connected, "Removing a boundary road disconnects the remote district");
        city.Build(23, 12, TileKind.Road);
        check(city[25, 11].Connected, "Restoring the boundary road reconnects remote buildings");
        city.Build(47, 5, TileKind.Road); city.Build(46, 5, TileKind.Factory);
        check(!city[46, 5].Connected, "An expanded map edge does not create a new external inlet");
        city.Build(0, 12, TileKind.Bulldoze);
        check(city.Capacity == 0 && city.Jobs == 0, "The original inlet remains the source after westward expansion");
        city.Build(0, 12, TileKind.Road);
        check(city.Capacity > 0 && city.Jobs > 0, "The original inlet can be restored");
        int[] faces = city[-1, 11].FaceStyles.ToArray();
        city.SetActiveChunks([]);
        check(city.Chunks.Values.Where(c => c.Stats.Placed > 0).All(c => c.IsPacked), "Inactive building details are compressed");
        money = city.Money;
        city.Tick();
        check(city.LastExpense > 0 && city.Money == money + city.LastIncome - city.LastExpense, "Offscreen districts still pay taxes and maintenance every month");
        city.Save(path);
        var loaded = City.Load(path);
        check(loaded.Money == city.Money && loaded.Chunks.Keys.SequenceEqual(city.Chunks.Keys) && loaded[-1, 11].FaceStyles.SequenceEqual(faces), "Purchased districts, negative coordinates and facades survive saving");
        check(loaded.Chunks.All(p => p.Value.LastSimulatedMonth == city.Chunks[p.Key].LastSimulatedMonth), "Save/load preserves pending simulation months");
        for (int i = 0; i < 36; i++) city.Tick();
        check(city.Chunks.Values.All(c => city.Month - 1 - c.LastSimulatedMonth < City.DistantUpdateInterval), "Staggered background updates keep catch-up bounded");
        city.SetActiveChunks([new(0, 0), new(1, 0), new(-1, 0), new(0, -1)]);
        check(city.Chunks.Values.All(c => c.LastSimulatedMonth == city.Month - 1), "Re-entering all districts catches them up immediately");

        var sleeping = new City();
        sleeping[6, 11].Residents = 20; sleeping[6, 11].Growth = 5; sleeping.Reconnect();
        var originalFaces = sleeping[6, 11].FaceStyles.ToArray();
        sleeping.SetActiveChunks([]);
        for (int i = 0; i < 5; i++) sleeping.Tick();
        check(sleeping.Chunks[new(0, 0)].LastSimulatedMonth == 0, "Offscreen development is deferred between scheduled updates");
        sleeping.Save(path);
        var waking = City.Load(path);
        money = waking.Money;
        waking.SetActiveChunks([new(0, 0)]);
        check(waking[6, 11].Level > 1 && waking[6, 11].FaceStyles.Take(4).SequenceEqual(originalFaces), "Catch-up grows new floors while preserving existing artwork");
        check(waking.Money == money, "Catch-up never charges or credits past months a second time");
        var stable = waking[6, 11].FaceStyles.ToArray(); long population = waking.Population;
        waking.SetActiveChunks([]); waking.SetActiveChunks([new(0, 0)]);
        check(waking[6, 11].FaceStyles.SequenceEqual(stable) && waking.Population == population, "Repeated camera visits do not advance the same month twice");
        sleeping.TaxRate = 20;
        check(sleeping.Chunks[new(0, 0)].LastSimulatedMonth == sleeping.Month - 1, "Tax changes settle the previous offscreen conditions first");

        city.Save(path);
        string valid = File.ReadAllText(path);
        void Reject(Action<JsonNode> mutate, string name)
        {
            var json = JsonNode.Parse(valid)!; mutate(json); File.WriteAllText(path, json.ToJsonString());
            bool rejected = false; try { City.Load(path); } catch (InvalidDataException) { rejected = true; }
            check(rejected, name);
        }
        Reject(j => j["Chunks"]!.AsArray().Add(j["Chunks"]![0]!.DeepClone()), "Duplicate saved districts are rejected");
        Reject(j => j["Chunks"]![0]!["X"] = 1000, "Disconnected saved ownership is rejected");
        Reject(j => j["Chunks"]![0]!["LastSimulatedMonth"] = city.Month + 1, "Future simulation timestamps are rejected");
        Reject(j => j["Version"] = 2, "Pre-expansion saves get an explicit old-format error");

        var roads = new City(false) { Money = 10_000_000 };
        roads.Purchase(new(-1, 0));
        roads.Build(0, 5, TileKind.Road);
        (int x, int z)[] neighbors = [(0, 6), (1, 5), (0, 4), (-1, 5)];
        for (int mask = 0; mask < 16; mask++)
        {
            for (int side = 0; side < 4; side++)
                roads.Build(neighbors[side].x, neighbors[side].z,
                    (mask & (1 << side)) != 0 ? TileKind.Road : TileKind.Bulldoze);
            check(roads.RoadMask(0, 5) == mask, $"Road artwork follows additions/removals across a negative chunk seam: {mask}");
            check(RoadRenderer.IsCurve(mask) == (mask is 3 or 6 or 9 or 12)
                && RoadRenderer.IsIntersection(mask) == (mask is 7 or 11 or 13 or 14 or 15),
                $"Road pattern selects straight, curve or intersection: {mask}");
        }

        var drag = new CameraDrag();
        var water = new City(false) { Money = 10_000_000 };
        water.Purchase(new(-1, 0));
        long beforeWater = water.Money;
        water.Build(2, 10, TileKind.Water);
        check(water[2, 10].Kind == TileKind.Water && water.Money == beforeWater - 280, "Water construction charges its listed price");
        check(water.LocalComfort(2, 11) == 10 && water.LocalComfort(2, 14) == 0, "Water improves comfort without road access, within three tiles");
        water.Build(3, 10, TileKind.Water); water.Build(2, 9, TileKind.Water);
        check(water.LocalComfort(2, 11) == 25, "Water comfort shares the existing positive cap");
        for (int x = -3; x <= 3; x++) water.Build(x, 12, TileKind.Road);
        for (int x = -2; x <= 2; x++)
        {
            water.Build(x, 11, TileKind.Water); water.Build(x, 13, TileKind.Water);
        }
        check(Enumerable.Range(-2, 5).All(x => water.BridgeAxis(x, 12) == 1), "Existing roads become a five-tile bridge across a chunk boundary");
        check(water.HasWater(0, 12) && water[-2, 12].Connected, "Water continues beneath bridges and roads retain service");
        water.Build(0, 11, TileKind.Bulldoze);
        check(water.BridgeAxis(0, 12) == 0 && water.BridgeAxis(-1, 12) == 1, "Removing water updates only affected bridges");
        water.Build(0, 11, TileKind.Water);
        water.Build(0, 12, TileKind.Bulldoze);
        check(water.BridgeAxis(-1, 12) == 0 && !water[-2, 12].Connected, "Removing a bridge disconnects roads and updates artwork");
        water.Build(0, 12, TileKind.Road);
        for (int z = 4; z <= 8; z++) water.Build(10, z, TileKind.Road);
        for (int z = 5; z <= 7; z++)
        {
            water.Build(9, z, TileKind.Water); water.Build(11, z, TileKind.Water);
        }
        check(Enumerable.Range(5, 3).All(z => water.BridgeAxis(10, z) == 2), "North/south long bridges use the perpendicular water pair");
        water.Build(9, 6, TileKind.Road);
        check(water[9, 6].Kind == TileKind.Road && water.BridgeAxis(10, 6) == 0, "Roads can replace water and a junction is not a bridge");
        water.Save(path);
        var waterLoaded = City.Load(path);
        check(waterLoaded[0, 11].Kind == TileKind.Water && waterLoaded.BridgeAxis(0, 12) == 1, "Water and automatic bridges survive save/load");
        water.SetActiveChunks([]); water.Tick();
        check(water.LastExpense > 0 && water.Chunks.Values.All(c => c.IsPacked), "Water supports maintenance and cold chunk compression");
        drag.Update(true, true, true, true, true);
        check(drag.Active && drag.Panning, "Shift at right-button press starts panning");
        drag.Update(true, false, true, true, false);
        check(drag.Panning, "Releasing Shift mid-drag does not switch to rotation");
        drag.Update(false, false, true, true, false);
        drag.Update(true, true, true, true, false);
        drag.Update(true, false, true, true, true);
        check(drag.Active && !drag.Panning, "Pressing Shift during rotation does not switch modes");
        drag.Update(true, false, false, true, true);
        check(!drag.Active, "Losing focus cancels the drag");
        drag.Update(true, true, true, false, true); drag.Update(true, false, true, true, true);
        check(!drag.Active, "Dragging from UI into the map does not capture the camera");
        var delta = CameraDrag.Pan(new Vector2(100, 0), 0, MathF.PI / 4, 30, 900);
        check(delta.X < 0 && Math.Abs(delta.Z) < .0001f, "Panning moves the ground in the mouse direction");
        var basin = new City(false) { Money = 10_000_000 };
        basin.Purchase(new(-1, 0));
        basin.Build(0, 5, TileKind.Water); basin.Build(-1, 5, TileKind.Water);
        var centerChunk = basin.Chunks[new(0, 0)];
        var westChunk = basin.Chunks[new(-1, 0)];
        var ground = TerrainGeometry.Build(basin, centerChunk);
        check(ground.Land.Sum(s => s.Length) == 575 && ground.Water[0].A.Y == -.16f,
            "Water removes exactly one land tile and sits below ground");
        var westGround = TerrainGeometry.Build(basin, westChunk);
        check(ground.Skirts.Count == 3 && westGround.Skirts.Count == 3,
            "Purchased chunk seams have no underground blocking walls");
        var shoreVertices = ground.Water.SelectMany(q => new[] { q.A, q.B, q.C, q.D }).ToArray();
        check(shoreVertices.Any(p => p.Y == 0) && shoreVertices.All(p => p.Y >= -.16f && p.Y <= 0),
            "Banks slope from ground level down to the water surface");
        long westRevision = westChunk.TerrainRevision;
        basin.Build(0, 5, TileKind.Bulldoze);
        check(centerChunk.TerrainRevision > 0 && westChunk.TerrainRevision > westRevision
            && TerrainGeometry.Build(basin, centerChunk).Land.Sum(s => s.Length) == 576,
            "Removing boundary water restores ground and invalidates the neighboring bank");
        for (int x = 0; x <= 2; x++) basin.Build(x, 12, TileKind.Road);
        basin.Build(1, 11, TileKind.Water); basin.Build(1, 13, TileKind.Water);
        check(TerrainGeometry.Build(basin, centerChunk).Land.Sum(s => s.Length) == 573,
            "Automatic bridges also remove the land beneath their decks");
        basin.Build(1, 11, TileKind.Bulldoze);
        check(TerrainGeometry.Build(basin, centerChunk).Land.Sum(s => s.Length) == 575,
            "Losing bridge conditions restores the ground beneath the road");
    }
}
