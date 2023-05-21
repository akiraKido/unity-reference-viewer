/*
unity-reference-viewer

Copyright (c) 2019 ina-amagami (ina@amagamina.jp)

This software is released under the MIT License.
https://opensource.org/licenses/mit-license.php
*/

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityReferenceViewer.Editor.Models;
using UnityReferenceViewer.Editor.ValueObjects;

namespace UnityReferenceViewer.Editor.Views
{
    /// <summary>
    /// 環境ごとに実行内容を切り分け
    /// Execution contents for target OS.
    /// </summary>
    public class ReferenceViewerMenu : EditorWindow
    {
        // =============================================================================================================
        // 設定ファイルのロード

        private static ExcludeSettings _settings;

        private static bool LoadSettings()
        {
            // インスタンスを経由して自身のファイルパスを取得
            // Retrieve its own file path via instance.
            var window = GetWindow<ReferenceViewerMenu>();
            _settings = window.GetSettingFile();
            window.Close();

            if (_settings != null)
            {
                return true;
            }

            Debug.LogError("[ReferenceViewer] Failed to load exclude setting file.");
            return false;
        }

        private ExcludeSettings GetSettingFile()
        {
	        var thisObject = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(thisObject);
            var packageDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
            return ExcludeSettings.Load(packageDirectory);
        }

        private static void DoSearch(IReferenceFinder referenceFinder, string additionalInfo = null)
        {
            var finder = new ReferenceFinder(referenceFinder, _settings.GetExcludeExtentions());
            var searchItems = Selection.assetGUIDs
                .Select(finder.FindReferencedFiles)
                .ToArray();
            var result = new SearchResult(searchItems, additionalInfo);
            ReferenceViewerWindow.CreateWindow(result);
        }

        #if UNITY_EDITOR_OSX

        // =============================================================================================================
        // Mac/Spotlight

        [MenuItem("Assets/Find References In Project/By Spotlight", true)]
        static bool IsEnabledBySpotlight()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Find References In Project/By Spotlight", false, 25)]
        public static void FindReferencesBySpotlight()
        {
            if (!LoadSettings())
            {
                return;
            }

            DoSearch(new MacOsSpotlightReferenceFinder());
        }

        // =============================================================================================================
        // Mac/Grep

        [MenuItem("Assets/Find References In Project/By Grep", true)]
        static bool IsEnabledByGrep()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Find References In Project/By Grep", false, 26)]
        public static void FindReferencesByGrep()
        {
            if (!LoadSettings())
            {
                return;
            }

            DoSearch(new MacOsGrepReferenceFinder(_settings.GetExcludeExtentions()));
        }

        // =============================================================================================================
        // Mac/GitGrep
        
        [MenuItem("Assets/Find References In Project/By GitGrep", true)]
        static bool IsEnabledByGitGrep()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Find References In Project/By GitGrep", false, 27)]
        public static void FindReferencesByGitGrep()
        {
            if (!LoadSettings())
            {
                return;
            }

            DoSearch(
                new MacOsGitGrepReferenceFinder(),
                additionalInfo: "Assets内のインデックスが作られていない場合など、正しく検索できないことがあります。"
                                + "正確に検索するにはGrep版を使用して下さい。\n\n"
                                + "Spotlight is not be able to search correctly, for example, when an file index in Assets is not created. "
                                + "Please use Grep version to search exactly."
            );
        }

        #endif

        #if UNITY_EDITOR_WIN
        // =============================================================================================================
 	    // Win/FindStr
        
		[MenuItem("Assets/Find References In Project/By FindStr", true)]
		static bool IsEnabledByFindStr()
		{
			if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
			{
				return false;
			}
			return true;
		}

		[MenuItem("Assets/Find References In Project/By FindStr", false, 25)]
		public static void FindReferencesByFindStr()
		{
			if (!LoadSettings())
			{
				return;
			}

			DoSearch(new WindowsFindStrReferenceFinder());
		}

		// =============================================================================================================
		// Win/Grep

		[MenuItem("Assets/Find References In Project/By GitGrep", true)]
		static bool IsEnabledByGitGrep()
		{
			if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
			{
				return false;
			}

			string pathEnv =
 System.Environment.GetEnvironmentVariable("Path", System.EnvironmentVariableTarget.Process);
			if (pathEnv == null || pathEnv.Trim() == "")
			{
				return false;
			}
			string[] paths = pathEnv.Split(';');
			foreach (string path in paths)
			{
				if (System.IO.File.Exists(System.IO.Path.Combine(path, Result.SearchType.WIN_GitGrep.Command().Command)))
				{
					return true;
				}
			}
			return false;
		}
		
		[MenuItem("Assets/Find References In Project/By GitGrep", false, 27)]
		public static void FindReferencesByGitGrep()
		{
			if (!LoadSettings())
			{
				return;
			}

			Result result =
 ReferenceViewerProcessor.FindReferencesByCommand(Result.SearchType.WIN_GitGrep, settings.GetExcludeExtentions());
			if (result != null)
			{
				ReferenceViewerWindow.CreateWindow(result);
			}
		}
        #endif
    }
}
