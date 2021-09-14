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

        public static AutoPickMode DefaultAutoPickMode => _instance ? _instance._defaultAutoPickMode : AutoPickMode.None;

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

        [Header("Defaults")]
        [SerializeField] private PickerType _defaultPickerType = PickerType.Dropdown;
        [SerializeField] private int _defaultObjectProvider = (int)(ObjectProviderType.Assets | ObjectProviderType.Scene);
        [SerializeField] private AutoPickMode _defaultAutoPickMode = AutoPickMode.None;

        [Header("Open in window types")]
        [SerializeField] 
        private List<string> _defaultToWindowTypeNames = new List<string>() { typeof(Sprite).FullName, typeof(Texture2D).FullName, typeof(Mesh).FullName };

        internal static PickerType GetDefaultPickerType(System.Type fieldType)
        {
            var instance = _instance;
            if (!instance)
                return PickerType.Dropdown;

            return instance._defaultToWindowTypeNames.Contains(fieldType.FullName) ? PickerType.Window : instance._defaultPickerType;
        }

        public static ObjectProviderType GetDefaultProviderType()
        {
            var instance = _instance;
            if (!instance)
                return (ObjectProviderType.Scene | ObjectProviderType.Assets);

            return (ObjectProviderType)instance._defaultObjectProvider;
        }

        [CustomEditor(typeof(PickleSettings))]
        public class PickleSettingsEditor : UnityEditor.Editor
        {
            private const string PICKLE_IS_DEFAULT = "DEFAULT_TO_PICKLE";
            private const string PICKLE_IN_ROOT_NAMESPACE = "PICKLE_IN_ROOT_NAMESPACE";

            private SerializedProperty _defaultPickerTypeProp;
            private SerializedProperty _defaultProviderTypeProp;
            private SerializedProperty _defaultAutoPickModeProp;
            private SerializedProperty _defaultToWindowTypesProp;

            private string[] _providerMaskDisplayNames;

            public override void OnInspectorGUI()
            {
                base.DrawHeader();

                FetchProperties();

                EditorGUILayout.PropertyField(_defaultPickerTypeProp);
                EditorGUILayout.PropertyField(_defaultAutoPickModeProp);

                int providerTypeMask = _defaultProviderTypeProp.intValue;

                var newTypeMask = EditorGUILayout.MaskField("Default Provider Type", providerTypeMask, _providerMaskDisplayNames);
                if (newTypeMask != providerTypeMask)
                {
                    _defaultProviderTypeProp.intValue = newTypeMask;
                }


                EditorGUILayout.PropertyField(_defaultToWindowTypesProp);

                DrawScriptingDefineToggles();
            }

            private void DrawScriptingDefineToggles()
            {
                EditorGUILayout.Space();

                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out var defines);

                DrawDefineToggle(PICKLE_IS_DEFAULT, defines, "Set Pickle as default picker", "Unset Pickle as default picker");
                DrawDefineToggle(PICKLE_IN_ROOT_NAMESPACE, defines, "Move attribute to default namespace", "Move attribute to Pickle namespace");
            }

            private void DrawDefineToggle(string defineString, string[] defines, string positiveLabel, string negativeLabel)
            {
                var defineIndex = System.Array.IndexOf(defines, defineString);
                var isSymbolSet = defineIndex >= 0;
                var toggleButtonLabel = isSymbolSet ? negativeLabel : positiveLabel;

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
                    if (isSymbolSet)
                    {
                        defines[defineIndex] = defines[defines.Length - 1];
                        System.Array.Resize(ref defines, defines.Length - 1);
                    }
                    else
                    {
                        System.Array.Resize(ref defines, defines.Length + 1);
                        defines[defines.Length - 1] = defineString;

                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);

                    Repaint();
                }
            }

            private void FetchProperties()
            {
                if (_defaultPickerTypeProp == null)
                    _defaultPickerTypeProp = serializedObject.FindProperty(nameof(_defaultPickerType));

                if (_defaultProviderTypeProp == null)
                    _defaultProviderTypeProp = serializedObject.FindProperty(nameof(_defaultObjectProvider));

                if (_defaultAutoPickModeProp == null)
                    _defaultAutoPickModeProp = serializedObject.FindProperty(nameof(_defaultAutoPickMode));

                if (_defaultToWindowTypesProp == null)
                    _defaultToWindowTypesProp = serializedObject.FindProperty(nameof(_defaultToWindowTypeNames));

                if (_providerMaskDisplayNames == null || _providerMaskDisplayNames.Length == 0)
                {
                    var values = (ObjectProviderType[]) System.Enum.GetValues(typeof(ObjectProviderType));
                    var displayNames = new List<string>();

                    for (int i = 0; i < 32; i++)
                    {
                        var val = 1 << i;
                        if (System.Array.IndexOf(values, (ObjectProviderType)val) >= 0)
                        {
                            displayNames.Add(((ObjectProviderType)val).ToString());
                        }
                        else
                        {
                            displayNames.Add(i.ToString());
                        }
                    }

                    _providerMaskDisplayNames = displayNames.ToArray();
                }
            }
        }
    }
}
