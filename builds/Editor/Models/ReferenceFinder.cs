#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityReferenceViewer.Editor.ValueObjects;

namespace UnityReferenceViewer.Editor.Models
{
    public class ReferenceFinder
    {
        private static readonly string ProjectPath = Path.GetDirectoryName(Application.dataPath)!;

        private readonly HashSet<string>? _excludeExtensions;
        private readonly IReferenceFinder _referenceFinder;

        public ReferenceFinder(
            IReferenceFinder referenceFinder,
            IEnumerable<string> excludeExtensions)
        {
            _excludeExtensions = new HashSet<string>(excludeExtensions);
            _referenceFinder = referenceFinder;
        }

        public SearchItem FindReferencedFiles(string guid)
        {
            var currentFilePath = AssetDatabase.GUIDToAssetPath(guid);
            var rawReferenceFiles = _referenceFinder.FindReferencedItemPaths(guid);
            var filteredReferenceFiles = FilterValidFiles(currentFilePath, rawReferenceFiles);

            return new SearchItem(
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
}
