using System;
using System.Text;

public static class ASCIITextGenerator
{
    public const string TypeName = "ASCIIText";
    public static readonly int[] typeSizes = [8, 16, 32, 64, 128, 256, 512, 1024];

    public static string Generate(int typeSize)
    {
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
            source = source.Replace("{{ImplicitDowncastOperators}}", GetImplicitDowncastOperators(typeSize, 2));
            source = source.Replace("{{ExplicitUpcastOperators}}", GetExplicitUpcastOperators(typeSize, 2));
            return source;
        }
        else
        {
            return "Failed to find template";
        }
    }

    public static string GetImplicitDowncastOperators(int typeSize, int indentation)
    {
        int sizeIndex = Array.IndexOf(typeSizes, typeSize);
        StringBuilder builder = new();
        for (int i = sizeIndex - 1; i >= 0; i--)
        {
            int otherTypeSize = typeSizes[i];
            Indent();
            builder.Append("/// <inheritdoc/>");
            builder.AppendLine();

            Indent();
            builder.Append("public static implicit operator ");
            builder.Append(TypeName);
            builder.Append(otherTypeSize);
            builder.Append('(');
            builder.Append(TypeName);
            builder.Append(typeSize);
            builder.Append(" value)");
            builder.AppendLine();

            Indent();
            builder.Append('{');
            builder.AppendLine();
            {
                indentation++;

                Indent();
                builder.Append("Span<char> span = stackalloc char[value.Length];");
                builder.AppendLine();

                Indent();
                builder.Append("value.CopyTo(span);");
                builder.AppendLine();

                Indent();
                builder.Append("return new ");
                builder.Append(TypeName);
                builder.Append(otherTypeSize);
                builder.Append("(span);");
                builder.AppendLine();

                indentation--;
            }
            Indent();
            builder.Append('}');

            if (i > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        void Indent()
        {
            builder.Append(' ', indentation * 4);
        }

        return builder.ToString();
    }

    public static string GetExplicitUpcastOperators(int typeSize, int indentation)
    {
        int sizeIndex = Array.IndexOf(typeSizes, typeSize);
        StringBuilder builder = new();
        for (int i = sizeIndex + 1; i < typeSizes.Length; i++)
        {
            int otherTypeSize = typeSizes[i];
            Indent();
            builder.Append("/// <inheritdoc/>");
            builder.AppendLine();

            Indent();
            builder.Append("public static explicit operator ");
            builder.Append(TypeName);
            builder.Append(otherTypeSize);
            builder.Append('(');
            builder.Append(TypeName);
            builder.Append(typeSize);
            builder.Append(" value)");
            builder.AppendLine();

            Indent();
            builder.Append('{');
            builder.AppendLine();
            {
                indentation++;

                Indent();
                builder.Append("Span<char> span = stackalloc char[value.Length];");
                builder.AppendLine();

                Indent();
                builder.Append("value.CopyTo(span);");
                builder.AppendLine();

                Indent();
                builder.Append("return new ");
                builder.Append(TypeName);
                builder.Append(otherTypeSize);
                builder.Append("(span);");
                builder.AppendLine();

                indentation--;
            }
            Indent();
            builder.Append('}');

            if (i < typeSizes.Length - 1)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        void Indent()
        {
            builder.Append(' ', indentation * 4);
        }

        return builder.ToString();
    }
}