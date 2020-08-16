﻿using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EasyHook;
using Dia2Lib;
using Unity;

namespace TypeTreeDumper
{
    public class EntryPoint : IEntryPoint
    {
        readonly IpcInterface server;

        readonly ProcessModule module;

        readonly IDiaDataSource dia;

        readonly IDiaSession session;

        SymbolResolver resolver;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void AfterEverythingLoadedDelegate(IntPtr app);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityVersionDelegate(out UnityVersion version, [MarshalAs(UnmanagedType.LPStr)] string value);

        static UnityVersionDelegate ParseUnityVersion;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetUnityVersionDelegate();

        static GetUnityVersionDelegate GetUnityVersion;

        bool engineLoaded;

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            module = Process.GetCurrentProcess().MainModule;
            server = RemoteHooking.IpcConnectClient<IpcInterface>(channelName);
            dia    = new DiaSourceClass();
            dia.loadDataForExe(module.FileName, null, null);
            dia.openSession(out session);
        }

        [SuppressMessage("Style", "IDE0060", Justification = "Required by EasyHook")]
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            resolver = new DiaSymbolResolver(session, module);

            // Wait for Unity to initialize before grabbing data
            using (var hook = CreateEngineInitializationHook())
            {
                RemoteHooking.WakeUpProcess();

                while (!engineLoaded)
                {
                    server.Ping();
                    Thread.Sleep(500);
                }
            }

            GetUnityVersion   = resolver.ResolveFunction<GetUnityVersionDelegate>("?GetUnityVersion@Application_Bindings@@YAPEBDXZ");
            ParseUnityVersion = resolver.ResolveFunction<UnityVersionDelegate>("??0UnityVersion@@QEAA@PEBD@Z");

            if (GetUnityVersion is null || ParseUnityVersion is null)
            {
                server.WriteLine("Error: Could not resolve Application_Bindings::GetUnityVersion and/or UnityVersion::UnityVersion.");
                return;
            }

            ParseUnityVersion(out UnityVersion version, Marshal.PtrToStringAnsi(GetUnityVersion()));
            var engine = new UnityEngine(version, resolver);

            foreach (var type in engine.RuntimeTypes)
                server.WriteLine(type.Name);

            server.WriteLine();
            server.WriteLine("Exiting");
            Environment.Exit(0);
        }

        LocalHook CreateEngineInitializationHook()
        {
            var address = resolver.Resolve("?AfterEverythingLoaded@Application@@QEAAXXZ");
            var hook    = LocalHook.Create(address, new AfterEverythingLoadedDelegate(AfterEverythingLoaded), null);
            
            void AfterEverythingLoaded(IntPtr application)
            {
                var bypass = HookRuntimeInfo.Handle.HookBypassAddress;
                var method = Marshal.GetDelegateForFunctionPointer<AfterEverythingLoadedDelegate>(bypass);
                method.Invoke(application);
                engineLoaded = true;
            }

            hook.ThreadACL.SetExclusiveACL(Array.Empty<int>());
            return hook;
        }
    }
}