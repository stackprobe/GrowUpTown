using System.Runtime.CompilerServices;

namespace GrowUpTown;

internal static class StoragePaths
{
#if DEBUG
    // This source file lives beside GrowUpTown.sln, regardless of the build output folder.
    public static string OutputDirectory { get; } = Path.Combine(SourceDirectory(), "SaveData");

    private static string SourceDirectory([CallerFilePath] string sourcePath = "") =>
        Path.GetDirectoryName(sourcePath)!;
#else
    public static string OutputDirectory { get; } = AppContext.BaseDirectory;
#endif

    public static string ForFile(string fileName)
    {
        Directory.CreateDirectory(OutputDirectory);
        return Path.Combine(OutputDirectory, Path.GetFileName(fileName));
    }
}
