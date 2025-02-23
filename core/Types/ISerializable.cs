namespace Unmanaged
{
    /// <summary>
    /// Represents an object that can be serialized into and initialized from bytes.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Writes the state of the object into the given writer.
        /// </summary>
        void Write(ByteWriter writer);

        /// <summary>
        /// Reads the data from the reader into the object, updating
        /// it's internal state to match.
        /// <para>
        /// Target is uninitialized and <see langword="default"/> from the beginning.
        /// </para>
        /// </summary>
        void Read(ByteReader reader);
    }
}