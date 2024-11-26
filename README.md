# Unmanaged
Library containing primitive definitions for working with native C#.

### Allocations
`Allocation`s are a reference to native memory, and they must be disposed manually.
The equivalent of `alloc` and `free`:
```cs
using (Allocation allocation = new(sizeof(char) * 5))
{
    allocation.Write("Hello".AsSpan());
    USpan<char> text = allocation.AsSpan<char>();
}

using (Allocation allocation = Allocation.Create(3.14f))
{
    ref float floatValue = ref allocation.Read<float>();
    floatValue *= 2;
}
```

### Fixed String
The `FixedString` value type can store up to 256 `char` values. Useful for when text is known
to be short enough until a list/array is needed:
```cs
FixedString text = new("Hello World");
USpan<char> textBuffer = stackalloc char[256];
uint length = text.CopyTo(textBuffer);

USpan<byte> utf8bytes = stackalloc char[256];
uint bytesCopied = text.CopyTo(utf8bytes);

FixedString textFromBytes = new(utf8bytes[..bytesCopied]);
Assert.That(textFromBytes.ToString, Is.EqualTo(Encoding.UTF8.GetString(textBuffer[..length])));
```

### Random Generator
Can generate random data and values using the XORshift technique:
```cs
using RandomGenerator random = new();
int fairDiceRoll = random.NextInt(0, 6);
```

### Safety checks
When compiling with `#DEBUG` or `#TRACK` flag set, all allocations originating from `Allocations` or
`Allocation` will be tracked. Providing dispose and access out of bounds checks, in addition to a 
final check when the current domain exits and there are still allocations present (leaks).

> It's the programmers responsibility and decision for when, and how allocations should be disposed.

### Contributing and direction
This library is made to provide the building blocks that a `System` namespace might,
but exclusively through and for native code. This is to minimize runtime cost and to expose
efficiency that was always available with C#. Commonly putting the author in a position where they
need to exercise more control, because _with great power comes great responsibility_.

Contributions that fit this are welcome.
