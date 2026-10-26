using UnityEditor;
using UnityEngine;

namespace SHIN.Editor
{
    public class ResourceLoadTraceWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "보유 중", "해제됨" };

        private int _tab;
        private Vector2 _scroll;

        [MenuItem("SHIN/Debug/Resource Load Trace")]
        public static void Open()
        {
            GetWindow<ResourceLoadTraceWindow>("Resource Load Trace");
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("ResourceManager를 지난 로드만 표시합니다.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("기록 지우기"))
                ResourceManager.ClearLoadTraces();

            var traces = ResourceManager.LoadTraces;
            var heldCount = 0;
            var releasedCount = 0;
            for (var i = 0; i < traces.Count; i++)
            {
                if (IsHeld(traces[i]))
                    heldCount++;
                else
                    releasedCount++;
            }

            Tabs[0] = $"보유 중 ({heldCount})";
            Tabs[1] = $"해제됨 ({releasedCount})";
            _tab = GUILayout.Toolbar(_tab, Tabs);

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var shown = 0;
            for (var i = traces.Count - 1; i >= 0; i--)
            {
                var entry = traces[i];
                if (IsHeld(entry) != (_tab == 0))
                    continue;

                DrawEntry(entry);
                shown++;
            }

            if (shown == 0)
            {
                EditorGUILayout.LabelField(_tab == 0
                    ? "지금 들고 있는 핸들이 없습니다."
                    : "해제한 기록이 없습니다.");
            }

            EditorGUILayout.EndScrollView();
        }

        private static bool IsHeld(ResourceTraceEntry entry)
        {
            return entry.State == ResourceTraceState.Loading || entry.State == ResourceTraceState.Held;
        }

        private static void DrawEntry(ResourceTraceEntry entry)
        {
            var elapsed = entry.HasFinished
                ? entry.FinishedAt - entry.StartedAt
                : Time.realtimeSinceStartup - entry.StartedAt;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{entry.State}  {entry.Kind}  {elapsed:0.00}s");
            EditorGUILayout.LabelField(entry.Key, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }
    }
}
