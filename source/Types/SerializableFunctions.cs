namespace Unmanaged
{
    /// <summary>
    /// Extension functions for <see cref="ISerializable"/> objects.
    /// </summary>
    public static class SerializableFunctions
    {
        /// <summary>
        /// Clones the given value by serializing it to bytes, and deserializing
        /// into a new instance.
        /// </summary>
        public unsafe static T Clone<T>(this T self) where T : unmanaged, ISerializable
        {
            using BinaryWriter writer = new(4);
            self.Write(writer);
            using BinaryReader reader = new(writer);
            return reader.ReadObject<T>();
        }
    }
}