# Unmanaged

[![Test](https://github.com/game-simulations/unmanaged/actions/workflows/test.yml/badge.svg)](https://github.com/game-simulations/unmanaged/actions/workflows/test.yml)

Library containing primitives for working with native C#.

### Allocations

`Allocation`s are a reference to heap memory, and they must be manually disposed.
The equivalent of `alloc` and `free`:
```cs
//an allocation that is 10 bytes in size
using (Allocation allocation = new(sizeof(char) * 5))
{
    allocation.Write("Hello".AsSpan());
    USpan<char> text = allocation.AsSpan<char>();
}

//an allocation containing a float
using (Allocation allocation = Allocation.Create(3.14f))
{
    ref float floatValue = ref allocation.Read<float>();
    floatValue *= 2;
}
```

### Fixed String

The `FixedString` type can store up to 255 `char` values. Useful for when text is known
to be short enough until a list/array is needed:
```cs
FixedString text = new("Hello World");
USpan<char> textBuffer = stackalloc char[FixedString.Capacity];
uint length = text.CopyTo(textBuffer);

//get utf8 bytes from the text
USpan<byte> utf8bytes = stackalloc char[FixedString.Capacity];
uint bytesCopied = text.CopyTo(utf8bytes);

//get text from utf8 bytes using System.Text.Encoding
FixedString textFromBytes = new(utf8bytes.Slice(0, bytesCopied));
Assert.That(textFromBytes.ToString, Is.EqualTo(Encoding.UTF8.GetString(textBuffer.Slice(0, length))));
```

### Text

Accompanying the above is the disposable `Text` type. Which is a reference to an arbitrary amount
of `char`s, and behaves like a `string`:
```cs
using Text builder = new();
builder.Append("Hello");
builder.Append(" there");

USpan<char> text = builder.AsSpan();
Console.WriteLine(text.ToString());
```

### Random Generator

Can generate random data and values using the XORshift technique:
```cs
using RandomGenerator random = new();
int fairDiceRoll = random.NextInt(0, 6);
```

### Optional checks

When compiling with `#DEBUG` or `#TRACK` flag set, all allocations originating from `Allocations` or
`Allocation` will be tracked. Providing disposed and access out of bounds checks. In addition to a 
final check when the app domain exits, and there are still allocations present (aka leaks).

Because these checks are an additional branch at runtime, they are made optional for efficiency. It's
the programmers responsibility and decision for when, and how allocations should be disposed.

### Included `default` analyzer

Included is an analyzer that emits errors where a disposable struct type is being created and
initialized to `default`. Because disposable types imply they must be disposed, and so must have a 
way to properly initialize them:
```cs
Allocation allocation = default; //U0001 error
```

There is no analysis for `new()` however. Because a default constructor can be used on purpose,
but if not, they can be declared with an `[Obsolete]` attribute:
```cs
SomeData someData = new(); //CS0619 error

public readonly struct SomeData : IDisposable
{
    private readonly Allocation allocation;

    public readonly ref int Value => ref allocation.Read<int>();

    [Obsolete("Default constructor not supported", true)] //disallows default constructor
    public SomeData() 
    {
        throw new NotSupportedException();
    }

    public SomeData(int value)
    {
        allocation = Allocation.Create(value);
    }

    public readonly void Dispose()
    {
        allocation.Dispose();
    }
}
```

### Contributing and direction

This library is made to provide building blocks for working with native code in C#.
For minimizing runtime cost and to expose the efficiency that was always there.
Commonly putting the author in a position where they need to exercise more control.

> _with great power, comes great responsibility_

Contributions that fit this are welcome.