/*
unity-reference-viewer

Copyright (c) 2019 ina-amagami (ina@amagamina.jp)

This software is released under the MIT License.
https://opensource.org/licenses/mit-license.php
*/

#nullable enable

using System;
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

		private readonly Dictionary<string, SearchItem> _searchCache = new();

		public static void CreateWindow(ReferenceFinder finder, SearchResult result)
		{
			var window = GetWindow<ReferenceViewerWindow>("ReferenceViewer2");
			window.SetResult(finder, result);
		}
		
		private ReferenceFinder? _referenceFinder;
		private string? _additionalInformation;
		private IReadOnlyList<SearchResultView>? _resultViews;
		private Vector2 _scrollPosition = Vector2.zero;

		private bool _automaticUpdate;

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
			if (_automaticUpdate == false)
			{
				return;
			}
			
			if (_referenceFinder == null)
			{
				#if UNITY_EDITOR_OSX
				var ignore = ArraySegment<string>.Empty;
				_referenceFinder = new ReferenceFinder(new MacOsGrepReferenceFinder(ignore), ignore);
				#elif UNITY_EDITOR_WIN
				_referenceFinder = new ReferenceFinder(new WindowsFindStrReferenceFinder(), ArraySegment<string>.Empty);
				#endif
			}
			
			var selection = Selection.assetGUIDs;
			if (selection.Length == 0)
			{
				return;
			}
			
			var itemsResults = selection.Select(
				guid =>
				{
					var result = _searchCache.GetValueOrDefault(guid);
					if (result != null)
					{
						return result;
					}
					
					result = _referenceFinder.FindReferencedFiles(guid);
					_searchCache.Add(guid, result);
					return result;
				});
			var searchResult = new SearchResult(itemsResults.ToArray(), _additionalInformation);
			SetResult(_referenceFinder, searchResult);
		}

		private void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var text = _automaticUpdate ? "Disable Auto Update" : "Enable Auto Update";
				if (GUILayout.Button(text))
				{
					_automaticUpdate = !_automaticUpdate;
				}
				if (GUILayout.Button("Clear cache"))
				{
					_searchCache.Clear();
					OnSelectionUpdate();
				}
			}
			
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