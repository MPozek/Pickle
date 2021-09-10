using Pickle.ObjectProviders;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Pickle.Editor
{

    public class ObjectPickerWindow : EditorWindow
    {
        private static readonly Vector2 DEFAULT_SIZE = new Vector2(700f, 560f);
        private const float GRID_TILE_SIZE_MIN = 100f;

        private IObjectProvider _lookupStrategy;
        private Action<UnityEngine.Object> _onPickCallback;
        private Predicate<ObjectTypePair> _filter;
        private SearchField _searchField;

        private int _selectedOptionIndex;
        private string _searchString;
        private Vector2 _scrollPosition;
        private List<ObjectTypePair> _options = new List<ObjectTypePair>();
        private List<int> _visibleOptionIndices = new List<int>();
        private bool _drawAsAList;

        public static void OpenCustomPicker(
            string title, 
            Action<UnityEngine.Object> onPick, 
            IObjectProvider lookupStrategy, 
            Predicate<ObjectTypePair> filter, 
            UnityEngine.Object selectedObject = null)
        {
            var picker = GetWindow<ObjectPickerWindow>(true, title);

            picker._drawAsAList = false;
            picker._lookupStrategy = lookupStrategy;
            picker._onPickCallback = onPick;
            picker._filter = filter;
            picker.RefreshList();

            picker._selectedOptionIndex = picker._options.FindIndex((option) => option.Object == selectedObject);
        }

        private void OnEnable()
        {
            _searchString = null;
            _selectedOptionIndex = -1;
            _scrollPosition = new Vector2(0f, 0f);

            _searchField = new SearchField();
            _searchField.autoSetFocusOnFindCommand = true;
            _searchField.downOrUpArrowKeyPressed += SearchField_downOrUpArrowKeyPressed;

            var pos = position;
            pos.size = DEFAULT_SIZE;
            position = pos;
        }

        private void SearchField_downOrUpArrowKeyPressed()
        {
            // which one was it?
            var ev = Event.current;
            var isDown = ev.keyCode == KeyCode.DownArrow;
            Event.current.Use();

            var displayIndexOfCurrentOption = _visibleOptionIndices.IndexOf(_selectedOptionIndex);
            displayIndexOfCurrentOption += isDown ? 1 : -1;

            // make sure the index is in range
            displayIndexOfCurrentOption = (displayIndexOfCurrentOption + _visibleOptionIndices.Count) % _visibleOptionIndices.Count;
            _selectedOptionIndex = _visibleOptionIndices[displayIndexOfCurrentOption];

            Repaint();
        }

        private void RefreshList()
        {
            _options.Clear();

            var lookupEnumeration = _lookupStrategy.Lookup();
            while (lookupEnumeration.MoveNext())
            {
                var objectTypePair = lookupEnumeration.Current;
                var obj = objectTypePair.Object;

                if ((_filter?.Invoke(objectTypePair)).GetValueOrDefault(true) && (obj.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    _options.Add(objectTypePair);
                }
            }

            RefreshVisibleOptions();

            Repaint();
        }

        private void RefreshVisibleOptions()
        {
            _visibleOptionIndices.Clear();
            var hasSearchString = !string.IsNullOrEmpty(_searchString);

            _visibleOptionIndices.Add(-1);
            for (int i = 0; i < _options.Count; i++)
            {
                UnityEngine.Object obj = _options[i].Object;
                if (!hasSearchString || obj.ToString().ToLowerInvariant().Contains(_searchString.ToLowerInvariant()))
                {
                    _visibleOptionIndices.Add(i);
                }
            }
        }

        private void AcceptSelectionAndClose()
        {
            _onPickCallback?.Invoke(_selectedOptionIndex >= 0 ? _options[_selectedOptionIndex].Object : null);
            Close();
        }

        private void OnGUI()
        {
            // handle enter and escape keys
            var ev = Event.current;
            if (ev.type == EventType.KeyDown)
            {
                if (ev.keyCode == KeyCode.KeypadEnter || ev.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    AcceptSelectionAndClose();
                    return;
                }
                else if (ev.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    Close();
                    return;
                }
            }

            // search bar
            EditorGUI.BeginChangeCheck();

            var toolbarRect = EditorGUILayout.GetControlRect(false);

            var searchRect = toolbarRect;
            searchRect.width -= 150f;

            _searchString = _searchField.OnGUI(searchRect, _searchString);
            _searchField.SetFocus();

            var buttonsRect = toolbarRect;
            buttonsRect.width = 100f;
            buttonsRect.center += Vector2.right * (searchRect.width + 50f);

            var leftButton = buttonsRect; leftButton.width *= 0.5f;
            var rightButton = leftButton; rightButton.center += Vector2.right * rightButton.width;

            if (GUI.Toggle(leftButton, _drawAsAList, "list", EditorStyles.miniButtonLeft) != _drawAsAList)
            {
                _drawAsAList = true;
            }

            if (GUI.Toggle(rightButton, !_drawAsAList, "grid", EditorStyles.miniButtonRight) == _drawAsAList)
            {
                _drawAsAList = false;
            }

            if (EditorGUI.EndChangeCheck())
            {
                RefreshVisibleOptions();
            }

            EditorGUILayout.Space();

            DrawOptionsGUI();
        }

        private void DrawOptionsGUI()
        {
            // display
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_drawAsAList)
            {
                DrawList();
            }
            else
            {
                DrawGrid();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawList()
        {
            for (int i = 0; i < _visibleOptionIndices.Count; i++)
            {
                DrawOptionListSelectionLabel(_visibleOptionIndices[i]);
            }
        }

        private void DrawGrid()
        {
            const float PADDING = 10f;

            var width = position.width;
            var columnCount = Mathf.FloorToInt(width / GRID_TILE_SIZE_MIN);
            var tileWidth = width / columnCount;
            var tileHeight = tileWidth + EditorGUIUtility.singleLineHeight * 2f;

            var rows = Mathf.CeilToInt(_visibleOptionIndices.Count * 1f / columnCount);
            var height = rows * tileHeight;

            var rect = EditorGUILayout.GetControlRect(false, height, GUILayout.ExpandWidth(true));

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    var tileRect = new Rect(rect.min + new Vector2(j * tileWidth, i * tileHeight), new Vector2(tileWidth, tileHeight));
                    tileRect = Rect.MinMaxRect(tileRect.xMin + PADDING, tileRect.yMin + PADDING, tileRect.xMax - PADDING, tileRect.yMax - PADDING);

                    var idx = i * columnCount + j;

                    if (_visibleOptionIndices.Count > idx)
                    {
                        DrawOptionImagesSelectionLabel(tileRect, _visibleOptionIndices[idx]);
                    }
                }
            }
        }

        private void DrawOptionImagesSelectionLabel(Rect tileRect, int optionIndex)
        {
            if (GUI.Button(tileRect, "", GUIStyle.none))
            {
                if (_selectedOptionIndex == optionIndex)
                {
                    AcceptSelectionAndClose();
                    return;
                }

                _selectedOptionIndex = optionIndex;
            }

            var labelStyle = (GUIStyle)"GridListText";
            labelStyle.wordWrap = true;
            labelStyle.alignment = TextAnchor.UpperCenter;

            EditorGUI.Toggle(tileRect, GUIContent.none, _selectedOptionIndex == optionIndex, labelStyle);
            if (optionIndex >= 0)
            {
                var option = _options[optionIndex];

                var imageRect = tileRect;
                imageRect.height = imageRect.width;

                var preview = AssetPreview.GetAssetPreview(option.Object);
                if (!preview)
                {
                    preview = AssetPreview.GetMiniThumbnail(option.Object);
                }
                var imageDrawRect = Rect.MinMaxRect(imageRect.xMin + 10f, imageRect.yMin + 10f, imageRect.xMax - 10f, imageRect.yMax - 10f);
                
                GUI.DrawTexture(imageDrawRect, preview, ScaleMode.ScaleToFit, true);

                var labelRect = imageRect;
                labelRect.height = tileRect.height - imageRect.height;
                labelRect.center += Vector2.up * imageRect.height;

                EditorGUI.LabelField(labelRect, option.Object.ToString(), labelStyle);
            }
            else
            {
                var labelRect = tileRect;
                labelRect.height = tileRect.height - tileRect.width;
                labelRect.center += Vector2.up * tileRect.width;
                EditorGUI.LabelField(labelRect, "None", labelStyle);
            }
        }

        private void DrawOptionListSelectionLabel(int optionIndex)
        {
            var obj = optionIndex >= 0 ? _options[optionIndex].Object : null;

            string name = optionIndex >= 0 ? _options[optionIndex].Object.ToString() : "None";
            string tag = optionIndex >= 0 ? _options[optionIndex].Type.ToString() : "";

            var previewTexture = obj == null ? null : AssetPreview.GetMiniThumbnail(obj);

            if (DrawSelectableLabel(name, tag, _selectedOptionIndex == optionIndex, previewTexture))
            {
                if (_selectedOptionIndex == optionIndex)
                {
                    AcceptSelectionAndClose();
                    return;
                }

                _selectedOptionIndex = optionIndex;
            }
        }

        private bool DrawSelectableLabel(string text, string tag, bool isSelected, Texture2D icon = null)
        {
            var labelStyle = (GUIStyle)"GridListText";

            bool result = false;
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));

            if (GUI.Button(r, "", GUIStyle.none))
            {
                result = true;
            }

            EditorGUI.Toggle(r, isSelected, labelStyle);

            float indentation = r.height;
            r.xMin += indentation;

            float iconWidth = r.height;
            if (icon)
            {
                GUI.DrawTexture(new Rect(r.min, new Vector2(r.height, r.height)), icon);
            }

            r.xMin += iconWidth;
            r.width -= iconWidth;

            float columnWidth = r.width / 2f;

            var columnRect = r; r.width = columnWidth;
            EditorGUI.LabelField(columnRect, text, EditorStyles.whiteLabel);

            columnRect.x += columnWidth;
            EditorGUI.LabelField(columnRect, tag, EditorStyles.whiteLabel);

            return result;
        }
    }
}
