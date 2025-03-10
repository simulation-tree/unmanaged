using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

public static class EmbeddedResources
{
    public static bool TryGet(string path, [NotNullWhen(true)] out string? source)
    {
        Assembly assembly = typeof(EmbeddedResources).Assembly;
        string[] names = assembly.GetManifestResourceNames();
        foreach (string name in names)
        {
            if (name.EndsWith(path))
            {
                Stream stream = assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException($"Resource not found: {path}");
                using StreamReader reader = new(stream);
                source = reader.ReadToEnd();
                stream.Dispose();
                return true;
            }
        }

        source = null;
        return false;
    }
}