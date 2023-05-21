/*
unity-reference-viewer

Copyright (c) 2019 ina-amagami (ina@amagamina.jp)

This software is released under the MIT License.
https://opensource.org/licenses/mit-license.php
*/

#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ReferenceViewer
{
	public class ReferenceViewerWindow : EditorWindow
	{
		private const string HeaderLabelTitle = "【Find References In Project - Result】";

		public static void CreateWindow(IEnumerable<SearchResult> result)
		{
			var window = GetWindow<ReferenceViewerWindow>("ReferenceViewer2");
			window.SetResult(result);
		}

		private IReadOnlyList<SearchResultView>? _resultViews;
		private Vector2 _scrollPosition = Vector2.zero;

		public void SetResult(IEnumerable<SearchResult> result)
		{
			_resultViews = result.Select(it => new SearchResultView(it)).ToArray();
			_scrollPosition = Vector2.zero;
			Repaint();
		}

		private void OnGUI()
		{
			if (_resultViews == null)
			{
				return;
			}
			EditorGUILayout.Separator();
			GUILayout.Label(HeaderLabelTitle);

			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(Vector2.one * 16f);

			// アセット毎の参照リスト
			// Reference list for each asset.
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			foreach (var item in _resultViews)
			{
				item.OnGui();
			}
			
			EditorGUILayout.EndScrollView();

			EditorGUIUtility.SetIconSize(iconSize);

			// TODO

			// Spotlightの時のみヘルプ情報
			// Help information only for Spotlight.
			// if (result.Type == Result.SearchType.OSX_Spotlight)
			// {
			// 	EditorGUILayout.Separator();
			// 	EditorGUILayout.HelpBox(
			// 		"Assets内のインデックスが作られていない場合など、正しく検索できないことがあります。" +
			// 		"正確に検索するにはGrep版を使用して下さい。\n\n" +
			// 		"Spotlight is not be able to search correctly, for example, when an file index in Assets is not created. " +
			// 		"Please use Grep version to search exactly.",
			// 		MessageType.Info
			// 	);
			// 	EditorGUILayout.Separator();
			// }
		}
	}
}