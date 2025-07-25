using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Unmanaged.Tests
{
    public abstract class UnmanagedTests
    {
        [SetUp]
        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void TearDown()
        {
            MemoryTracker.ThrowIfAny(true);
        }

        public ReadOnlySpan<char> GetResourceText(string name)
        {
            using Stream stream = GetResourceStream(name);
            Span<byte> buffer = stackalloc byte[(int)stream.Length];
            stream.ReadExactly(buffer);
            ReadOnlySpan<char> text = Encoding.UTF8.GetString(buffer).AsSpan();
            if (text.Length > 0 && text[0] == 65279) // BOM
            {
                text = text[1..];
            }

            return text;
        }

        public void GetResourceText(Assembly assembly, string name, Text destination)
        {
            using Stream stream = GetResourceStream(name);
            Span<byte> buffer = stackalloc byte[(int)stream.Length];
            stream.ReadExactly(buffer);
            destination.CopyFrom(Encoding.UTF8.GetString(buffer));
            if (destination.Length > 0 && destination[0] == 65279) // BOM
            {
                destination.RemoveAt(0);
            }
        }

        public Stream GetResourceStream(string name)
        {
            ThrowIfResourceDoesntExist(name);

            int index = IndexOfResource(name);
            Assembly assembly = GetType().Assembly;
            string resourceName = assembly.GetManifestResourceNames()[index];
            return assembly.GetManifestResourceStream(resourceName)!;
        }

        public int IndexOfResource(string name)
        {
            Assembly assembly = GetType().Assembly;
            string[] resourceNames = assembly.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; i++)
            {
                string resourceName = resourceNames[i];
                for (int c = 0; c < resourceName.Length; c++)
                {
                    char resourceNameCharacter = resourceName[resourceName.Length - 1 - c];
                    char nameCharacter = name[name.Length - 1 - c];
                    if (resourceNameCharacter != nameCharacter)
                    {
                        if (resourceNameCharacter == '.' && nameCharacter == '/')
                        {
                            continue;
                        }
                        else if (resourceNameCharacter == '_' && nameCharacter == ' ')
                        {
                            continue;
                        }

                        break;
                    }

                    if (c == name.Length - 1)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        [Conditional("DEBUG")]
        private void ThrowIfResourceDoesntExist(string name)
        {
            if (IndexOfResource(name) == -1)
            {
                throw new NullReferenceException($"Resource `{name}` does not exist");
            }
        }
    }
}