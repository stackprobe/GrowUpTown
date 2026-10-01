namespace GrowUpTown;

internal static class LandPriceTests
{
    internal static void Run(Action<bool, string> check, string path)
    {
        var city = City.NewEmpty();
        long[] prices = [1_000_000, 1_500_000, 2_000_000, 2_500_000];
        for (int i = 0; i < prices.Length; i++)
        {
            long price = prices[i]; var land = new ChunkPos(i + 1, 0);
            check(city.LandPrice == price, $"Land purchase {i + 1} has the expected increasing price");
            city.Money = price - 1;
            string failure = city.Purchase(land);
            check(city.Chunks.Count == i + 1 && city.Money == price - 1 && city.LandPrice == price
                && failure.Contains($"¥{price:N0}"), "Insufficient funds preserve land, money and price and report current cost");
            city.Money = price;
            city.Purchase(new(0, 0)); city.Purchase(new(100, 100));
            check(city.LandPrice == price && city.Money == price, "Duplicate and nonadjacent purchases do not raise the price");
            string success = city.Purchase(land);
            check(city.Money == 0 && city.Chunks.Count == i + 2 && city.LandPrice == price + 500_000
                && success.Contains($"¥{price:N0}"), "Successful purchase charges and reports the old price then raises the next price");
            city.Save(path); city = City.Load(path);
            check(city.LandPrice == price + 500_000, "Next land price survives save/load using existing district data");
        }
        check(new City().LandPrice == 1_000_000, "New town resets the land purchase price");
    }
}
