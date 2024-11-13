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
        void Write(BinaryWriter writer);

        /// <summary>
        /// Reads the data from the reader into the object, updating
        /// it's internal state to match.
        /// </summary>
        void Read(BinaryReader reader);
    }
}