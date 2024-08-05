# Unmanaged
Library containing primitive programming patterns for C# implemented exclusively with unmanaged code.

### Types
- Lists - `UnmanagedList<T>`
- Arrays - `UnmanagedArray<T>`
- Dictionaries (wip) - `UnmanagedDictionary<K, V>`
- Hash sets (wip)
- Linked lists (wip)

### Runtime Type
A type that replaces `System.Type`. Only for unmanaged type definitions:
```cs
RuntimeType type = RuntimeType.Get<uint>();
int hash = type.GetHashCode();
```

### Containers
Containers contain the bytes of the value, and the type that it is:
```cs
using Container myFloat = Container.Create(5f);
RuntimeType type = myFloat.type;
float floatValue = myFloat.As<float>();
Assert.Throws(myFloat.As<int>());
```

The equality operation between two containers, compare the bytes of the two values rather than the
address of the pointer like with `Allocation` types. This achieves value equality. To perform reference
equality instead, compare the addresses manually or use the `Allocation` type if possible.

### Fixed String
The `FixedString` type mimics a `string`. It can store up to 291 characters, where each character is 7 bits and terminated
by a terminator character. It replaces the need to use `Encoding.UTF8` to achieve text<->utf8 bytes serialization:
```cs
FixedString text = new("Hello World");
Span<char> text = stackalloc char[text.Length];
text.CopyTo(strSpan);

Span<byte> utf8bytes = stackalloc char[16];
int bytesCopied = text.CopyTo(utf8bytes);

FixedString textFromBytes = new(utf8bytes[..bytesCopied]);
```

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
`Allocations.ThrowIfAny()` will be called when the AppDomain exits. This will check if there are
any allocations remaining that haven't been freed, to throw an exception if so.

The `Allocations.Finish` delegate is called before this check is performed, allowing the user to
insert automatic clean up code.

### Contributing and direction
This library is developed to provide fundamental pieces that a `System` namespace would
for other projects, but using unmanaged code to minimize runtime cost. Commonly putting the user in a position
where they need to excerise more manual control over their data, at the benefit of efficiency.

Contributions to this are welcome.
