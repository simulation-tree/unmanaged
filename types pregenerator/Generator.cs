public static class Generator
{
    public static string GenerateASCIIText(int typeSize)
    {
        const string TypeName = "ASCIIText";
        if (EmbeddedResources.TryGet($"{TypeName}.txt", out string? source))
        {
            int lengthTypeSize;
            string lengthType;
            if (typeSize <= 256)
            {
                lengthTypeSize = 1;
                lengthType = "byte";
            }
            else
            {
                lengthTypeSize = 2;
                lengthType = "ushort";
            }

            int capacity = typeSize - lengthTypeSize;
            source = source.Replace("{{TypeName}}", TypeName + typeSize);
            source = source.Replace("{{TypeSize}}", typeSize.ToString());
            source = source.Replace("{{Capacity}}", capacity.ToString());
            source = source.Replace("{{LengthType}}", lengthType);
            return source;
        }
        else
        {
            return "Failed to find template";
        }
    }
}