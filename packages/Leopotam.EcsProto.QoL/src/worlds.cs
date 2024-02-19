// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023-2024 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsProto.QoL {
    public abstract class ProtoAspectInject : IProtoAspect {
        static readonly Type _aspectType = typeof (IProtoAspect);
        static readonly Type _poolType = typeof (IProtoPool);
        static readonly Type _itType = typeof (IProtoIt);

        List<IProtoAspect> _aspects;
        List<IProtoPool> _pools;
        List<Type> _poolTypes;
        List<IProtoIt> _its;
        ProtoWorld _world;
        ProtoIt _it;

        public virtual void Init (ProtoWorld world) {
            world.AddAspect (this);
            _world = world;
            foreach (var f in GetType ().GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (f.IsStatic) { continue; }
                // аспекты.
                if (_aspectType.IsAssignableFrom (f.FieldType)) {
                    if (world.HasAspect (f.FieldType)) {
                        f.SetValue (this, _world.Aspect (f.FieldType));
                        continue;
                    }
                    var aspect = (IProtoAspect) f.GetValue (this);
#if DEBUG
                    if (aspect == null && f.FieldType.GetConstructor (Type.EmptyTypes) == null) {
                        throw new Exception ($"аспект \"{f.FieldType.Name}\" должен иметь конструктор по умолчанию, либо экземпляр должен быть создан заранее");
                    }
#endif
                    aspect ??= (IProtoAspect) Activator.CreateInstance (f.FieldType);
                    _aspects ??= new (4);
                    _aspects.Add (aspect);
                    aspect.Init (world);
                    f.SetValue (this, aspect);
                    continue;
                }
                // пулы.
                if (_poolType.IsAssignableFrom (f.FieldType)) {
                    var pool = (IProtoPool) f.GetValue (this);
#if DEBUG
                    if (pool == null && f.FieldType.GetConstructor (Type.EmptyTypes) == null) {
                        throw new Exception ($"пул \"{f.FieldType.Name}\" должен иметь конструктор по умолчанию, либо экземпляр должен быть создан заранее");
                    }
#endif
                    // уменьшаем размер аллокаций при запросе типа компонента пула.
                    if (f.FieldType.IsGenericType) {
                        var poolConstraints = f.FieldType.GetGenericArguments ();
                        if (poolConstraints.Length == 1) {
                            if (world.HasPool (poolConstraints[0])) {
                                pool = world.Pool (poolConstraints[0]);
                            }
                        }
                    }
                    if (pool == null) {
                        pool = (IProtoPool) Activator.CreateInstance (f.FieldType);
                        var itemType = pool.ItemType ();
                        if (world.HasPool (itemType)) {
                            pool = world.Pool (itemType);
                        } else {
                            world.AddPool (pool);
                        }
                    }
                    _pools ??= new (8);
                    _poolTypes ??= new (8);
                    _pools.Add (pool);
                    _poolTypes.Add (pool.ItemType ());
                    f.SetValue (this, pool);
                    continue;
                }
                // итераторы.
                if (_itType.IsAssignableFrom (f.FieldType)) {
                    var it = (IProtoIt) f.GetValue (this);
#if DEBUG
                    if (it == null) { throw new Exception ($"итератор \"{f.FieldType.Name}\" должен быть создан заранее"); }
#endif
                    _its ??= new (8);
                    _its.Add (it);
                }
            }
        }

        public virtual void PostInit () {
            if (_aspects != null) {
                foreach (var aspect in _aspects) {
                    aspect.PostInit ();
                }
            }
            if (_its != null) {
                foreach (var it in _its) {
                    it.Init (_world);
                }
            }
            if (_poolTypes != null) {
                _it = new ProtoIt (_poolTypes.ToArray ());
                _it.Init (_world);
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoWorld World () {
            return _world;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoIt Iter () {
#if DEBUG
            if (_it == null) { throw new Exception ("в аспекте нет пулов для итерирования по ним"); }
#endif
            return _it;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoEntity NewEntity () {
            var e = _world.NewEntity ();
            if (_poolTypes != null) {
                foreach (var pool in _pools) {
                    pool.AddRaw (e);
                }
            }
            return e;
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public static class ProtoWorldExtensions {
        public static void GetAliveEntities (this ProtoWorld world, Slice<int> result) {
            result.Clear (false);
            var gens = world.EntityGens ();
            for (int i = 0, iMax = gens.Len (); i < iMax; i++) {
                if (gens.Get (i) > 0) {
                    result.Add (i);
                }
            }
        }

        public static void GetComponents (this ProtoWorld world, ProtoEntity entity, Slice<object> result) {
            result.Clear (false);
            if (world.EntityGen (entity) < 0) { return; }
            var pools = world.Pools ();
            var maskData = world.EntityMasks ().Data ();
            var maskLen = world.EntityMaskItemLen ();
            var maskOffset = world.EntityMaskOffset (entity);
            for (int i = 0, offset = 0; i < maskLen; i++, offset += 64, maskOffset++) {
                var v = maskData[maskOffset];
                for (var j = 0; v != 0 && j < 64; j++) {
                    var mask = 1UL << j;
                    if ((v & mask) != 0) {
                        v &= ~mask;
                        result.Add (pools.Get (offset + j).Raw (entity));
                    }
                }
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static T Aspect<T> (this ProtoWorld world) where T : IProtoAspect {
            return (T) world.Aspect (typeof (T));
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static IProtoPool Pool<T> (this ProtoWorld world) where T : struct {
            return world.Pool (typeof (T));
        }
    }
}
