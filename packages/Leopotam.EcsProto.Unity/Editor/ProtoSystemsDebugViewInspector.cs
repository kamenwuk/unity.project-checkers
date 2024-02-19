// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–
// Коммерческая лицензия подписчика
// (c) 2023-2024 Leopotam <leopotam@yandex.ru>
// –‒–––‒‒‒–‒––––––‒––‒–‒–‒–––‒–‒‒‒–‒–‒–‒––––‒‒–‒–––‒‒–––‒‒––‒‒––‒––––‒–‒‒–

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leopotam.EcsProto.Unity.Editor {
    [CustomEditor (typeof (ProtoSystemsDebugView))]
    sealed class ProtoSystemsDebugViewInspector : UnityEditor.Editor {
        static bool _preInitOpened;
        static bool _initOpened;
        static bool _runOpened;
        static bool _postRunOpened;
        static bool _destroyOpened;
        static bool _postDestroyOpened;
        static Dictionary<int, string> _formattedTime;

        static string[] _labels = {
            "PreInit системы",
            "Init системы",
            "Run системы",
            "PostRun системы",
            "Destroy системы",
            "PostDestroy системы"
        };

        const string NoDataLabel = "<???>";

        public override void OnInspectorGUI () {
            var view = (ProtoSystemsDebugView) target;
            var savedState = GUI.enabled;
            GUI.enabled = true;
            RenderLabeledList (ProtoSystems.BenchType.PreInit, view.PreInitSystems, view.Systems, ref _preInitOpened);
            RenderLabeledList (ProtoSystems.BenchType.Init, view.InitSystems, view.Systems, ref _initOpened);
            RenderLabeledList (ProtoSystems.BenchType.Run, view.RunSystems, view.Systems, ref _runOpened);
            RenderLabeledList (ProtoSystems.BenchType.PostRun, view.PostRunSystems, view.Systems, ref _postRunOpened);
            RenderLabeledList (ProtoSystems.BenchType.Destroy, view.DestroySystems, view.Systems, ref _destroyOpened);
            RenderLabeledList (ProtoSystems.BenchType.PostDestroy, view.PostDestroySystems, view.Systems, ref _postDestroyOpened);
            GUI.enabled = savedState;
            EditorUtility.SetDirty (target);
        }

        void RenderLabeledList (ProtoSystems.BenchType sysType, Slice<ProtoSystemsDebugView.SysItem> list, ProtoSystems benchSystems, ref bool opened) {
            if (list.Len () > 0) {
                opened = EditorGUILayout.BeginFoldoutHeaderGroup (opened, _labels[(int) sysType]);
                if (opened) {
                    EditorGUI.indentLevel++;
                    var savedWidth = EditorGUIUtility.labelWidth;
                    for (var i = 0; i < list.Len (); i++) {
                        ref var item = ref list.Get (i);
#if DEBUG || LEOECSPROTO_SYSTEM_BENCHES
                        var time = benchSystems.Bench (item.Index, sysType);
                        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 84f;
                        EditorGUILayout.LabelField (item.Name, FormattedTime (time));
#else
                        EditorGUILayout.LabelField (item.Name, NoDataLabel);
#endif
                    }
                    EditorGUIUtility.labelWidth = savedWidth;
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup ();
                EditorGUILayout.Space ();
            }
        }

        static string FormattedTime (int time) {
            if (time < 0) {
                return NoDataLabel;
            }
            _formattedTime ??= new Dictionary<int, string> (512);
            if (!_formattedTime.TryGetValue (time, out var timeStr)) {
                timeStr = $"{time * 0.01f:F2}мс";
                _formattedTime[time] = timeStr;
            }
            return timeStr;
        }
    }
}
