#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityReferenceViewer.Editor.Models
{
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
}
