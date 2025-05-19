using System;

namespace Unmanaged.Tests
{
    public class BinaryReaderWriterTests : UnmanagedTests
    {
        [Test]
        public void CloneSomething()
        {
            Something apple = new("Apple 2 xx");
            Assert.That(apple.Name.ToString(), Is.EqualTo("Apple 2 xx"));
            Something apple2 = apple.Clone();
            Assert.That(apple2.Name.ToString(), Is.EqualTo("Apple 2 xx"));
            Assert.That(apple.Name, Is.EqualTo(apple2.Name));
        }

        public struct Something : ISerializable, IEquatable<Something>
        {
            private ASCIIText256 name;

            public readonly ASCIIText256 Name => name;

            public Something(string name)
            {
                this.name = new(name);
            }

            void ISerializable.Write(ByteWriter writer)
            {
                writer.WriteUTF8(name);
            }

            void ISerializable.Read(ByteReader reader)
            {
                Span<char> buffer = stackalloc char[ASCIIText256.Capacity];
                int length = reader.ReadUTF8(buffer);
                name = new ASCIIText256(buffer.Slice(0, length));
            }

            public override bool Equals(object? obj)
            {
                return obj is Something something && Equals(something);
            }

            public bool Equals(Something other)
            {
                return name.Equals(other.name);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name);
            }

            public static bool operator ==(Something left, Something right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Something left, Something right)
            {
                return !(left == right);
            }
        }

