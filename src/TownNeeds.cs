namespace GrowUpTown;

internal readonly record struct TownAdvice(string Title, string Detail, string Action, bool Attention);

internal static class TownNeeds
{
    internal static TownAdvice[] Evaluate(City city) => Evaluate(city.Population, city.Capacity, city.Jobs,
        city.Happiness, city.Chunks.Values.Sum(c => c.Stats.DisconnectedBuildings));

    // Uses cached chunk totals, so the HUD never unpacks offscreen buildings.
    internal static TownAdvice[] Evaluate(long population, long capacity, long jobs, int happiness, int disconnected)
    {
        long workers = population * 55 / 100;
        long vacancies = Math.Max(0, capacity - population);
        long neededWorkers = (jobs * 55 + 99) / 100;
        long neededPopulation = (neededWorkers * 100 + 54) / 55;
        bool workerShortage = workers < neededWorkers;
        TownAdvice housing;
        if (capacity == 0)
            housing = new("住宅がありません", "入居できる住宅の定員が0人です", "道路につながる住宅を建てましょう", true);
        else if (workerShortage && capacity < neededPopulation)
            housing = new("住宅が不足しています", $"職場の発展に定員あと{neededPopulation - capacity:N0}人分", "通常住宅を増やしましょう", true);
        else if (workerShortage)
            housing = new("住宅には空きがあります", $"住宅の空き：{vacancies:N0}人分", "今の住宅定員で職場の発展が可能", false);
        else if (vacancies <= Math.Max(4, capacity / 10))
            housing = new("住宅の空きが少ないです", $"住宅の空き：あと{vacancies:N0}人分", "人口を増やすなら住宅を追加", true);
        else
            housing = new("住宅には空きがあります", $"住宅の空き：{vacancies:N0}人分", "新しい住民が入居できます", false);

        TownAdvice work;
        if (jobs == 0)
            work = new("職場がありません", "道路につながる雇用枠が0人です", "商業地か工業地を建てましょう", true);
        else if (workers > jobs)
            work = new("職場が不足しています", $"仕事のない労働者：{workers - jobs:N0}人", "商業地か工業地を増やしましょう", true);
        else if (workerShortage)
            work = new("労働者が不足しています", $"職場の発展にあと{neededWorkers - workers:N0}人", capacity < neededPopulation
                ? "職場の追加より通常住宅を優先" : "住宅の空きあり・人口増加を待つ", true);
        else
            work = new("職場は足りています", $"雇用枠 {jobs:N0} / 労働者 {workers:N0}", "商業・工業の労働者条件は達成", false);

        TownAdvice growth;
        if (disconnected > 0)
            growth = new("道路の接続が必要です", $"道路未接続の建物：{disconnected:N0}棟", "入口 [0, 12] へ道路をつなぐ", true);
        else if (happiness < 60)
            growth = new("幸福度を上げましょう", $"現在{happiness}% / 発展には60%以上", "税率・公園・住宅付近の工場を確認", true);
        else
            growth = new("幸福度は良好です", $"現在{happiness}% / 発展の基準60%以上", "建物は条件を6か月続けると成長", false);
        return [housing, work, growth];
    }
}
