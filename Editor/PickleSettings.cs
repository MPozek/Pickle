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
    }
}
