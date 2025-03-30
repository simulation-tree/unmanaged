public static class ValueArrayGenerator
{
    public static readonly int minSize = 2;
    public static readonly int maxSize = 2048;
    private const string Template = 
@"using System.Runtime.CompilerServices;

namespace Unmanaged
{ 
    /// <summary>
    /// Array of {{TypeSize}} values.
    /// </summary>
    public struct ValueArray{{TypeSize}}<T> where T : unmanaged
    {
        private T element0;
    }
}";

    public static string Generate(int typeSize)
    {
        string source = Template;
        source = source.Replace("{{TypeSize}}", typeSize.ToString());
        return source;
    }
}