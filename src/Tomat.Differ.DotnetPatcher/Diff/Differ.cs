using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeChicken.DiffPatch;
using DotnetPatcher.Utility;
using DiffPatcher = CodeChicken.DiffPatch.Patcher;
using DiffDiffer = CodeChicken.DiffPatch.Differ;

namespace DotnetPatcher.Diff {
    public class Differ {
        private static string[] DiffableFileExtensions = { ".cs", ".csproj", ".ico", ".resx", ".png", "App.config", ".json", ".targets", ".txt", ".bat", ".sh" };

        public static bool IsDiffable(string relPath) => DiffableFileExtensions.Any(relPath.EndsWith);

        private static readonly string RemovedFileList = "removed_files.list";
        private static readonly Regex HunkOffsetRegex = new Regex(@"@@ -(\d+),(\d+) \+([_\d]+),(\d+) @@", RegexOptions.Compiled);

        public string SourcePath;
        public string PatchPath;
        public string PatchedPath;

        public Differ(string sourcePath, string patchPath, string patchedPath) {
            SourcePath = sourcePath;
            PatchPath = patchPath;
            PatchedPath = patchedPath;
        }

        public void Diff() {
            List<WorkTask> items = new List<WorkTask>();

            foreach ((var file, var relPath) in DirectoryUtility.EnumerateSrcFiles(PatchedPath)) {
                if (!File.Exists(Path.Combine(SourcePath, relPath))) {
                    items.Add(new WorkTask(() => DirectoryUtility.Copy(file, Path.Combine(PatchPath, relPath))));
                }
                else if (IsDiffable(relPath)) {
                    items.Add(new WorkTask(() => DiffFile(relPath)));
                }
            }

            WorkTask.ExecuteParallel(items);

            foreach ((var file, var relPath) in DirectoryUtility.EnumerateFiles(PatchPath)) {
                var targetPath = relPath.EndsWith(".patch") ? relPath.Substring(0, relPath.Length - 6) : relPath;
                if (!File.Exists(Path.Combine(PatchedPath, targetPath))) {
                    DirectoryUtility.DeleteFile(file);
                }
            }

            DirectoryUtility.DeleteEmptyDirs(PatchPath);

            string[] removedFiles =
                DirectoryUtility.EnumerateSrcFiles(SourcePath)
                    .Where(f => !f.relPath.StartsWith(".git" + Path.DirectorySeparatorChar) && !File.Exists(Path.Combine(PatchedPath, f.relPath)))
                    .Select(f => f.relPath)
                    .ToArray();

            var removedFileList = Path.Combine(PatchPath, RemovedFileList);
            if (removedFiles.Length > 0) {
                File.WriteAllLines(removedFileList, removedFiles);
            }
            else {
                DirectoryUtility.DeleteFile(removedFileList);
            }
        }

        private void DiffFile(string relPath) {
            var patchFile = DiffDiffer.DiffFiles(
                new LineMatchedDiffer(),
                Path.Combine(SourcePath, relPath)
                    .Replace('\\', '/'),
                Path.Combine(PatchedPath, relPath)
                    .Replace('\\', '/')
            );

            var patchPath = Path.Combine(PatchPath, relPath + ".patch");
            if (!patchFile.IsEmpty) {
                DirectoryUtility.CreateParentDirectory(patchPath);
                File.WriteAllText(patchPath, patchFile.ToString(true));
            }
            else {
                DirectoryUtility.DeleteFile(patchPath);
            }
        }
    }
}
