﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Unity
{
    public abstract class SymbolResolver
    {
        protected abstract IntPtr GetAddressOrZero(string name);

        public abstract IEnumerable<string> FindSymbolsMatching(Regex expression);

        public IntPtr Resolve(string name)
        {
            if (TryResolve(name, out IntPtr address))
                return address;

            throw new UnresolvedSymbolException(name);
        }

        public bool TryResolve(string name, out IntPtr address)
        {
            address = GetAddressOrZero(name);
            return address != IntPtr.Zero;
        }

        public unsafe bool TryResolve<T>(string name, out T* address)
            where T : unmanaged
        {
            address = (T*)GetAddressOrZero(name);
            return address != null;
        }

        public bool TryResolveFunction<T>(string name, out T function)
            where T : Delegate
        {
            var address = GetAddressOrZero(name);
            function    = address != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(address) : null;
            return function != null;
        }

        public T ResolveFunction<T>(string name)
            where T : Delegate
        {
            if (TryResolveFunction(name, out T del))
                return del;

            throw new UnresolvedSymbolException(name);
        }

        public unsafe T* Resolve<T>(string name)
            where T : unmanaged
        {
            if (TryResolve(name, out T* address))
                return address;

            throw new UnresolvedSymbolException(name);
        }

        public IntPtr ResolveFirstMatching(Regex regex)
        {
            var name = FindSymbolsMatching(regex).FirstOrDefault();

            if (string.IsNullOrEmpty(name))
                throw new UnresolvedSymbolException(regex.ToString());

            return Resolve(name);
        }

        public unsafe T* ResolveFirstMatching<T>(Regex regex)
            where T : unmanaged
        {
            return (T*)ResolveFirstMatching(regex);
        }

        public T ResolveFirstFunctionMatching<T>(Regex regex)
            where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(ResolveFirstMatching(regex));
        }
    }
}
