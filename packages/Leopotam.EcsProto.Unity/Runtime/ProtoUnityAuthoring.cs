// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023-2024 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

using System;
using System.Collections.Generic;
using Leopotam.EcsProto.QoL;
using UnityEngine;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsProto.Unity {
    public interface IProtoUnityAuthoring {
        void Authoring (in ProtoPackedEntityWithWorld entity, GameObject go);
    }

    public sealed class ProtoUnityAuthoringAttribute : Attribute {
        public readonly string Name;

        public ProtoUnityAuthoringAttribute () : this (default) { }

        public ProtoUnityAuthoringAttribute (string name) {
            Name = name;
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    [DefaultExecutionOrder (10000)]
    public class ProtoUnityAuthoring : MonoBehaviour {
        [SerializeField] string _worldName;
        [SerializeField] DestroyType _destroyAfterAuthoring = DestroyType.GameObject;
        [SerializeReference, HideInInspector] public List<object> Components;

        ProtoPackedEntityWithWorld _packed;

        void Start () {
#if UNITY_EDITOR
            if (Components == null || Components.Count == 0) { throw new Exception ($"[ProtoUnityAuthoring] Пустой список компонентов"); }
#endif
            _worldName = !string.IsNullOrEmpty (_worldName) ? _worldName : default;
            var world = ProtoUnityWorlds.Get (_worldName);
            var entity = world.NewEntity ();
            var packed = world.PackEntityWithWorld (entity);
            var go = gameObject;
            foreach (var c in Components) {
#if UNITY_EDITOR
                if (c == null) { throw new Exception ($"[ProtoUnityAuthoring] Обнаружен сломанный компонент"); }
#endif
                if (c is IProtoUnityAuthoring linkE) {
                    linkE.Authoring (packed, go);
                }
                var pool = world.Pool (c.GetType ());
                pool.AddRaw (entity);
                pool.SetRaw (entity, c);
            }

            switch (_destroyAfterAuthoring) {
                case DestroyType.Component:
                    Destroy (this);
                    return;
                case DestroyType.GameObject:
                    Destroy (gameObject);
                    return;
            }

            _packed = world.PackEntityWithWorld (entity);
        }

        public ProtoPackedEntityWithWorld Entity () {
            return _packed;
        }

        public enum DestroyType {
            None,
            Component,
            GameObject
        }
    }
}
