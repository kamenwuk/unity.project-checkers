// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

using System;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsProto {
    public interface IProtoPool {
        void Init (ushort id, ProtoWorld host);
        Type ItemType ();
        ushort Id ();
        ProtoWorld World ();
        bool Has (ProtoEntity entity);
        void Del (ProtoEntity entity);
        void AddRaw (ProtoEntity entity);
        object Raw (ProtoEntity entity);
        void SetRaw (ProtoEntity entity, object dataRaw);
        void AddBlocker (int amount);
        void Resize (int cap);
        int Len ();
        ProtoEntity[] Entities ();
        void Copy (ProtoEntity srcEntity, ProtoEntity dstEntity);
    }

    public interface IProtoAutoReset<T> where T : struct {
        void AutoReset (ref T c);
    }

    public interface IProtoAutoCopy<T> where T : struct {
        void AutoCopy (ref T src, ref T dst);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class ProtoPool<T> : IProtoPool where T : struct {
        ushort _id;
        ProtoWorld _world;
        ProtoEntity[] _dense;
        int[] _sparse;
        T[] _data;
        int _len;
        Type _itemType;
        AutoResetHandler _autoResetHandler;
        AutoCopyHandler _autoCopyHandler;
        T _default = default;
#if DEBUG
        int _blockers;
#endif

        public ProtoPool () : this (128) { }

        public ProtoPool (int capacity) {
            _dense = new ProtoEntity[capacity];
            _data = new T[capacity];
            _len = 0;
            _itemType = typeof (T);
            var arType = typeof (IProtoAutoReset<T>);
            if (arType.IsAssignableFrom (_itemType)) {
                var searchMethod = arType.GetMethod (nameof (IProtoAutoReset<T>.AutoReset));
                foreach (var m in _itemType.GetInterfaceMap (arType).InterfaceMethods) {
                    if (m == searchMethod) {
                        _autoResetHandler =
                            (AutoResetHandler) Delegate.CreateDelegate (typeof (AutoResetHandler), _default, m!);
                        break;
                    }
                }
            }
            var acType = typeof (IProtoAutoCopy<T>);
            if (acType.IsAssignableFrom (_itemType)) {
                var searchMethod = acType.GetMethod (nameof (IProtoAutoCopy<T>.AutoCopy));
                foreach (var m in _itemType.GetInterfaceMap (acType).InterfaceMethods) {
                    if (m == searchMethod) {
                        _autoCopyHandler =
                            (AutoCopyHandler) Delegate.CreateDelegate (typeof (AutoCopyHandler), _default, m!);
                        break;
                    }
                }
            }
#if DEBUG
            _blockers = 0;
#endif
        }

        void IProtoPool.Init (ushort id, ProtoWorld world) {
#if DEBUG
            if (_world != null) { throw new Exception ($"пул компонентов \"{_itemType.Name}\" уже привязан к миру"); }
#endif
            _id = id;
            _world = world;
            _sparse = new int[_world.EntityGens ().Cap ()];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Type ItemType () => _itemType;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ushort Id () => _id;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoWorld World () => _world;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Get (ProtoEntity entity) {
#if DEBUG
            if (_world.EntityGens ().Get (entity._id) < 0) { throw new Exception ("не могу получить доступ к удаленной сущности"); }
            if (_sparse[entity._id] == 0) { throw new Exception ($"компонент \"{_itemType.Name}\" отсутствует на сущности"); }
#endif
            return ref _data[_sparse[entity._id] - 1];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Has (ProtoEntity entity) {
#if DEBUG
            if (_world.EntityGens ().Get (entity._id) < 0) { throw new Exception ("не могу получить доступ к удаленной сущности"); }
#endif
            return _sparse[entity._id] > 0;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Add (ProtoEntity entity) {
#if DEBUG
            if (Has (entity)) { throw new Exception ("не могу добавить компонент, он уже существует"); }
            if (_blockers > 1) { throw new Exception ($"нельзя изменить пул компонентов \"{_itemType.Name}\", он находится в режиме \"только чтение\" из-за множественного доступа"); }
#endif
            if (_dense.Length == _len) {
                Array.Resize (ref _dense, _len << 1);
                Array.Resize (ref _data, _len << 1);
            }

            var idx = _len;
            _len++;
            _dense[idx] = entity;
            _sparse[entity._id] = _len;

            _autoResetHandler?.Invoke (ref _data[idx]);

            _world.SetEntityMaskBit (entity, _id);

            return ref _data[idx];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Del (ProtoEntity entity) {
#if DEBUG
            if (_world.EntityGens ().Get (entity._id) < 0) { throw new Exception ("не могу получить доступ к удаленной сущности"); }
            if (_blockers > 1) { throw new Exception ($"нельзя изменить пул компонентов \"{_itemType.Name}\", он находится в режиме \"только чтение\" из-за множественного доступа"); }
#endif
            var idx = _sparse[entity._id] - 1;
            if (idx >= 0) {
                _sparse[entity._id] = 0;
                _len--;

                if (_autoResetHandler != null) {
                    _autoResetHandler.Invoke (ref _data[idx]);
                } else {
                    _data[idx] = default;
                }

                if (idx < _len) {
                    _dense[idx] = _dense[_len];
                    _sparse[_dense[idx]._id] = idx + 1;
                    (_data[idx], _data[_len]) = (_data[_len], _data[idx]);
                }

                _world.UnsetEntityMaskBit (entity, _id);
            }
        }

        public void Resize (int cap) {
            Array.Resize (ref _sparse, cap);
        }

        public void Copy (ProtoEntity srcEntity, ProtoEntity dstEntity) {
#if DEBUG
            if (_world.EntityGens ().Get (srcEntity._id) < 0) { throw new Exception ("не могу получить доступ к удаленной исходной сущности"); }
            if (_world.EntityGens ().Get (dstEntity._id) < 0) { throw new Exception ("не могу получить доступ к удаленной целевой сущности"); }
            if (_blockers > 1) { throw new Exception ($"нельзя изменить пул компонентов \"{_itemType.Name}\", он находится в режиме \"только чтение\" из-за множественного доступа"); }
#endif
            if (Has (srcEntity)) {
                ref var srcData = ref Get (srcEntity);
                if (!Has (dstEntity)) {
                    Add (dstEntity);
                }
                ref var dstData = ref Get (dstEntity);
                if (_autoCopyHandler != null) {
                    _autoCopyHandler.Invoke (ref srcData, ref dstData);
                } else {
                    dstData = srcData;
                }
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int Len () => _len;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ProtoEntity[] Entities () => _dense;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public T[] Data () => _data;

        void IProtoPool.AddRaw (ProtoEntity entity) {
            Add (entity);
        }

        object IProtoPool.Raw (ProtoEntity entity) => Get (entity);

        void IProtoPool.SetRaw (ProtoEntity entity, object dataRaw) {
#if DEBUG
            if (dataRaw == null || dataRaw.GetType () != _itemType) { throw new Exception ($"неправильные данные для использования в качестве компонента \"{_itemType.Name}\""); }
#endif
            Get (entity) = (T) dataRaw;
        }

        void IProtoPool.AddBlocker (int amount) {
#if DEBUG
            _blockers += amount;
            if (_blockers < 0) { throw new Exception ("ошибочный баланс пользователей пула при попытке освобождения"); }
#endif
        }

        delegate void AutoResetHandler (ref T component);

        delegate void AutoCopyHandler (ref T srcComponent, ref T dstComponent);
    }
}
