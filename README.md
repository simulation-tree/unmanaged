# Unmanaged
Library containing primitive definitions for working with unmanaged C#.

### Types
- `UnmanagedList<T>`
- `UnmanagedArray<T>`
- `UnmanagedDictionary<K, V>`
- `RuntimeType`
- `Allocation`
- `Container`

### Allocations and Containers
`Allocation`s are a reference to unmanaged memory, and they must be disposed manually. The equivalent of `alloc` and `free`.
```cs
using (Allocation allocation = new(sizeof(char) * 5))
{
    allocation.Write("Hello".AsSpan());
    Span<char> text = allocation.AsSpan<char>();
}

using (Allocation allocation = Allocation.Create(3.14f))
{
    ref float floatValue = ref allocation.Read<float>();
    floatValue *= 2;
}
```

`Container`s extend a bit further by being aware of the type they store.
```cs
using (Container floatContainer = Container.Create(3.14f))
{
    RuntimeType type = floatContainer.type;
    ref float floatValue = ref floatContainer.Read<float>();
    Assert.Throws(floatContainer.Read<int>()); //type not the same
}
```

> The equality condition between two containers is different from allocations.
Allocations check for address equality, while containers check for memory equality.

### Fixed String
The `FixedString` type can store up to 291 UTF8 characters within its 256 bytes of space, 
until a `\0` terminator. Useful for when text is known to be short enough, until a list/array is needed:
```cs
FixedString text = new("Hello World");
Span<char> textBuffer = stackalloc char[FixedString.MaxLength];
int length = text.CopyTo(textBuffer);

Span<byte> utf8bytes = stackalloc char[256];
int bytesCopied = text.CopyTo(utf8bytes);

FixedString textFromBytes = new(utf8bytes[..bytesCopied]);
Assert.That(textFromBytes.ToString, Is.EqualTo(Encoding.UTF8.GetString(textBuffer[..length])));
```

### Random Generator
Can generate random data and values using the XORshift technique:
```cs
using RandomGenerator random = new();
int fairDiceRoll = random.NextInt(0, 6);
```

### Runtime Type
The `RuntimeType` itself is 4 bytes big, and it stores the hash built from the type's full name
with its size embedded:
```cs
RuntimeType intType = RuntimeType.Get<byte>();
RuntimeType floatType = RuntimeType.Get<float>();
Assert.That(intType.Size, Is.EqualTo(sizeof(byte)));
Assert.That(intType == floatType, Is.False);
```

### Safety checks
When compiling without release settings (where a `#DEBUG` flag is set), all allocations
originating from `Allocations` or `Allocation` will be tracked. This is so that, when debugging
is finished and the program exists, an exception can be thrown if there are any allocations
that would leak in a release build.

With release settings, all checks are dropped. The executing program is expected to dispose all
of the allocations it has made. The `#TRACK` flag is used to re-enabled allocation tracking,
at the cost of performance.

> It's the program's responsibility, and choice for when and how allocations are disposed.

### Memory alignment
Allocations are not aligned by default, this can be toggled with the `#ALIGNED` flag.

### Contributing and direction
This library is developed to provide the building blocks that a `System` namespace might,
but exclusively through unmanaged code. In order to minimize runtime cost and to expose more
efficiency that was always there with C#. Commonly putting the author in a position where they
need to excerise more control, because _with great power comes great responsibility_.

Contributions that fit this are welcome.