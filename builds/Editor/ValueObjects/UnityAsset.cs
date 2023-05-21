#nullable enable
using UnityEditor;
using UnityEngine;

namespace UnityReferenceViewer.Editor.ValueObjects
{
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
}
