using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pickle.Editor
{
    public class PickleSettings : ScriptableObject
    {
        private static PickleSettings m_instance;
        private static PickleSettings _instance
        {
            get
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(PickleSettings)}");
                if (guids.Length > 0)
                {
                    m_instance = AssetDatabase.LoadAssetAtPath<PickleSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                return m_instance;
            }
        }

        [InitializeOnLoadMethod]
        private static void InitializePickleSettings()
        {
            if (!_instance)
            {
                EditorUtility.DisplayDialog("Pickle installer", "Pickle settings asset does not exist, pick a location in project to create it.", "Ok");

                var path = EditorUtility.SaveFilePanelInProject("Pickle installer", "PickleSettings", "asset", "");
            
                if (string.IsNullOrEmpty(path) == false)
                {
                    m_instance = ScriptableObject.CreateInstance<PickleSettings>();
                    AssetDatabase.CreateAsset(m_instance, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        [SerializeField] 
        private List<string> _defaultToWindowTypeNames = new List<string>() { typeof(Sprite).FullName, typeof(Texture2D).FullName, typeof(Mesh).FullName };

        internal static PickerType GetDefaultPickerType(System.Type fieldType)
        {
            var instance = _instance;
            if (!instance)
                return PickerType.Dropdown;

            return instance._defaultToWindowTypeNames.Contains(fieldType.FullName) ? PickerType.Window : PickerType.Dropdown;
        }

        [CustomEditor(typeof(PickleSettings))]
        public class PickleSettingsEditor : UnityEditor.Editor
        {
            private const string PICKLE_IS_DEFAULT = "DEFAULT_TO_PICKLE";

            public override void OnInspectorGUI()
            {
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out var defines);

                EditorGUILayout.Space();

                var pickleDefineIndex = System.Array.IndexOf(defines, PICKLE_IS_DEFAULT);
                var isPickleDefault = pickleDefineIndex >= 0;
                var toggleButtonLabel = isPickleDefault ? "Unset Pickle as default picker" : "Set Pickle as default picker";

                var buttonStyle = (GUIStyle)"AC Button";
                var size = buttonStyle.CalcSize(new GUIContent(toggleButtonLabel));
                
                var buttonRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(size.y));
                buttonRect = Rect.MinMaxRect(
                    buttonRect.center.x - size.x * 0.5f,
                    buttonRect.center.y - size.y * 0.5f,
                    buttonRect.center.x + size.x * 0.5f,
                    buttonRect.center.y + size.y * 0.5f
                );

                if (GUI.Button(buttonRect, toggleButtonLabel, buttonStyle))
                {

                    if (isPickleDefault)
                    {
                        defines[pickleDefineIndex] = defines[defines.Length - 1];
                        System.Array.Resize(ref defines, defines.Length - 1);
                    }
                    else
                    {
                        System.Array.Resize(ref defines, defines.Length + 1);
                        defines[defines.Length - 1] = PICKLE_IS_DEFAULT;

                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);

                    Repaint();
                }

                base.OnInspectorGUI();
            }
        }
    }
}
