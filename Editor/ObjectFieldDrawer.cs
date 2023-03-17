using System;
using UnityEditor;
using UnityEngine;

namespace Pickle.Editor
{
    public class ObjectFieldDrawer
    {
        public event Action OnObjectPickerButtonClicked;
        public readonly Predicate<UnityEngine.Object> IsObjectValidForField;
        private readonly Func<UnityEngine.Object, GUIContent> _objectLabelGetter;

        public Rect FieldRect { get; private set; }

        public static GUIContent DefaultObjectLabelGetter(UnityEngine.Object obj, string typeName)
        {
            return obj ? new GUIContent(obj.ToString(), AssetPreview.GetMiniThumbnail(obj)) : new GUIContent($"None ({typeName})");
        }

        public ObjectFieldDrawer(Predicate<UnityEngine.Object> isObjectValidForField, Func<UnityEngine.Object, GUIContent> objectLabelGetter)
        {
            IsObjectValidForField = isObjectValidForField;
            _objectLabelGetter = objectLabelGetter;
        }

        public ObjectFieldDrawer(Predicate<UnityEngine.Object> isObjectValidForField, System.Type fieldType)
        {
            IsObjectValidForField = isObjectValidForField;
            _objectLabelGetter = (obj) => DefaultObjectLabelGetter(obj, fieldType.Name);
        }

        public ObjectFieldDrawer(Predicate<UnityEngine.Object> isObjectValidForField, string fieldTypeName)
        {
            IsObjectValidForField = isObjectValidForField;
            _objectLabelGetter = (obj) => DefaultObjectLabelGetter(obj, fieldTypeName);
        }

        public UnityEngine.Object Draw(
            Rect position, GUIContent label,
            UnityEngine.Object activeObject)
        {
            var dropBoxRect = EditorGUI.PrefixLabel(position, label);
            var buttonRect = dropBoxRect;
            buttonRect.xMin = position.xMax - 20f;
            buttonRect = new RectOffset(-1, -1, -1, -1).Add(buttonRect);

            if (Event.current.type == EventType.Repaint)
                FieldRect = dropBoxRect;

            // we have to manually handle the mouse down events cause GUI.Button eats them
            if (GUI.enabled)
            {
                var ev = Event.current;

                var isMouseInsideControl = dropBoxRect.Contains(ev.mousePosition);

                if (isMouseInsideControl)
                {
                    if (ev.type == EventType.MouseDown)
                    {
                        if (isMouseInsideControl)
                        {
                            var isMouseOverSelectButton = buttonRect.Contains(ev.mousePosition);

                            if (isMouseOverSelectButton)
                            {
                                Event.current.Use();
                                OnObjectPickerButtonClicked?.Invoke();
                            }
                            else
                            {
                                Event.current.Use();
                                if (activeObject != null)
                                {
                                    EditorGUIUtility.PingObject(GetPingableObject(activeObject));
                                }
                            }
                        }
                    }
                    else if (HandleDragEvents(GetDraggedObjectIfValid(), ref activeObject))
                    {
                        Event.current.Use();
                    }

                }
            }

            GUIContent activeObjectLabel = _objectLabelGetter(activeObject);

            if (activeObjectLabel.image)
            {
                GUI.Toggle(dropBoxRect, dropBoxRect.Contains(Event.current.mousePosition) && GetDraggedObjectIfValid(), GUIContent.none, EditorStyles.objectField);

                var iconRect = dropBoxRect;
                iconRect.center += Vector2.right * 3f;
                iconRect.width = 15f;

                var labelRect = dropBoxRect;
                labelRect.xMin += iconRect.width + 1f;

                var icon = activeObjectLabel.image;
                activeObjectLabel.image = null;

                var labelStyle = new GUIStyle(EditorStyles.objectField);
                labelStyle.normal.background = Texture2D.blackTexture;

                EditorGUI.LabelField(labelRect, activeObjectLabel, labelStyle);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Toggle(dropBoxRect, dropBoxRect.Contains(Event.current.mousePosition) && GetDraggedObjectIfValid(), activeObjectLabel, EditorStyles.objectField);
            }

            var objectFieldButtonStyle = new GUIStyle("ObjectFieldButton");
            GUI.Button(buttonRect, new GUIContent(""), objectFieldButtonStyle);

            return activeObject;
        }

        private UnityEngine.Object GetPingableObject(UnityEngine.Object activeObject)
        {
            if (activeObject is Component component)
            {
                return component.gameObject;
            }
            else
            {
                return activeObject;
            }
        }

        private UnityEngine.Object GetDraggedObjectIfValid()
        {
            var draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects.Length != 1) return null;
            var obj = draggedObjects[0];

            return IsObjectValidForField.Invoke(obj) ? obj : null;
        }

        private static bool HandleDragEvents(bool isValidObjectBeingDragged, ref UnityEngine.Object activeObject)
        {
            var ev = Event.current;
            if (ev.type == EventType.DragUpdated)
            {
                if (isValidObjectBeingDragged)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }

                return true;
            }
            else if (ev.type == EventType.DragPerform)
            {
                if (isValidObjectBeingDragged)
                {
                    DragAndDrop.AcceptDrag();
                    activeObject = DragAndDrop.objectReferences[0];
                }

                return true;
            }
            else if (ev.type == EventType.DragExited)
            {
                return true;
            }
            return false;
        }
    }
}
