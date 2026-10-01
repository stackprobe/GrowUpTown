namespace GrowUpTown;

internal static class TownNeedsTests
{
    internal static void Run(Action<bool, string> check)
    {
        var advice = TownNeeds.Evaluate(0, 0, 0, 70, 0);
        check(advice[0].Attention && advice[1].Attention && advice[1].Title == "職場がありません",
            "Empty towns request both housing and jobs");
        advice = TownNeeds.Evaluate(23520, 23664, 24032, 100, 0);
        check(advice[0].Detail.Contains("369") && advice[1].Detail.Contains("282")
            && advice[1].Action.Contains("通常住宅"),
            "Housing advice reproduces the reported factory growth shortage exactly");
        advice = TownNeeds.Evaluate(23520, 25000, 24032, 100, 0);
        check(!advice[0].Attention && advice[1].Action.Contains("人口増加を待つ"),
            "Worker shortage with spare housing recommends waiting for residents");
        advice = TownNeeds.Evaluate(100, 120, 40, 70, 0);
        check(advice[1].Title == "職場が不足しています" && advice[1].Detail.Contains("15"),
            "Unemployment requests additional commercial or industrial jobs");
        advice = TownNeeds.Evaluate(100, 100, 80, 70, 0);
        check(advice[0].Attention && advice[0].Detail.Contains("0人") && !advice[1].Attention,
            "Full housing with adequate jobs requests more homes");
        advice = TownNeeds.Evaluate(99, 200, 100, 70, 0);
        check(advice[1].Attention, "One worker below the business threshold reports a shortage");
        advice = TownNeeds.Evaluate(100, 200, 100, 60, 0);
        check(!advice[1].Attention && !advice[2].Attention, "Exact employment and happiness thresholds are satisfied");
        advice = TownNeeds.Evaluate(100, 200, 100, 59, 0);
        check(advice[2].Attention && advice[2].Title.Contains("幸福度"), "Low happiness shows actionable growth advice");

        var city = SelfTests.DevelopedCity();
        city.Build(2, 2, TileKind.SuperMixed);
        city.SetActiveChunks([]);
        check(city.Chunks[new(0, 0)].IsPacked, "Advice fixture has packed offscreen buildings");
        advice = TownNeeds.Evaluate(city);
        check(advice[2].Title.Contains("道路") && advice[2].Detail.Contains("1棟")
            && city.Chunks[new(0, 0)].IsPacked,
            "Disconnected 2x2 buildings count once without unpacking them for the HUD");
        city.Build(3, 3, TileKind.Bulldoze);
        check(!TownNeeds.Evaluate(city)[2].Attention, "Advice refreshes after construction and demolition");
    }
}
