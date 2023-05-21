using UnityEditor;
using UnityEngine;

namespace ReferenceViewer
{
    internal class SearchResultView
    {
        private readonly SearchResult _searchResult;
        private readonly GUIStyle _labelStyle = CreateLabelStyle();
        
        bool _isFoldout = true;

        public SearchResultView(SearchResult searchResult)
        {
            _searchResult = searchResult;
        }

        private static GUIStyle CreateLabelStyle()
        {
            var result = new GUIStyle(EditorStyles.label)
            {
                fixedHeight = 18f,
                richText = true
            };

            RectOffset margin = result.margin;
            margin.top = 0;
            margin.bottom = 0;
            result.margin = margin;

            return result;
        }

        public void OnGui()
        {
            var listItem = CreateListItem(_searchResult.Asset, _searchResult.Path);

            // 参照が無い場合はFoldoutではなくButtonで表示する
            // If there is no reference, display with Button instead of Foldout.
            if (_searchResult.References.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                

                if (GUILayout.Button(listItem, _labelStyle))
                {
                    Selection.activeObject = _searchResult.Asset;
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }

                GUILayout.EndHorizontal();
                return;
            }

            var wasFoldedOut = _isFoldout;
            _isFoldout = EditorGUILayout.Foldout(_isFoldout, listItem, toggleOnLabelClick: true);
            
            if (_isFoldout)
            {
                foreach (var assetData in _searchResult.References)
                {
                    // 削除されたか、そもそもアセットではない
                    // It was deleted or not an asset.
                    if (assetData.Asset == null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(32f);
                        GUILayout.Label($"<i>（Missing）</i>{assetData.Path}", _labelStyle);
                        GUILayout.EndHorizontal();
                        continue;
                    }

                    DrawUnityAsset(assetData);
                }
            }

            // アセットをクリックした時にProjectビューで選択状態にする
            // Select asset in project view when clicking asset.
            if (wasFoldedOut != _isFoldout)
            {
                Selection.activeObject = _searchResult.Asset;
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }

        private void DrawUnityAsset(UnityAsset data)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(32f);

            // アセットをクリックした時にProjectビューで選択状態にする
            // Select asset in project view when clicking asset.
            var listItem = CreateListItem(data.Asset, data.Path);
            if (GUILayout.Button(listItem, _labelStyle))
            {
                Selection.activeObject = data.Asset;
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            GUILayout.EndHorizontal();
        }
        
        private GUIContent CreateListItem(Object asset, string path)
        {
            var icon = AssetDatabase.GetCachedIcon(path);
            return new GUIContent(asset.name, icon, path);
        }
    }
}
