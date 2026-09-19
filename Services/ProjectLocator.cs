namespace WinUIXamlDesigner.Services;

public static class ProjectLocator
{
    public static string? FindCodeBehind(string xamlPath)
    {
        var directory = Path.GetDirectoryName(xamlPath);
        if (directory is null) return null;

        var baseName = Path.GetFileNameWithoutExtension(xamlPath);
        var direct = Path.Combine(directory, baseName + ".xaml.cs");
        if (File.Exists(direct)) return direct;

        return Directory.EnumerateFiles(directory, "*.xaml.cs", SearchOption.TopDirectoryOnly)
            .Where(p => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(p))
                .Equals(baseName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    public static IReadOnlyList<string> FindXamlFiles(string projectFile)
    {
        var directory = Path.GetDirectoryName(projectFile);
        if (directory is null || !Directory.Exists(directory)) return Array.Empty<string>();
        return Directory.EnumerateFiles(directory, "*.xaml", SearchOption.AllDirectories)
            .Where(p => !Path.GetFileName(p).Equals("App.xaml", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
