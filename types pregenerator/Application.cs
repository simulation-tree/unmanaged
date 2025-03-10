using System;
using System.IO;

public readonly struct Application : IDisposable
{
    private readonly string[] args;

    public Application(string[] args)
    {
        this.args = args;
    }

    public readonly void Run()
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--generate-ascii-text")
            {
                for (int t = 0; t < Generator.typeSizes.Length; t++)
                {
                    int typeSize = Generator.typeSizes[t];
                    File.WriteAllText($"ASCIIText{typeSize}.cs", Generator.GenerateASCIIText(typeSize));
                }
            }
        }
    }

    public readonly void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}