# Unmanaged

[![Test](https://github.com/simulation-tree/unmanaged/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/unmanaged/actions/workflows/test.yml)

Library containing primitives for working with native C#.

### Installation

Install it by cloning it, or referencing through the NuGet package through GitHub's registry ([authentication info](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages)).

For installing as a Unity package, use this git url to add it:
```
https://github.com/simulation-tree/unmanaged.git?path=core#unity
```

### Memory Addresses

`MemoryAddress` instances can point to either heap or stack memory. They can be
allocated using static methods, in which case they must also be disposed:
```cs
//an allocation that is 10 bytes in size
using (MemoryAddress allocation = MemoryAddress.Allocate(sizeof(char) * 5))
{
    allocation.Write("Hello".AsSpan());
    Span<char> text = allocation.AsSpan<char>();
}

//an allocation containing a float
using (MemoryAddress allocation = MemoryAddress.Create(3.14f))
{
    ref float floatValue = ref allocation.Read<float>();
    floatValue *= 2;
}
```

### ASCII Text

The types prefixed with `ASCIIText` store extended ASCII `char` values. 
Useful for when text is known to be short, until a list/array is needed:
```cs
ASCIIText16 text = new("Hello there");
Span<char> textBuffer = stackalloc char[text.Length];
text.CopyTo(textBuffer);

//get utf8 bytes from the text
Span<byte> utf8bytes = stackalloc char[ASCIIText16.Capacity];
uint bytesCopied = text.CopyTo(utf8bytes);

//get text from utf8 bytes using System.Text.Encoding
ASCIIText1024 textFromBytes = new(utf8bytes.Slice(0, bytesCopied));
string systemString = Encoding.UTF8.GetString(textBuffer.Slice(0, length));
Assert.That(textFromBytes.ToString(), Is.EqualTo(systemString));

//casting to a smaller type
ASCIIText32 textFromBytesButSmaller = textFromBytes;
Assert.That(textFromBytesButSmaller.ToString(), Is.EqualTo(systemString));
```

### Text

Accompanying the above is the disposable `Text` type, which is a reference to an arbitrary amount of `char`s values.
And behaves like a `string`:
```cs
using Text builder = new();
builder.Append("Hello");
builder.Append(" there");

ReadOnlySpan<char> text = builder.AsSpan();
Console.WriteLine(text.ToString());
```

### Random Generator

Can generate random data and values using the XORshift technique:
```cs
using RandomGenerator random = new();
int fairDiceRoll = random.NextInt(0, 6);
```

> The default random seed is based on process ID, current time, and instance index.

### Safety

When working with `MemoryAddress` values, there are checks available to ensure
that the memory is not accessed after it has been disposed, or accessed out of bounds.
This only occurs in debug mode, or with the `#TRACK` directive defined, and only on
heap allocated memory through the `MemoryAddress` methods.

Ultimately, it is the programmer's responsibility for how memory should be managed. Including
when allocations should be disposed, and how they should be accessed.

### Included `default` analyzer

Included is an analyzer that emits errors where a disposable struct type is 
being created, but then initialized to `default`. Because disposable types 
imply they have a way to properly initialize them:
```cs
Allocation allocation = default; //U0001 error
```

There is no analysis for `new()` however, because a default constructor with
value types can be by design. Though if not, they can be declared with an 
`[Obsolete("", true)]` attribute with the parameter `true` to disallow it.

### Contributing and direction

This library is made to provide building blocks for working with native code in C#.
For minimizing runtime cost and to expose the efficiency that was always there.
Commonly putting the author in a position where they need to exercise more control.

> _with great power, comes great responsibility_

Contributions that fit this are welcome.
