﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2019.3+
        unsafe class V2019_3 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => Tree.Data->StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            private TypeTreeNode[] m_Nodes;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label);

            public V2019_3(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z");
                var label = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label);
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                var nodes = new TypeTreeNode[Tree.Data->Nodes.Size];

                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = new TypeTreeNode(new TypeTreeNode.V2019_1(Tree.Data->Nodes.Ptr[i]), owner);
                m_Nodes = nodes;
            }

            internal struct TypeTree
            {
                public TypeTreeShareableData* Data;
                public IntPtr ReferencedTypes;
                [MarshalAs(UnmanagedType.U1)]
                public bool PoolOwned;
            }

            internal struct TypeTreeShareableData
            {
                public DynamicArray<TypeTreeNode.V2019_1.TypeTreeNode> Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
                public TransferInstructionFlags FlagsAtGeneration;
                public int RefCount;
                public MemLabelId* MemLabel;
            }
        }
    }
}
