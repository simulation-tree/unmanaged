namespace Unmanaged
{
    /// <summary>
    /// Represents an object that can be serialized into and initialized from bytes.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Writes the state of the object into the given <paramref name="byteWriter"/>.
        /// </summary>
        void Write(ByteWriter byteWriter);

        /// <summary>
        /// Reads the data from the <paramref name="byteReader"/> into the object, 
        /// updating it's internal state to match.
        /// </summary>
        void Read(ByteReader byteReader);
    }
}