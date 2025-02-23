namespace Unmanaged
{
    /// <summary>
    /// Extension functions for <see cref="ISerializable"/> objects.
    /// </summary>
    public static class SerializableExtensions
    {
        /// <summary>
        /// Clones the given value by serializing it to bytes, and deserializing
        /// into a new instance.
        /// </summary>
        public unsafe static T Clone<T>(this T self) where T : unmanaged, ISerializable
        {
            using ByteWriter writer = new((uint)sizeof(T));
            self.Write(writer);
            using ByteReader reader = new(writer);
            return reader.ReadObject<T>();
        }
    }
}