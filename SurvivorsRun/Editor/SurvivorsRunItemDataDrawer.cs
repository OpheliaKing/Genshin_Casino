using UnityEditor;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// ItemType / AttackPattern에 따라 SurvivorsRunItemData의 관련 필드만 인스펙터에 표시한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SurvivorsRunItemData))]
    public class SurvivorsRunItemDataDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            height += GetFieldsHeight(property);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var tid = property.FindPropertyRelative("_tid");
            var header = string.IsNullOrEmpty(tid.stringValue) ? label.text : $"{label.text}  [{tid.stringValue}]";
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, header, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = position.y + EditorGUIUtility.singleLineHeight + VerticalSpacing;
                DrawFields(ref y, position.x, position.width, property);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static float GetFieldsHeight(SerializedProperty property)
        {
            var line = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            var count = 5; // tid, name, description, icon, itemType

            var itemType = (SURVIVORSRUN_ITEM_TYPE)property.FindPropertyRelative("_itemType").intValue;
            if (!ShowsCombatFields(itemType))
                return count * line;

            count += 2; // attackPattern + maxStack
            var pattern = (SURVIVORSRUN_ATTACK_PATTERN)property.FindPropertyRelative("_attackPattern").intValue;
            if (pattern == SURVIVORSRUN_ATTACK_PATTERN.NONE)
                return count * line;

            if (pattern == SURVIVORSRUN_ATTACK_PATTERN.BUFF)
            {
                count++; // fireCooldown
                var effects = property.FindPropertyRelative("_effects");
                return count * line + EditorGUI.GetPropertyHeight(effects, true) + VerticalSpacing;
            }

            count += CountWeaponCombatFields(itemType, pattern);
            return count * line;
        }

        private static void DrawFields(ref float y, float x, float width, SerializedProperty property)
        {
            DrawProp(ref y, x, width, property, "_tid");
            DrawProp(ref y, x, width, property, "_name");
            DrawProp(ref y, x, width, property, "_description");
            DrawProp(ref y, x, width, property, "_icon");
            DrawProp(ref y, x, width, property, "_itemType");

            var itemType = (SURVIVORSRUN_ITEM_TYPE)property.FindPropertyRelative("_itemType").intValue;
            if (!ShowsCombatFields(itemType))
                return;

            DrawProp(ref y, x, width, property, "_attackPattern");
            DrawProp(ref y, x, width, property, "_maxStack");

            var pattern = (SURVIVORSRUN_ATTACK_PATTERN)property.FindPropertyRelative("_attackPattern").intValue;
            if (pattern == SURVIVORSRUN_ATTACK_PATTERN.NONE)
                return;

            if (pattern == SURVIVORSRUN_ATTACK_PATTERN.BUFF)
            {
                DrawProp(ref y, x, width, property, "_fireCooldown");
                DrawEffectsList(ref y, x, width, property);
                return;
            }

            DrawProp(ref y, x, width, property, "_damagePrefabPath");
            DrawProp(ref y, x, width, property, "_baseDamage");
            DrawProp(ref y, x, width, property, "_hitCooldown");

            var showFireCooldown = itemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE
                || itemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE
                || pattern == SURVIVORSRUN_ATTACK_PATTERN.PULSE
                || pattern == SURVIVORSRUN_ATTACK_PATTERN.PROJECTILE;
            if (showFireCooldown)
                DrawProp(ref y, x, width, property, "_fireCooldown");

            switch (pattern)
            {
                case SURVIVORSRUN_ATTACK_PATTERN.ORBIT:
                    DrawProp(ref y, x, width, property, "_baseObjectCount");
                    break;
                case SURVIVORSRUN_ATTACK_PATTERN.PROJECTILE:
                    DrawProp(ref y, x, width, property, "_projectileSpeed");
                    DrawProp(ref y, x, width, property, "_projectileLifetime");
                    DrawProp(ref y, x, width, property, "_maxRange");
                    break;
            }

            DrawProp(ref y, x, width, property, "_hitEffectPrefabPath");
            DrawProp(ref y, x, width, property, "_hitEffectLifetime");
        }

        private static void DrawEffectsList(ref float y, float x, float width, SerializedProperty root)
        {
            var effects = root.FindPropertyRelative("_effects");
            if (effects == null)
                return;

            var height = EditorGUI.GetPropertyHeight(effects, true);
            var rect = new Rect(x, y, width, height);
            EditorGUI.PropertyField(rect, effects, true);
            y += height + VerticalSpacing;
        }

        private static int CountWeaponCombatFields(SURVIVORSRUN_ITEM_TYPE itemType, SURVIVORSRUN_ATTACK_PATTERN pattern)
        {
            // damagePrefab, baseDamage, hitCooldown, hitEffectPath, hitEffectLifetime
            var count = 5;

            var showFireCooldown = itemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE
                || itemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE
                || pattern == SURVIVORSRUN_ATTACK_PATTERN.PULSE
                || pattern == SURVIVORSRUN_ATTACK_PATTERN.PROJECTILE;
            if (showFireCooldown)
                count++;

            switch (pattern)
            {
                case SURVIVORSRUN_ATTACK_PATTERN.ORBIT:
                    count += 1;
                    break;
                case SURVIVORSRUN_ATTACK_PATTERN.PROJECTILE:
                    count += 3;
                    break;
            }

            return count;
        }

        private static bool ShowsCombatFields(SURVIVORSRUN_ITEM_TYPE itemType)
        {
            return itemType == SURVIVORSRUN_ITEM_TYPE.WEAPON
                || itemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE
                || itemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE;
        }

        private static void DrawProp(ref float y, float x, float width, SerializedProperty root, string relativeName)
        {
            var prop = root.FindPropertyRelative(relativeName);
            if (prop == null)
                return;

            var rect = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, prop);
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }
    }
}
