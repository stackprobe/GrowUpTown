using GrowUpTown;

if (args.Contains("--self-test")) return SelfTests.Run();
try
{
    using var game = new Game();
    game.Run(args.Contains("--smoke-test"));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
