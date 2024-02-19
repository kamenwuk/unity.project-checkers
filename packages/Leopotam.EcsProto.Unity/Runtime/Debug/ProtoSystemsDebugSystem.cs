// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023-2024 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Leopotam.EcsProto.Unity {
    public sealed class ProtoSystemsDebugView : MonoBehaviour {
        [NonSerialized] public Slice<SysItem> PreInitSystems;
        [NonSerialized] public Slice<SysItem> InitSystems;
        [NonSerialized] public Slice<SysItem> RunSystems;
        [NonSerialized] public Slice<SysItem> PostRunSystems;
        [NonSerialized] public Slice<SysItem> DestroySystems;
        [NonSerialized] public Slice<SysItem> PostDestroySystems;

        public struct SysItem {
            public string Name;
            public int Index;
        }

        [NonSerialized] public ProtoSystems Systems;
    }

    public sealed class ProtoSystemsDebugSystem : IProtoPreInitSystem, IProtoPostDestroySystem {
        readonly string _systemsName;
        GameObject _go;

        public ProtoSystemsDebugSystem (string systemsName = default) {
            _systemsName = systemsName;
        }

        public void PreInit (IProtoSystems systems) {
            var allSystems = systems.Systems ();
            var preInitList = new Slice<ProtoSystemsDebugView.SysItem> ();
            var initList = new Slice<ProtoSystemsDebugView.SysItem> ();
            var runList = new Slice<ProtoSystemsDebugView.SysItem> ();
            var postRunList = new Slice<ProtoSystemsDebugView.SysItem> ();
            var destroyList = new Slice<ProtoSystemsDebugView.SysItem> ();
            var postDestroyList = new Slice<ProtoSystemsDebugView.SysItem> ();
            for (var i = 0; i < allSystems.Len (); i++) {
                var sys = allSystems.Get (i);
                var sysItem = new ProtoSystemsDebugView.SysItem {
                    Name = EditorExtensions.GetCleanTypeName (sys.GetType ()),
                    Index = i
                };
                if (sys is IProtoPreInitSystem) { preInitList.Add (sysItem); }
                if (sys is IProtoInitSystem) { initList.Add (sysItem); }
                if (sys is IProtoRunSystem) { runList.Add (sysItem); }
                if (sys is IProtoPostRunSystem) { postRunList.Add (sysItem); }
                if (sys is IProtoDestroySystem) { destroyList.Add (sysItem); }
                if (sys is IProtoPostDestroySystem) { postDestroyList.Add (sysItem); }
            }
            _go = new GameObject (_systemsName != null ? $"[PROTO-SYSTEMS {_systemsName}]" : "[PROTO-SYSTEMS]");
            Object.DontDestroyOnLoad (_go);
            _go.hideFlags = HideFlags.NotEditable;
            var view = _go.AddComponent<ProtoSystemsDebugView> ();
            view.PreInitSystems = preInitList;
            view.InitSystems = initList;
            view.RunSystems = runList;
            view.PostRunSystems = postRunList;
            view.DestroySystems = destroyList;
            view.PostDestroySystems = postDestroyList;
            view.Systems = systems as ProtoSystems;
        }

        public void PostDestroy () {
            if (Application.isPlaying) {
                Object.Destroy (_go);
            } else {
                Object.DestroyImmediate (_go);
            }
            _go = null;
        }
    }
}
#endif