        [Test]
        public void WriteValues()
        {
            using ByteWriter writer = new();
            writer.WriteValue(32);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int)));
            writer.WriteValue(64);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 2));
            writer.WriteValue(128);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));

            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));
            byte[] bytes = writer.AsSpan().ToArray();
            using ByteReader reader = new(bytes);
            byte[] readerBytes = reader.GetBytes().ToArray();
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [TestCase("Hello there")]
        [TestCase("And goodbye")]
        [TestCase("To anyone")]
        [TestCase("Reading this code")]
        public void WriteSpan(string inputString)
        {
            using ByteWriter writer = new();
            writer.WriteSpan<char>(inputString);

            using ByteReader reader = new(writer.AsSpan());
            Assert.That(reader.ReadSpan<char>(inputString.Length).ToString(), Is.EqualTo(inputString));
        }

        [Test]
        public void CreateReaderFromBytes()
        {
            using var stream = new System.IO.MemoryStream();
            using System.IO.BinaryWriter binWriter = new(stream);
            binWriter.Write(32);
            binWriter.Write(64);
            binWriter.Write(128);
            byte[] bytes = stream.ToArray();
            using ByteReader reader = new(bytes);
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void CreateReaderFromStream()
        {
            using System.IO.MemoryStream stream = new System.IO.MemoryStream();
            stream.Write([32, 0, 0, 0, 64, 0, 0, 0, 128, 0, 0, 0]);
            stream.Position = 0;
            using ByteReader reader = new(stream);
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void ReadUTF8Text()
        {
            byte[] data = new byte[] { 239, 187, 191, 60, 80, 114, 111, 106, 101, 99, 116, 32, 83, 100, 107 };
            using ByteReader reader = new(data);
            Span<char> sample = stackalloc char[16];
            int length = reader.ReadUTF8(sample);
            Span<char> result = sample.Slice(0, length);
            Assert.That(result.ToString(), Is.EqualTo("<Project Sdk"));
        }

        [TestCase("Hello, 你好, 🌍")]
        [TestCase("aaaaaaaaaaaaaaa")]
        public void WriteUTF8Text(string myString)
        {
            using ByteWriter writer = new();
            writer.WriteUTF8(myString);
            using ByteReader reader = new(writer.AsSpan());
            Span<char> sample = stackalloc char[myString.Length * 2];
            int length = reader.ReadUTF8(sample);
            Span<char> result = sample.Slice(0, length);
            string resultString = result.ToString();
            Assert.That(resultString, Is.EqualTo(myString));
        }

        [TestCase('a', 23)]
        [TestCase('b', 13)]
        [TestCase('2', 9)]
        public void WriteUTF8Repeat(char character, int repeat)
        {
            using ByteWriter writer = new();
            writer.WriteUTF8(character, repeat);
            using ByteReader reader = new(writer.AsSpan());
            Span<char> sample = stackalloc char[repeat * 2];
            int length = reader.ReadUTF8(sample);
            Span<char> result = sample.Slice(0, length);
            string resultString = result.ToString();
            Assert.That(resultString, Is.EqualTo(new string(character, repeat)));
        }

#if DEBUG
        [Test]
        public void ThrowIfReadTooMuch()
        {
            using ByteWriter writer = new();
            writer.WriteSpan<char>("The snake that eats its own tail".AsSpan());
            using ByteReader reader = new(writer.AsSpan());
            Assert.Throws<InvalidOperationException>(() => reader.ReadSpan<char>(100));
        }
#endif

        [Test]
        public void ReuseWriter()
        {
            ByteWriter writer = new();
            writer.WriteValue(32);
            writer.WriteValue(64);
            writer.WriteValue(128);

            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));
            int[] values = writer.AsSpan().Reinterpret<byte, int>().ToArray();
            writer.Position = 0;
            Assert.That(writer.Position, Is.EqualTo(0));
            Assert.That(values, Has.Length.EqualTo(3));
            Assert.That(values, Contains.Item(32));
            Assert.That(values, Contains.Item(64));
            Assert.That(values, Contains.Item(128));
            Assert.That(writer.AsSpan().Reinterpret<byte, int>().Length, Is.EqualTo(0));

            writer.Dispose();
        }

        [Test]
        public void BinaryReadAndWrite()
        {
            Span<Fruit> fruits =
            [
                new(1),
                new(3),
                new(6),
                new(-10),
            ];

            Big big = new(32, new Cherry("apple"), fruits);
            using ByteWriter writer = new();
            writer.WriteValue(big);
            using ByteReader reader = new(writer.AsSpan());
            using Big loadedBig = reader.ReadValue<Big>();
            Assert.That(loadedBig, Is.EqualTo(big));
        }

        [Test]
        public void SaveAndLoadSpans()
        {
            using ByteWriter writer = new();
            writer.WriteSpan<byte>([1, 2, 3, 4, 5]);
            writer.WriteSpan<int>([1, 2, 3, 4, 5]);
            writer.WriteSpan<ASCIIText256>(["Hello", "World", "Goodbye"]);

            using ByteReader reader = new(writer.AsSpan());
            Span<byte> bytes = reader.ReadSpan<byte>(5);
            Span<int> ints = reader.ReadSpan<int>(5);
            Span<ASCIIText256> strings = reader.ReadSpan<ASCIIText256>(3);

            Assert.That(bytes.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(ints.ToArray(), Is.EquivalentTo(new int[] { 1, 2, 3, 4, 5 }));
            Assert.That(strings.ToArray(), Is.EquivalentTo(new ASCIIText256[] { "Hello", "World", "Goodbye" }));
        }

        [Test]
        public void CheckSerializable()
        {
            using Complicated complicated = new();
            Player player1 = new(100, 10);
            player1.Add(new Fruit(32));
            player1.Add(new Fruit(6123231));
            Player player2 = new(200, 20);
            player2.Add(new Fruit(32));
            Player player3 = new(300, 30);
            player3.Add(new Fruit(123123213));
            complicated.Add(player1);
            complicated.Add(player2);
            complicated.Add(player3);

            using ByteWriter writer = new();
            writer.WriteObject(complicated);
            using ByteReader reader = new(writer.AsSpan());
            using Complicated loadedComplicated = reader.ReadObject<Complicated>();

            Assert.That(loadedComplicated.List.Length, Is.EqualTo(complicated.List.Length));
            for (int i = 0; i < complicated.List.Length; i++)
            {
                Player actual = loadedComplicated.List[i];
                Player expected = complicated.List[i];
                Assert.That(actual.Inventory.Length, Is.EqualTo(expected.Inventory.Length));
                for (int j = 0; j < actual.Inventory.Length; j++)
                {
                    Fruit actualFruit = actual.Inventory[j];
                    Fruit expectedFruit = expected.Inventory[j];
                    Assert.That(actualFruit, Is.EqualTo(expectedFruit));
                }

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        public struct Complicated : IDisposable, ISerializable
        {
            private MemoryAddress players;
            private int count;
            private int capacity;

            public readonly Span<Player> List => players.AsSpan<Player>(0, count);

            public Complicated()
            {
                players = MemoryAddress.Allocate(Player.TypeSize);
                capacity = 1;
            }

            public void Add(Player player)
            {
                if (count == capacity)
                {
                    capacity *= 2;
                    MemoryAddress.Resize(ref players, capacity * Player.TypeSize);
                }

                players.Write(count * Player.TypeSize, player);
                count++;
            }

            public readonly void Dispose()
            {
                Span<Player> list = List;
                foreach (Player player in list)
                {
                    player.Dispose();
                }

                players.Dispose();
            }

            void ISerializable.Read(ByteReader reader)
            {
                byte add = reader.ReadValue<byte>();
                players = MemoryAddress.Allocate(Player.TypeSize);
                capacity = 1;
                for (uint i = 0; i < add; i++)
                {
                    Player player = reader.ReadObject<Player>();
                    Add(player);
                }
            }

            void ISerializable.Write(ByteWriter writer)
            {
                writer.WriteValue((byte)count);
                foreach (Player player in List)
                {
                    writer.WriteObject(player);
                }
            }
        }

        public struct Player : IDisposable, ISerializable, IEquatable<Player>
        {
            public unsafe static readonly int TypeSize = sizeof(Player);

            public uint hp;
            public uint damage;

            private int count;
            private int capacity;
            private MemoryAddress inventory;

            public readonly Span<Fruit> Inventory => inventory.AsSpan<Fruit>(0, count);

            public Player(uint hp, uint damage)
            {
                this.hp = hp;
                this.damage = damage;
                this.inventory = MemoryAddress.Allocate(Fruit.TypeSize);
                capacity = 1;
            }

            public readonly override string ToString()
            {
                return $"Player: HP={hp}, Damage={damage}";
            }

            public void Dispose()
            {
                inventory.Dispose();
            }

            public void Add(Fruit fruit)
            {
                if (count == capacity)
                {
                    capacity *= 2;
                    MemoryAddress.Resize(ref inventory, capacity * Fruit.TypeSize);
                }

                inventory.Write(count * Fruit.TypeSize, fruit);
                count++;
            }

            void ISerializable.Read(ByteReader reader)
            {
                hp = reader.ReadValue<uint>();
                damage = reader.ReadValue<uint>();
                uint add = reader.ReadValue<uint>();
                inventory = MemoryAddress.Allocate(Fruit.TypeSize);
                capacity = 1;
                for (uint i = 0; i < add; i++)
                {
                    Add(reader.ReadValue<Fruit>());
                }
            }

            void ISerializable.Write(ByteWriter writer)
            {
                writer.WriteValue(hp);
                writer.WriteValue(damage);
                writer.WriteValue(count);
                foreach (Fruit fruit in Inventory)
                {
                    writer.WriteValue(fruit);
                }
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Player player && Equals(player);
            }

            public readonly bool Equals(Player other)
            {
                return hp == other.hp && damage == other.damage && InventoryContentsEqual(other);
            }

            public readonly bool InventoryContentsEqual(Player other)
            {
                if (count != other.count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (inventory[i] != other.inventory[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(hp, damage, inventory, count, capacity);
            }

            public static bool operator ==(Player left, Player right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Player left, Player right)
            {
                return !(left == right);
            }
        }

        public readonly struct Big(int a, Cherry apple, Span<Fruit> fruits) : IDisposable, IEquatable<Big>
        {
            public readonly int a = a;
            public readonly Cherry apple = apple;
            public readonly byte count = (byte)fruits.Length;
            public readonly MemoryAddress fruits = MemoryAddress.Allocate(fruits);

            public void Dispose()
            {
                fruits.Dispose();
            }

            public override bool Equals(object? obj)
            {
                return obj is Big big && Equals(big);
            }

            public bool Equals(Big other)
            {
                return a == other.a && fruits.Equals(other.fruits) && count == other.count && apple.stones == other.apple.stones;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(a, count, apple, fruits);
            }

            public static bool operator ==(Big left, Big right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Big left, Big right)
            {
                return !(left == right);
            }
        }
        public readonly struct Fruit : IEquatable<Fruit>
        {
            public unsafe static readonly int TypeSize = sizeof(Fruit);

            public readonly int data;

            public Fruit(int data)
            {
                this.data = data;
            }

            public override bool Equals(object? obj)
            {
                return obj is Fruit fruit && Equals(fruit);
            }

            public bool Equals(Fruit other)
            {
                return data == other.data;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(data);
            }

            public static bool operator ==(Fruit left, Fruit right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Fruit left, Fruit right)
            {
                return !(left == right);
            }
        }

        public struct Cherry
        {
            public ASCIIText256 stones;

            public Cherry(ASCIIText256 stones)
            {
                this.stones = stones;
            }
        }
    }
}