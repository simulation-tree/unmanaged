namespace Unmanaged
{
    public interface IBinaryObject
    {
        /// <summary>
        /// Serializes the object into the writer.
        /// </summary>
        void Write(BinaryWriter writer);

        /// <summary>
        /// Deserializes the object from it's <c>default</c> state, with
        /// the data in the reader.
        /// </summary>
        void Read(ref BinaryReader reader);
    }
}