using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System.Collections.Generic;
using Pickle.ObjectProviders;
using UnityEngine;

namespace Pickle.Editor
{
    public class ObjectPickerDropdown : AdvancedDropdown, IObjectPicker
    {
        public event Action<UnityEngine.Object> OnOptionPicked;

        public string Title = "";

        private List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();

        private readonly IObjectProvider _lookupStrategy;
        private readonly Predicate<ObjectTypePair> _filter;

        public ObjectPickerDropdown(IObjectProvider lookupStrategy, AdvancedDropdownState state, Predicate<ObjectTypePair> filter = null) : base(state)
        {
            _lookupStrategy = lookupStrategy;
            _filter = filter;

            var minSize = base.minimumSize;
            minSize.y = 300f;
            base.minimumSize = minSize;
        }

        public void Show(Rect sourceRect, UnityEngine.Object _)
        {
            Show(sourceRect);
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item.enabled)
            {
                if (item.id >= 0)
                {
                    OnOptionPicked?.Invoke(_objects[item.id]);
                }
                else
                {
                    OnOptionPicked?.Invoke(null);
                }
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(Title);

            int assetsCount = 0;
            int sceneCount = 0;
            var assets = new AdvancedDropdownItem("Assets");
            var scene = new AdvancedDropdownItem("Scene");

            var nullChoice = new AdvancedDropdownItem("None");
            nullChoice.id = -1;
            root.AddChild(nullChoice);

            root.AddChild(assets);

            var iterator = _lookupStrategy.Lookup();
            while (iterator.MoveNext())
            {
                var cur = iterator.Current;

                if (_filter != null && !_filter.Invoke(cur))
                    continue;

                var item = new AdvancedDropdownItem(cur.Object.name);
                item.icon = AssetPreview.GetMiniThumbnail(cur.Object);
                //item.icon = AssetPreview.GetMiniTypeThumbnail(cur.Object.GetType());
                item.id = _objects.Count;
                if (cur.Type == ObjectSourceType.Asset)
                {
                    assetsCount++;
                    assets.AddChild(item);
                }
                else if (cur.Type == ObjectSourceType.Scene)
                {
                    sceneCount++;
                    root.AddChild(item);
                    // scene.AddChild(item);
                }
                else
                {
                    root.AddChild(item);
                }

                _objects.Add(cur.Object);
            }

            scene.enabled = sceneCount != 0;
            assets.enabled = assetsCount != 0;


            if (sceneCount == 0)
            {
                root = assets;
                root.name = Title;
            }

            return root;
        }
    }
}
