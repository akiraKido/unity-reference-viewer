/*
unity-reference-viewer

Copyright (c) 2019 ina-amagami (ina@amagamina.jp)

This software is released under the MIT License.
https://opensource.org/licenses/mit-license.php
*/

#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityReferenceViewer.Editor.Models;
using UnityReferenceViewer.Editor.ValueObjects;

namespace UnityReferenceViewer.Editor.Views
{
	public class ReferenceViewerWindow : EditorWindow
	{
		private const string HeaderLabelTitle = "【Find References In Project - Result】";

		public static void CreateWindow(ReferenceFinder finder, SearchResult result)
		{
			var window = GetWindow<ReferenceViewerWindow>("ReferenceViewer2");
			window.SetResult(finder, result);
		}
		
		private ReferenceFinder? _referenceFinder;
		private string? _additionalInformation;
		private IReadOnlyList<SearchResultView>? _resultViews;
		private Vector2 _scrollPosition = Vector2.zero;

		public void SetResult(ReferenceFinder finder, SearchResult result)
		{
			_referenceFinder = finder;
			
			_resultViews = result.SearchResults.Select(it => new SearchResultView(it)).ToArray();
			_additionalInformation = result.AdditionalInformation;
			
			_scrollPosition = Vector2.zero;
			Repaint();
		}

		private void OnEnable()
		{
			Selection.selectionChanged += OnSelectionUpdate;
		}

		private void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionUpdate;
		}

		private void OnSelectionUpdate()
		{
			Debug.Log("changed");
			if (_referenceFinder == null)
			{
				return;
			}
			
			var selection = Selection.assetGUIDs;
			if (selection.Length == 0)
			{
				return;
			}
			
			var itemsResults = selection.Select(_referenceFinder.FindReferencedFiles);
			var searchResult = new SearchResult(itemsResults.ToArray(), _additionalInformation);
			SetResult(_referenceFinder, searchResult);
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
			
			DisplayAdditionalInformation();
		}
		
		private void DisplayAdditionalInformation()
		{
			// Spotlightの時のみヘルプ情報
			// Help information only for Spotlight.
			if (_additionalInformation == null)
			{
				return;
			}
			
			EditorGUILayout.Separator();
			EditorGUILayout.HelpBox(_additionalInformation, MessageType.Info);
			EditorGUILayout.Separator();
		}
	}
}