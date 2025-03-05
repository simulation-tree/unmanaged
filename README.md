# Unmanaged

[![Test](https://github.com/game-simulations/unmanaged/actions/workflows/test.yml/badge.svg)](https://github.com/game-simulations/unmanaged/actions/workflows/test.yml)

Library containing primitives for working with native C#.

### Memory Addresses

`MemoryAddress` instances can point to either heap or stack memory. They can be
allocated using static methods, in which case they must also be disposed:
```cs
//an allocation that is 10 bytes in size
using (MemoryAddress allocation = MemoryAddress.Allocate(sizeof(char) * 5))
{
    allocation.Write("Hello".AsSpan());
    USpan<char> text = allocation.AsSpan<char>();
}

//an allocation containing a float
using (MemoryAddress allocation = MemoryAddress.Create(3.14f))
{
    ref float floatValue = ref allocation.Read<float>();
    floatValue *= 2;
}
```

### ASCII Text

The `ASCIIText256` type can store up to 255 extended ASCII `char` values. 
Useful for when text is known to be short, until a list/array is needed:
```cs
ASCIIText256 text = new("Hello there");
USpan<char> textBuffer = stackalloc char[text.Length];
text.CopyTo(textBuffer);

//get utf8 bytes from the text
USpan<byte> utf8bytes = stackalloc char[ASCIIText256.Capacity];
uint bytesCopied = text.CopyTo(utf8bytes);

//get text from utf8 bytes using System.Text.Encoding
ASCIIText256 textFromBytes = new(utf8bytes.Slice(0, bytesCopied));
string systemString = Encoding.UTF8.GetString(textBuffer.Slice(0, length));
Assert.That(textFromBytes.ToString, Is.EqualTo(systemString));
```

### Text

Accompanying the above is the disposable `Text` type, which is a reference to an arbitrary amount of `char`s values.
And behaves like a `string`:
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

### Safety

There are no safe guards when working with `MemoryAddress` values. As they
can represent any pointer, and can originate from anywhere. Including the stack.

It is the programmers responsibility for how memory should be managed. Including
when created allocations should be disposed, and how to interact with them.

### Included `default` analyzer

Included is an analyzer that emits errors where a disposable struct type is 
being created, but then initialized to `default`. Because disposable types 
imply they have a way to properly initialize them:
```cs
Allocation allocation = default; //U0001 error
```

There is no analysis for `new()` however, because a default constructor with
value types can be by design. Though if not, they can be declared with an 
`[Obsolete("", true)]` attribute with the parameter `true` to enforce usage.

### Contributing and direction

This library is made to provide building blocks for working with native code in C#.
For minimizing runtime cost and to expose the efficiency that was always there.
Commonly putting the author in a position where they need to exercise more control.

> _with great power, comes great responsibility_

Contributions that fit this are welcome.