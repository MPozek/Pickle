using System;
using UnityEngine;

namespace Pickle.Editor
{
    public interface IObjectPicker
    {
        event Action<UnityEngine.Object> OnOptionPicked;
        void Show(Rect sourceRect, UnityEngine.Object selectedObject);
    }
}
