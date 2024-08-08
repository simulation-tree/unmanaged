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
`Allocation`s are a reference to unmanaged memory, and they must be disposed manually.
```cs
using Allocation allocation = new(sizeof(char) * 5);
allocation.Write("Hello".AsSpan());
Span<char> text = allocation.AsSpan<char>();
```

`Container`s extend further, and reference both the memory and their intended type.
```cs
using Container floatContainer = Container.Create(5f);
RuntimeType type = floatContainer.type;
float floatValue = floatContainer.Read<float>();
Assert.Throws(floatContainer.Read<int>()); //type mismatch
```

> The equality operation between two containers are unlike the one for allocations.
Allocations check for address equality, while containers check for memory equality.

### Fixed String
The `FixedString` type can store up to 291 UTF8 characters until a terminator.
Useful for storing short text without heap allocations:
```cs
FixedString text = new("Hello World");
Span<char> text = stackalloc char[text.Length];
text.CopyTo(strSpan);

Span<byte> utf8bytes = stackalloc char[16];
int bytesCopied = text.CopyTo(utf8bytes);

FixedString textFromBytes = new(utf8bytes[..bytesCopied]);
```

### Random Generator
Can generate random data and values using the XORshift technique:
```cs
using RandomGenerator random = new();
int fairDiceRoll = random.NextInt(0, 6);
```

### Safety checks
When compiling with a debug profile (where `DEBUG` flag is set), all allocations originating
from `Allocations` or `Allocation`, will have their stacktraces tracked. This is useful for
investigating where the offending leaks are coming from.

When compiling with a release profile, all checks are dropped. The executing program is
expected to be able to maintain its state on its own. Tracing can be re-enabled in release
builds with the `TRACK` flag.

> Because release builds have all safety checks dropped, it's the users responsibility
and choice for how allocations are freed (automagically or manually). Considering
relying on the `using`+`IDisposable` combination throughout your types.

### Memory alignment
All allocations are unaligned by default. This can be toggled with the `ALIGNED` flag.

### Contributing and direction
This library is developed to provide the building blocks that a `System` namespace might, but only through unmanaged code. To minimize runtime cost and to expose more
efficiency that was always there. Commonly putting the author in a position where they need to excerise more control, because _with great power comes great responsibility_.

Contributions that fit this are welcome.