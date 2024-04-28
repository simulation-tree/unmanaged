
# Unmanaged
Library containing some commonly used definitions, implemented using unsafe code.

### Safety and Allocations
The `Allocation` type acts as the root component that other types use, it implements handling of `null`
and disposed references, but only when compiling with the `#DEBUG` compiler flag. They differ from regular
class objects in that they need to be disposed manually and aren't watched by the garbage collector:
```cs
using Allocation allocation = new(sizeof(uint));
Span<uint> span = allocation.AsSpan<uint>();
span[0] = 5;
ref uint value = ref allocation.AsRef<uint>();
Span<byte> allocationBytes = allocation.AsSpan<byte>();
```

If allocations are still present when the current domain exits (app closes), an exception will be thrown
to notify of all the allocations that weren't disposed, and from where they were created.

### Collections
A few collection types are available:
- Lists
- Arrays
- Dictionaries (wip)
- Hash sets (wip)
- Linked lists (wip)
```cs
using UnmanagedList<int> list = new();
list.Add(5);
Span<int> listSpan = list.AsSpan();
```

### Runtime Type
This is an unmanaged compatible type that replaces `System.Type`, but it requires that the type it's
representing is also an unmanaged type:
```cs
RuntimeType type = RuntimeType.Get<int>();
string fullName = type.Type.FullName;
```

### Containers
Similar to the `Allocation` type, but with a type associated with it in order to safely contain
a value of that type:
```cs
using Container myFloat = Container.Create(5f);
RuntimeType type = myFloat.type;
float floatValue = myFloat.As<float>();
```

The equality operation between two containers, compare the bytes of the two values rather than the
address of the pointer like with `Allocation` types.

### Fixed String
A common scenario in C# with unsafe code is the inability to store a `string` inside structure.
This type mimics a string of fixed length, but it only can contain up to 290 characters each 7-bit (ASCII):
```cs
FixedString str = new("Hello World");
Span<char> strSpan = stackalloc char[str.Length];
str.CopyTo(strSpan);
```

These can't be marshalled or treated as UTF8 strings, so they must be copied into span buffers when
the more common `string` type is needed.

### Random Generator
An object that can generate random data using the XORshift technique:
```cs
using RandomGenerator random = new();
int value = random.NextInt();
```

### Contributing and Direction
This library is developed as a module to provide the fundamental pieces that `System` would
for other projects, but with unsafe code. Commonly putting the user in a position where they
need to excerise more absolute control over their data, at the benefit of efficiency.

Contributions to this are welcome.