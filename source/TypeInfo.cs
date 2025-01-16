namespace Unmanaged
{
    /// <summary>
    /// Contains static information about a type.
    /// </summary>
    public unsafe static class TypeInfo<T> where T : unmanaged
    {
        //todo: seems uncessary, it saves the (uint) cast
        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly static uint size = (uint)sizeof(T);
    }
}
