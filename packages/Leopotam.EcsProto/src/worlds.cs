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
    [Serializable]
    public struct ProtoEntity : IEquatable<ProtoEntity> {
        internal int _id;

        public override int GetHashCode () => _id;
        public override string ToString () => _id.ToString ();

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Equals (ProtoEntity rhs) => _id == rhs._id;

        /// <summary>
        /// Использовать только в крайнем случае.
        /// </summary>
        public static ProtoEntity FromIdx (int idx) => new () { _id = idx };
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public static class ProtoEntityExtensions {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
#if DEBUG
        [Obsolete ("Вместо a.EqualsTo(b) следует использовать a.Equals(b)")]
#endif
        public static bool EqualsTo (this ProtoEntity a, ProtoEntity b) => a.Equals (b);
    }
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
    public interface IProtoEventListener {
        void OnEntityCreated (ProtoEntity entity);
        void OnEntityChanged (ProtoEntity entity, ushort poolId, bool added);
        void OnEntityDestroyed (ProtoEntity entity);
        void OnWorldResized (int capacity);
        void OnWorldDestroyed ();
    }
#endif
    public interface IProtoAspect {
        void Init (ProtoWorld world);
        void PostInit ();
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class ProtoWorld {
        readonly Slice<ulong> _entityMasks;
        readonly Slice<short> _entityGens;
        readonly Slice<ProtoEntity> _recycled;
        readonly Dictionary<Type, IProtoAspect> _aspects;
        readonly Slice<IProtoPool> _pools;
        readonly Dictionary<Type, IProtoPool> _poolsMap;
        readonly Slice<ulong> _copyBuf;
        readonly ushort _entityMaskLen;
        bool _destroyed;
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
        readonly Slice<IProtoEventListener> _listeners;
#endif
#if DEBUG
        protected bool _inited;
        readonly Slice<ProtoEntity> _leaked;

        public static bool CheckForLeakedEntities (ProtoWorld host) {
            if (host._leaked.Len () > 0) {
                for (int i = 0, iMax = host._leaked.Len (); i < iMax; i++) {
                    var e = host._leaked.Get (i);
                    if (host._entityGens.Get (e._id) > 0 && EntityMask.IsEmpty (host._entityMasks, host._entityMaskLen, e)) {
                        return true;
                    }
                }
                host._leaked.Clear (false);
            }
            return false;
        }
#endif
        public ProtoWorld (IProtoAspect aspect, Config cfg = default) {
            var capacity = cfg.Entities > 0 ? cfg.Entities : Config.EntitiesDefault;
            _entityGens = new (capacity);
            capacity = cfg.RecycledEntities > 0 ? cfg.RecycledEntities : Config.RecycledEntitiesDefault;
            _recycled = new (capacity);
            capacity = cfg.Aspects > 0 ? cfg.Aspects : Config.AspectsDefault;
            _aspects = new (capacity);
            capacity = cfg.Pools > 0 ? cfg.Pools : Config.PoolsDefault;
            _pools = new (capacity);
            _poolsMap = new (capacity);
            aspect.Init (this);

            var poolsLen = _pools.Len ();
            _entityMaskLen = (ushort) (poolsLen >> 6);
            if (poolsLen - (_entityMaskLen << 6) != 0) {
                _entityMaskLen++;
            }
            _entityMasks = new (_entityGens.Cap () * _entityMaskLen);

#if DEBUG || LEOECSPROTO_WORLD_EVENTS
            _listeners = new (4);
#endif
#if DEBUG
            if (_aspects.Count == 0) { throw new Exception ("нет зарегистрированных аспектов"); }
            _leaked = new (128);
            _inited = true;
#endif
            aspect.PostInit ();
            _copyBuf = new (_entityMaskLen, true);
            _destroyed = false;
        }

        public void Destroy () {
#if DEBUG
            if (_destroyed) { throw new Exception ("мир уже не существует до вызова ProtoWorld.Destroy()"); }
            if (CheckForLeakedEntities (this)) { throw new Exception ("обнаружена пустая сущность до вызова ProtoWorld.Destroy()"); }
#endif
            _destroyed = true;
            ProtoEntity e;
            for (var i = _entityGens.Len () - 1; i >= 0; i--) {
                if (_entityGens.Get (i) > 0) {
                    e._id = i;
                    DelEntity (e);
                }
            }
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
            for (var ii = _listeners.Len () - 1; ii >= 0; ii--) {
                _listeners.Get (ii).OnWorldDestroyed ();
            }
#endif
        }
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
        public void AddEventListener (IProtoEventListener el) {
            _listeners.Add (el);
        }

        public void RemoveEventListener (IProtoEventListener el) {
            for (int i = 0, iMax = _listeners.Len (); i < iMax; i++) {
                if (_listeners.Get (i) == el) {
                    _listeners.RemoveAt (i);
                    break;
                }
            }
        }
#endif

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsAlive () {
            return !_destroyed;
        }

        public ProtoEntity NewEntity () {
            ProtoEntity entity;
            if (_recycled.Len () > 0) {
                // есть сущности для переиспользования.
                entity = _recycled.RemoveLast ();
                _entityGens.Get (entity._id) *= -1;
            } else {
                // новая сущность.
                entity._id = _entityGens.Len ();
                for (var i = 0; i < _entityMaskLen; i++) {
                    _entityMasks.Add (0UL);
                }
                var oldCap = _entityGens.Cap ();
                _entityGens.Add (1);
                if (oldCap != _entityGens.Cap ()) {
                    var cap = _entityGens.Cap ();
                    for (int i = 0, iMax = _pools.Len (); i < iMax; i++) {
                        _pools.Get (i).Resize (cap);
                    }
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
                    for (int ii = 0, iMax = _listeners.Len (); ii < iMax; ii++) {
                        _listeners.Get (ii).OnWorldResized (cap);
                    }
#endif
                }
            }
#if DEBUG
            _leaked.Add (entity);
#endif
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
            for (int ii = 0, iMax = _listeners.Len (); ii < iMax; ii++) {
                _listeners.Get (ii).OnEntityCreated (entity);
            }
#endif
            return entity;
        }

        public void DelEntity (ProtoEntity entity) {
            ref var gen = ref _entityGens.Get (entity._id);
            if (gen < 0) {
                return;
            }
            var id = EntityMask.GetMinIndex (_entityMasks, _entityMaskLen, entity);
            if (id >= 0) {
                while (id >= 0) {
                    _pools.Get (id).Del (entity);
                    id = EntityMask.GetMinIndex (_entityMasks, _entityMaskLen, entity);
                }
            } else {
                gen = (short) (gen == short.MaxValue ? -1 : -(gen + 1));
                _recycled.Add (entity);
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
                for (int ii = 0, iMax = _listeners.Len (); ii < iMax; ii++) {
                    _listeners.Get (ii).OnEntityDestroyed (entity);
                }
#endif
            }
        }

        public void CopyEntity (ProtoEntity srcEntity, ProtoEntity dstEntity) {
            Array.Copy (_entityMasks.Data (), srcEntity._id * _entityMaskLen, _copyBuf.Data (), 0, _entityMaskLen);
            ProtoEntity bufE = default;
            var id = EntityMask.GetMinIndex (_copyBuf, _entityMaskLen, bufE);
            while (id >= 0) {
                _pools.Get (id).Copy (srcEntity, dstEntity);
                EntityMask.Unset (_copyBuf, _entityMaskLen, bufE, (ushort) id);
                id = EntityMask.GetMinIndex (_copyBuf, _entityMaskLen, bufE);
            }
        }

        public IProtoAspect Aspect (Type aType) {
#if DEBUG
            if (!_aspects.ContainsKey (aType)) { throw new Exception ($"не могу получить аспект \"{aType.Name}\", его сперва надо зарегистрировать в мире"); }
#endif
            return _aspects[aType];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Dictionary<Type, IProtoAspect> Aspects () {
            return _aspects;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Slice<ulong> EntityMasks () {
            return _entityMasks;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Slice<short> EntityGens () {
            return _entityGens;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Slice<IProtoPool> Pools () {
            return _pools;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ushort ComponentsCount (ProtoEntity entity) {
            return EntityMask.Len (_entityMasks, _entityMaskLen, entity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ushort EntityMaskItemLen () => _entityMaskLen;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int EntityMaskOffset (ProtoEntity entity) => entity._id * _entityMaskLen;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void SetEntityMaskBit (ProtoEntity entity, ushort index) {
            EntityMask.Set (_entityMasks, _entityMaskLen, entity, index);
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
            for (int i = 0, iMax = _listeners.Len (); i < iMax; i++) {
                _listeners.Get (i).OnEntityChanged (entity, index, true);
            }
#endif
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void UnsetEntityMaskBit (ProtoEntity entity, ushort index) {
            EntityMask.Unset (_entityMasks, _entityMaskLen, entity, index);
#if DEBUG || LEOECSPROTO_WORLD_EVENTS
            for (int i = 0, iMax = _listeners.Len (); i < iMax; i++) {
                _listeners.Get (i).OnEntityChanged (entity, index, false);
            }
#endif
            if (EntityMask.IsEmpty (_entityMasks, _entityMaskLen, entity)) {
                DelEntity (entity);
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool EntityCompatibleWith (ProtoEntity entity, Slice<ulong> inc) {
            return EntityMask.CompatibleWith (_entityMasks, _entityMaskLen, entity, inc);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool EntityCompatibleWithAndWithout (ProtoEntity entity, Slice<ulong> inc, Slice<ulong> exc) {
            return EntityMask.CompatibleWithAndWithout (_entityMasks, _entityMaskLen, entity, inc, exc);
        }

        public void AddAspect (IProtoAspect proto) {
#if DEBUG
            if (_inited) { throw new Exception ($"не могу добавить аспект \"{proto.GetType ().Name}\", мир уже инициализирован"); }
            if (_aspects.ContainsKey (proto.GetType ())) { throw new Exception ($"не могу добавить аспект \"{proto.GetType ().Name}\", он уже существует"); }
#endif
            _aspects[proto.GetType ()] = proto;
        }

        public bool HasAspect (Type aType) {
            return _aspects.ContainsKey (aType);
        }

        public void AddPool (IProtoPool pool) {
            var cType = pool.ItemType ();
#if DEBUG
            if (_inited) { throw new Exception ($"не могу добавить пул для компонента \"{cType.Name}\", мир уже инициализирован"); }
            if (_poolsMap.ContainsKey (cType)) { throw new Exception ($"не могу добавить пул для компонента \"{cType.Name}\", он уже существует"); }
#endif
            pool.Init ((ushort) _pools.Len (), this);
            _pools.Add (pool);
            _poolsMap[cType] = pool;
        }

        public IProtoPool Pool (Type cType) {
#if DEBUG
            if (!_poolsMap.ContainsKey (cType)) { throw new Exception ($"не могу получить пул для компонента \"{cType.Name}\", его сперва надо зарегистрировать в аспекте"); }
#endif
            return _poolsMap[cType];
        }

        public bool HasPool (Type cType) {
            return _poolsMap.ContainsKey (cType);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public short EntityGen (ProtoEntity entity) {
            return _entityGens.Get (entity._id);
        }

        public struct Config {
            public int Entities;
            public int RecycledEntities;
            public int Aspects;
            public int Pools;

            internal const int EntitiesDefault = 256;
            internal const int RecycledEntitiesDefault = 256;
            internal const int AspectsDefault = 4;
            internal const int PoolsDefault = 256;
        }
    }
}
