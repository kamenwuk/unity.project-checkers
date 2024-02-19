// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsProto {
    public interface IProtoSystem { }

    public interface IProtoPreInitSystem : IProtoSystem {
        void PreInit (IProtoSystems systems);
    }

    public interface IProtoInitSystem : IProtoSystem {
        void Init (IProtoSystems systems);
    }

    public interface IProtoRunSystem : IProtoSystem {
        void Run ();
    }

    public interface IProtoPostRunSystem : IProtoSystem {
        void PostRun ();
    }

    public interface IProtoDestroySystem : IProtoSystem {
        void Destroy ();
    }

    public interface IProtoPostDestroySystem : IProtoSystem {
        void PostDestroy ();
    }

    public interface IProtoModule {
        void Init (IProtoSystems systems);
        IProtoAspect[] Aspects ();
        IProtoModule[] Modules ();
    }

    public interface IProtoSystems {
        IProtoSystems AddSystem (IProtoSystem system, string pointName = default);
        IProtoSystems AddService (object injectInstance, Type asType = default);
        IProtoSystems AddModule (IProtoModule module);
        IProtoSystems AddPoint (string pointName);
        IProtoSystems AddWorld (ProtoWorld world, string name);
        ProtoWorld World (string worldName = default);
        Dictionary<string, ProtoWorld> NamedWorlds ();
        Dictionary<Type, object> Services ();
        Slice<IProtoSystem> Systems ();
        void Init ();
        void Run ();
        void Destroy ();
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class ProtoSystems : IProtoSystems {
        protected const string DefaultPointName = "<default>";

        protected ProtoWorld _defaultWorld;
        protected Dictionary<string, ProtoWorld> _worldMap;
        protected Slice<IProtoSystem> _allSystems;
        protected Slice<IProtoRunSystem> _runSystems;
        protected Slice<IProtoPostRunSystem> _postrunSystems;
        protected Dictionary<string, Slice<IProtoSystem>> _deferredSystems;
        protected Dictionary<Type, object> _services;
        protected bool _inited;
        bool _defaultPointAdded;
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
        System.Diagnostics.Stopwatch _sw;
        Slice<int[]> _benches;
        Slice<int> _runSystemIndices;
        Slice<int> _postRunSystemIndices;

        public enum BenchType {
            PreInit,
            Init,
            Run,
            PostRun,
            Destroy,
            PostDestroy
        }

        public int Bench (int idx, BenchType sType) => _benches.Get (idx)[(int) sType];
#endif

        public ProtoSystems (ProtoWorld defaultWorld) {
            _defaultWorld = defaultWorld;
            _worldMap = new (4);
            _allSystems = new (64);
            _runSystems = new (64);
            _postrunSystems = new (64);
            _deferredSystems = new (32);
            _services = new (32);
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
            _sw = new ();
            _benches = new (64);
            _runSystemIndices = new (64);
            _postRunSystemIndices = new (64);
#endif
            _defaultPointAdded = false;
            _inited = false;
        }

        public IProtoSystems AddSystem (IProtoSystem system, string pointName = default) {
#if DEBUG
            if (IsInited ()) { throw new Exception ($"не могу добавить систему \"{system.GetType ().Name}\", системы уже инициализированы"); }
#endif
            pointName = !string.IsNullOrEmpty (pointName) ? pointName : DefaultPointName;
            if (!_deferredSystems.TryGetValue (pointName, out var list)) {
                list = new (8);
                _deferredSystems[pointName] = list;
            }
            list.Add (system);
            return this;
        }

        public IProtoSystems AddService (object injectInstance, Type asType = default) {
            var type = asType ?? injectInstance.GetType ();
#if DEBUG
            if (IsInited ()) { throw new Exception ($"не могу добавить сервис с типом \"{type.Name}\", системы уже инициализированы"); }
            if (injectInstance is IProtoSystem) { throw new Exception ($"не могу добавить сервис с типом \"{type.Name}\", система не должна использоваться как сервис"); }
            if (_services.ContainsKey (type)) { throw new Exception ($"не могу добавить сервис с типом \"{type.Name}\", такой тип уже существует"); }
#endif
            _services[type] = injectInstance;
            return this;
        }

        public IProtoSystems AddModule (IProtoModule module) {
#if DEBUG
            if (IsInited ()) { throw new Exception ($"не могу добавить модуль \"{module.GetType ().Name}\", системы уже инициализированы"); }
            if (_defaultPointAdded) { throw new Exception ($"не могу добавить модуль \"{module.GetType ().Name}\", он должен быть зарегистрирован до первого вызова AddPoint()"); }
#endif
            module.Init (this);
            return this;
        }

        public IProtoSystems AddPoint (string pointName) {
#if DEBUG
            if (IsInited ()) { throw new Exception ($"не могу добавить точку \"{pointName}\", системы уже инициализированы"); }
            if (string.IsNullOrEmpty (pointName)) { throw new Exception ("не могу добавить точку без имени"); }
#endif
            if (!_defaultPointAdded) {
                // системы без явной привязки к точкам должны быть добавлены перед всеми точками.
                _defaultPointAdded = true;
                if (_deferredSystems.TryGetValue (DefaultPointName, out var defList)) {
                    AddDeferredSystems (defList);
                    _deferredSystems.Remove (DefaultPointName);
                }
            }
            if (_deferredSystems.TryGetValue (pointName, out var list)) {
                AddDeferredSystems (list);
                _deferredSystems.Remove (pointName);
            }
            return this;
        }

        public IProtoSystems AddWorld (ProtoWorld world, string name) {
#if DEBUG
            if (IsInited ()) { throw new Exception ($"не могу добавить мир с именем \"{name}\", системы уже инициализированы"); }
            if (string.IsNullOrEmpty (name)) { throw new Exception ("не могу добавить мир с пустым именем"); }
            if (_worldMap.ContainsKey (name)) { throw new Exception ($"не могу добавить мир с именем \"{name}\", имя уже существует"); }
#endif
            _worldMap[name] = world;
            return this;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoWorld World (string worldName = default) {
            if (worldName == default) {
                return _defaultWorld;
            }
#if DEBUG
            if (!_worldMap.ContainsKey (worldName)) { throw new Exception ($"не могу найти мир с именем \"{worldName}\", его сперва надо зарегистрировать в системах"); }
#endif
            return _worldMap[worldName];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Dictionary<string, ProtoWorld> NamedWorlds () => _worldMap;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Dictionary<Type, object> Services () => _services;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Slice<IProtoSystem> Systems () => _allSystems;

        public virtual void Init () {
            // добавляем системы без привязки, если они не были добавлены.
            AddPoint (DefaultPointName);
#if DEBUG
            foreach (var kv in _deferredSystems) {
                throw new Exception ($"требуемая точка привязки \"{kv.Key}\" не найдена");
            }
#endif
            for (int i = 0, iMax = _allSystems.Len (); i < iMax; i++) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                var benchesItem = new int[(int) BenchType.PostDestroy + 1];
                for (var ii = 0; ii < benchesItem.Length; ii++) { benchesItem[ii] = -100; }
                _benches.Add (benchesItem);
                if (_allSystems.Get (i) is IProtoRunSystem) {
                    _runSystemIndices.Add (i);
                }
                if (_allSystems.Get (i) is IProtoPostRunSystem) {
                    _postRunSystemIndices.Add (i);
                }
#endif
                if (_allSystems.Get (i) is IProtoPreInitSystem piSystem) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Restart ();
#endif
                    piSystem.PreInit (this);
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Stop ();
                    _benches.Get (i)[(int) BenchType.PreInit] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {piSystem.GetType ().Name}.PreInit()"); }
#endif
                }
            }
            for (int i = 0, iMax = _allSystems.Len (); i < iMax; i++) {
                if (_allSystems.Get (i) is IProtoInitSystem iSystem) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Restart ();
#endif
                    iSystem.Init (this);
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Stop ();
                    _benches.Get (i)[(int) BenchType.Init] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {iSystem.GetType ().Name}.Init()"); }
#endif
                }
            }
        }

        public virtual void Run () {
            for (int i = 0, iMax = _runSystems.Len (); i < iMax; i++) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                _sw.Restart ();
#endif
                _runSystems.Get (i).Run ();
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                _sw.Stop ();
                _benches.Get (_runSystemIndices.Get (i))[(int) BenchType.Run] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                var worldName = CheckForLeakedEntities (this);
                if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {_runSystems.Get (i).GetType ().Name}.Run()"); }
#endif
            }
            for (int i = 0, iMax = _postrunSystems.Len (); i < iMax; i++) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                _sw.Restart ();
#endif
                _postrunSystems.Get (i).PostRun ();
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                _sw.Stop ();
                _benches.Get (_postRunSystemIndices.Get (i))[(int) BenchType.PostRun] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                var worldName = CheckForLeakedEntities (this);
                if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {_postrunSystems.Get (i).GetType ().Name}.PostRun()"); }
#endif
            }
        }

        public virtual void Destroy () {
            for (int i = 0, iMax = _allSystems.Len (); i < iMax; i++) {
                if (_allSystems.Get (i) is IProtoDestroySystem dSystem) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Restart ();
#endif
                    dSystem.Destroy ();
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Stop ();
                    _benches.Get (i)[(int) BenchType.Destroy] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {dSystem.GetType ().Name}.Destroy()"); }
#endif
                }
            }
            for (int i = 0, iMax = _allSystems.Len (); i < iMax; i++) {
                if (_allSystems.Get (i) is IProtoPostDestroySystem pdSystem) {
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Restart ();
#endif
                    pdSystem.PostDestroy ();
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                    _sw.Stop ();
                    _benches.Get (i)[(int) BenchType.PostDestroy] = (int) (_sw.Elapsed.TotalMilliseconds * 100);
#endif
#if DEBUG
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new Exception ($"обнаружена пустая сущность в мире \"{worldName}\" после вызова {pdSystem.GetType ().Name}.PostDestroy()"); }
#endif
                }
            }
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
            _sw = null;
#endif
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsInited () {
            return _inited;
        }

        void AddDeferredSystems (Slice<IProtoSystem> list) {
            for (var i = 0; i < list.Len (); i++) {
                var sys = list.Get (i);
                _allSystems.Add (sys);
                if (sys is IProtoRunSystem runSystem) {
                    _runSystems.Add (runSystem);
                }
                if (sys is IProtoPostRunSystem postRunSystem) {
                    _postrunSystems.Add (postRunSystem);
                }
            }
        }
#if DEBUG
        public static string CheckForLeakedEntities (IProtoSystems systems) {
            if (ProtoWorld.CheckForLeakedEntities (systems.World ())) { return "по умолчанию"; }
            foreach (var pair in systems.NamedWorlds ()) {
                if (ProtoWorld.CheckForLeakedEntities (pair.Value)) {
                    return pair.Key;
                }
            }
            return null;
        }
#endif
    }
}
