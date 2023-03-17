using System;
using UnityEngine;
using UnityEditor;

namespace Pickle.Editor
{
    public interface IObjectPicker
    {
        event Action<SerializedProperty, UnityEngine.Object> OnOptionPicked;
        void Show(SerializedProperty property, Rect sourceRect, UnityEngine.Object selectedObject);
    }
}
