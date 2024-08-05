# Unmanaged
Library containing primitive programming patterns for C# implemented exclusively with unmanaged code.

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
A type that replaces `System.Type`, and only for unmanaged type definitions:
```cs
RuntimeType type = RuntimeType.Get<uint>();
Type systemType = type.Type;
```

### Containers
These are for containing a value, together with the type that it is:
```cs
using Container myFloat = Container.Create(5f);
RuntimeType type = myFloat.type;
float floatValue = myFloat.As<float>();
```

The equality operation between two containers, compare the bytes of the two values rather than the
address of the pointer like with `Allocation` types. That is value equality, to instead perform reference
equality, the address behind the container should be manually compared.

### Fixed String
A common scenario in C# with having types meant for unsafe code, is the inability to contain a `string`.
The `FixedString` type mimics a string, but of fixed length and it can only contain up to 290 characters, each 7-bit (ASCII):
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

### Allocations
The `Allocation` type acts as a pointer to memory, that must be disposed:
```cs
Allocation allocation = new(sizeof(uint) * 4);
Span<uint> span = allocation.AsSpan<uint>();
Span<byte> allocationBytes = allocation.AsSpan<byte>();
allocation.Dispose();
```
These can also be created with the `Allocations` static class as well, in the form of unsafe pointers:
```cs
public struct Player 
{
    public int health;
    public Allocation inventory;
}

Player* player = Allocations.Allocate<Player>();
player->health = 100;
player->inventory = new(sizeof(uint) * 10);
Span<uint> inventorySpan = player->inventory.AsSpan<uint>();

player->inventory.Dispose();
Allocations.Free(player);
```

### Safety
When compiling with a debug profile, all allocations originating from the `Allocations`
class or the `Allocation` type have their addresses tracked. And exceptions can
be thrown when attempting to access addresses belonging to freed/unallocated memory.

These thrown exceptions will also contain stack traces, at the cost of runtime efficiency.
Use the `#IGNORE_STACKTRACES` flag to disable this (is already disabled for non debug builds).

When compiling with a release profile, these checks are dropped. The executing program
is instead, expected to be able to perfectly maintain its state indefineitly. Allocation
tracking can be reenabled with the `#TRACK_ALLOCATIONS` flag (only in release builds).

### Final leak guard
`Allocations.ThrowIfAny()` will be called when the current AppDomain exits (when program ends).
This will check if there are any allocations that have not been freed, and throw an exception if so.

The `Allocations.Finish` delegate can be subscribed to insert clean up code before the program ends.

### Contributing and direction
This library is developed to provide fundamental pieces that a `System` namespace would
for other projects, but using unmanaged code to minimize runtime cost. Commonly putting the user in a position
where they need to excerise more manual control over their data, at the benefit of efficiency.

Contributions to this are welcome.
