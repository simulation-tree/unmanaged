
# Unmanaged
Library containing unmanaged objects and definitions implemented using unsafe code.

### Safety
Handling of `null` references is already done by the types in this library when compiling with
the `#DEBUG` compiler flag. If compiling with release settings, these checks are dropped,
good to check for red flags ahead of time in case there is code that can leak state or drift it.

The `Allocations` class is used to track pointers, and also for throwing when something bad happens:
```cs
public readonly void Dispose()
{
    Allocations.ThrowIfNull(pointer);
    Marshal.FreeHGlobal(pointer);
    Allocations.Unregister(pointer);
}
```

### Collections
Only basic arrays and lists are available, as `UnmanagedList<T>` and `UnmanagedArray<T>`:
```cs
using UnmanagedList<int> list = new();
list.Add(5);
Span<int> listSpan = list.AsSpan();
```

### Runtime Type
A value type that can be used inside other value types, intended to be unmanaged as well.
Their determinism is tied to the name of the type, so if name is fixed then hash is also fixed.
A limitation is that they can't be created for class types, and can't be created from a `Type` object
either, only through the generic `Get<T>()` method.
```cs
RuntimeType type = RuntimeType.Get<int>();
string fullName = type.Type.FullName;
```

### Containers
For storing an arbitrary object with the type known:
```cs
using Container myFloat = Container.Create(5f);
RuntimeType type = myFloat.type;
float floatValue = myFloat.As<float>();
```

### Fixed String
A value type that represents a string of fixed length. It can contain up to 290 characters,
each 7-bit, all inside 256 bytes of memory:
```cs
FixedString str = new("Hello World");
Span<char> strSpan = stackalloc char[str.Length];
str.CopyTo(strSpan);
```

### Random Generator
An object that can generate random data using the XORshift technique:
```cs
using RandomGenerator random = new();
int value = random.NextInt();
```

### Unmanaged Buffer
This is used by the list and array types in order to represent a region of memory that
behaves like a sequence, where it can be indexed and has a fixed size:
```cs
string str = "Hello World";
using UnsafeBuffer buffer = new(sizeof(char), str.Length);
Span<char> bufferSpan = buffer.AsSpan<char>();
str.CopyTo(bufferSpan);
Span<byte> bufferBytes = buffer.AsSpan<byte>();
```
