#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ReferenceViewer
{
    public class ReferenceFinder
    {
        private static readonly string ProjectPath = Path.GetDirectoryName(Application.dataPath)!;

        private readonly HashSet<string>? _excludeExtensions;
        private readonly IReferenceFinder _referenceFinder;

        [MenuItem("Tools/ReferenceViewer/Test")]
        private static void Test()
        {
            var selection = Selection.assetGUIDs.FirstOrDefault();
            if (selection == null)
            {
                Debug.LogError("Select asset.");
                return;
            }

            var result = new ReferenceFinder(new MacOsGrepReferenceFinder(ArraySegment<string>.Empty))
                .FindReferencedFiles(selection);

            Debug.Log(string.Join("\n", result.References.Select(it => it.Path)));
        }

        public ReferenceFinder(IReferenceFinder referenceFinder)
        {
            _referenceFinder = referenceFinder;
        }

        public ReferenceFinder(
            IReferenceFinder referenceFinder,
            IEnumerable<string> excludeExtensions)
        {
            _excludeExtensions = new HashSet<string>(excludeExtensions);
            _referenceFinder = referenceFinder;
        }

        public SearchResult FindReferencedFiles(string guid)
        {
            var currentFilePath = AssetDatabase.GUIDToAssetPath(guid);
            var rawReferenceFiles = _referenceFinder.FindReferencedItemPaths(guid);
            var filteredReferenceFiles = FilterValidFiles(currentFilePath, rawReferenceFiles);

            return new SearchResult(
                path: currentFilePath,
                references: filteredReferenceFiles.Select(it => new UnityAsset(it)).ToArray()
            );
        }

        private IEnumerable<string> FilterValidFiles(
            string searchTargetPath,
            IEnumerable<string> rawPaths)
        {
            foreach (var item in rawPaths)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                var extension = Path.GetExtension(item);
                if (extension != null && _excludeExtensions != null && _excludeExtensions.Contains(extension))
                {
                    continue;
                }

                string referencingFileRelativeToProject;

                if (extension == ".meta")
                {
                    // metaファイルの場合は親ファイルを参照する
                    var parentFile = item[..^".meta".Length];
                    referencingFileRelativeToProject = Path.GetRelativePath(ProjectPath, parentFile);
                    if (referencingFileRelativeToProject == searchTargetPath)
                    {
                        // ただし自分のmetaが持っている場合は除外する
                        continue;
                    }
                }
                else
                {
                    referencingFileRelativeToProject = Path.GetRelativePath(ProjectPath, item);
                }

                yield return referencingFileRelativeToProject;
            }
        }
    }

    public interface IReferenceFinder
    {
        IEnumerable<string> FindReferencedItemPaths(string guid);
    }

    internal abstract class ProcessBasedReferenceFinder : IReferenceFinder
    {
        public IEnumerable<string> FindReferencedItemPaths(string guid)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Command,
                Arguments = BuildArguments(guid),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = Application.dataPath
            };

            using var p = new Process
            {
                StartInfo = startInfo
            };

            p.Start();

            var result = p.StandardOutput.ReadToEnd();

            p.WaitForExit();

            return result.Split(new[] { SplitChar }, StringSplitOptions.None);
        }

        protected abstract string BuildArguments(string guid);
        protected abstract string Command { get; }

        [PublicAPI]
        protected virtual string SplitChar => "\0";
    }
    
    #if UNITY_EDITOR_OSX

    internal class MacOsGrepReferenceFinder : ProcessBasedReferenceFinder
    {
        private readonly IReadOnlyCollection<string> _excludeExtensionList;

        public MacOsGrepReferenceFinder(IReadOnlyCollection<string> excludeExtensionList)
        {
            _excludeExtensionList = excludeExtensionList;
        }

        // grep [guid] -rl --null '[Application.dataPath]'

        protected override string Command => "grep";

        protected override string BuildArguments(string guid)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(guid);
            stringBuilder.Append(" -rl --null '");
            stringBuilder.Append(Application.dataPath);
            stringBuilder.Append("'");

            foreach (var extension in _excludeExtensionList)
            {
                stringBuilder.Append(" --exclude='");
                stringBuilder.Append(extension);
                stringBuilder.Append("'");
            }

            return stringBuilder.ToString();
        }
    }

    internal class MacOsSpotlightReferenceFinder : ProcessBasedReferenceFinder
    {
        // mdfind -onlyin '[Application.dataPath]' -0 [guid]

        protected override string Command => "mdfind";

        protected override string BuildArguments(string guid) => $"-onlyin '{Application.dataPath}' -0 {guid}";
    }

    internal class MacOsGitGrepReferenceFinder : ProcessBasedReferenceFinder
    {
        // git -C '[Application.dataPath]' grep -z -l [guid]

        protected override string Command => "git";

        protected override string BuildArguments(string guid) => $"-C '{Application.dataPath}' grep -z -l {guid}";
    }

    #endif

    #if UNITY_EDITOR_WIN
    internal class WindowsFindStrReferenceFinder : ProcessBasedReferenceFinder
    {
        // "findstr.exe", "/M /S {1} *", Environment.NewLine

        protected override string Command => "findstr.exe";

        protected override string BuildArguments(string guid) => $"/M /S {guid} *";

        protected override string SplitChar => Environment.NewLine;
    }

    internal class WindowsGitGrepReferenceFinder : ProcessBasedReferenceFinder
    {
        // git.exe -C \"{0}\" grep -z -l {1}

        protected override string Command => "git.exe";

        protected override string BuildArguments(string guid) => $"-C \"{Application.dataPath}\" grep -z -l {guid}";
    }
    #endif

    public class UnityAsset
    {
        public readonly string Path;
        public readonly Object Asset;

        public UnityAsset(string path)
        {
            Path = path;
            Asset = AssetDatabase.LoadMainAssetAtPath(path);
        }
    }

    public class SearchResult : UnityAsset
    {
        public readonly IReadOnlyList<UnityAsset> References;

        public SearchResult(
            string path,
            IReadOnlyList<UnityAsset> references) : base(path)
        {
            References = references;
        }
    }
}
