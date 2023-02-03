using Pickle.ObjectProviders;
using System;
using UnityEngine;
using UnityEditor;

namespace Pickle.Editor
{
    public class ObjectPickerWindowBuilder : IObjectPicker
    {
        public event Action<SerializedProperty, UnityEngine.Object> OnOptionPicked;

        private SerializedProperty _property;

        private readonly string _title;
        private readonly IObjectProvider _lookupStrategy;
        private readonly Predicate<ObjectTypePair> _filter;

        public ObjectPickerWindowBuilder(string title, IObjectProvider lookupStrategy, Predicate<ObjectTypePair> filter)
        {
            _title = title;
            _lookupStrategy = lookupStrategy;
            _filter = filter;
        }

        public void Show(SerializedProperty property, Rect sourceRect, UnityEngine.Object selectedObject)
        {
            _property = property;
            ObjectPickerWindow.OpenCustomPicker(_title, OnOptionPickedListener, _lookupStrategy, _filter, selectedObject);
        }

        private void OnOptionPickedListener(UnityEngine.Object obj)
        {
            OnOptionPicked?.Invoke(_property, obj);
        }
    }
}
