using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Unmanaged
{
    public static class TypeSymbolExtensions
    {
        /// <summary>
        /// Checks if the type implements the <typeparamref name="T"/> interface.
        /// </summary>
        public static bool HasInterface<T>(this ITypeSymbol type) where T : class
        {
            string? fullInterfaceName = typeof(T)?.FullName;
            if (fullInterfaceName is null)
            {
                throw new InvalidOperationException("Type name is null when checking interface");
            }

            return type.HasInterface(fullInterfaceName);
        }

        /// <summary>
        /// Checks if the type implements the interface with the given <paramref name="fullInterfaceName"/>.
        /// </summary>
        public static bool HasInterface(this ITypeSymbol type, string fullInterfaceName)
        {
            Stack<ITypeSymbol> stack = new();
            foreach (ITypeSymbol interfaceType in type.AllInterfaces)
            {
                stack.Push(interfaceType);
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.ToDisplayString() == fullInterfaceName)
                {
                    return true;
                }
                else
                {
                    foreach (ITypeSymbol interfaceType in current.AllInterfaces)
                    {
                        stack.Push(interfaceType);
                    }

                    if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }

            return false;
        }
    }
}