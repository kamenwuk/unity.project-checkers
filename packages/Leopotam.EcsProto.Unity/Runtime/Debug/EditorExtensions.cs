// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023-2024 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Leopotam.EcsProto.Unity {
    public static class EditorExtensions {
        static readonly Dictionary<Type, string> _namesCache = new ();

        public static string GetCleanTypeName (Type type) {
            if (!_namesCache.TryGetValue (type, out var name)) {
                if (!type.IsGenericType) {
                    name = type.Name;
                } else {
                    var constraints = "";
                    foreach (var constraint in type.GetGenericArguments ()) {
                        if (constraints.Length > 0) { constraints += ", "; }
                        constraints += GetCleanTypeName (constraint);
                    }
                    var genericIndex = type.Name.LastIndexOf ("`", StringComparison.Ordinal);
                    var typeName = genericIndex == -1
                        ? type.Name
                        : type.Name.Substring (0, genericIndex);
                    name = $"{typeName}<{constraints}>";
                }
                _namesCache[type] = name;
            }
            return name;
        }
    }
}
#endif
