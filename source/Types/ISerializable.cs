using Unmanaged;

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
