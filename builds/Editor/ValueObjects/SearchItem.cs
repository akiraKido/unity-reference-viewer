#nullable enable
using System.Collections.Generic;

namespace UnityReferenceViewer.Editor.ValueObjects
{
    public class SearchItem : UnityAsset
    {
        public readonly IReadOnlyList<UnityAsset> References;

        public SearchItem(
            string path,
            IReadOnlyList<UnityAsset> references) : base(path)
        {
            References = references;
        }
    }
}
