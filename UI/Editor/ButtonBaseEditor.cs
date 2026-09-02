using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Button 기본 인스펙터가 서브클래스 필드를 숨기므로 ButtonBase 전용으로 추가 필드를 그린다.
    /// </summary>
    [CustomEditor(typeof(ButtonBase), true)]
    [CanEditMultipleObjects]
    public class ButtonBaseEditor : ButtonEditor
    {
        private SerializedProperty _buttonSoundPath;

        protected override void OnEnable()
        {
            base.OnEnable();
            _buttonSoundPath = serializedObject.FindProperty("_buttonSoundPath");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_buttonSoundPath, new GUIContent("Button Sound Path"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
