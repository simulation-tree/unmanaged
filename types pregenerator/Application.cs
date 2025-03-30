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
                for (int t = 0; t < ASCIITextGenerator.typeSizes.Length; t++)
                {
                    int typeSize = ASCIITextGenerator.typeSizes[t];
                    string fileName = $"ASCIIText{typeSize}.cs";
                    File.WriteAllText(fileName, ASCIITextGenerator.Generate(typeSize));
                }
            }
            else if (args[i] == "--generate-value-arrays")
            {
                for (int t = ValueArrayGenerator.minSize; t <= ValueArrayGenerator.maxSize; t++)
                {
                    string fileName = $"ValueArray{t}.cs";
                    File.WriteAllText(fileName, ValueArrayGenerator.Generate(t));
                }
            }
        }
    }

    public readonly void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}