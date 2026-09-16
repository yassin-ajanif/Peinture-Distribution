namespace GestionCommerciale.Shared.Database;

public static class DatabasePath
{
    private const string AppFolderName = "DistributionPeinture";
    private const string LegacyAppFolderName = "GestionCommerciale";
    private const string DbFileName = "data.db";

    public static string GetDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);
        Directory.CreateDirectory(dir);
        MigrateFromLegacyFolder(dir);
        return dir;
    }

    public static string GetConnectionString()
    {
        var dbPath = Path.Combine(GetDirectory(), DbFileName);
        return $"Data Source={dbPath}";
    }

    private static void MigrateFromLegacyFolder(string newDir)
    {
        var newDb = Path.Combine(newDir, DbFileName);
        if (File.Exists(newDb))
            return;

        var legacyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LegacyAppFolderName);
        if (!Directory.Exists(legacyDir))
            return;

        foreach (var file in Directory.EnumerateFiles(legacyDir))
        {
            var dest = Path.Combine(newDir, Path.GetFileName(file));
            if (!File.Exists(dest))
                File.Copy(file, dest);
        }
    }
}
