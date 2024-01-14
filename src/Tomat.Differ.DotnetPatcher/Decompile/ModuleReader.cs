using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetPatcher {
    public class ModuleReader {
        public static PEFile ReadModule(string path, bool createBackup) {
            if (!File.Exists(path)) {
                throw new FileNotFoundException($"Could not find file {path}");
            }

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                var module = new PEFile(path, fileStream, PEStreamOptions.PrefetchEntireImage);
                var assemblyName = new AssemblyName(module.FullName);

                var versionedPath = $"{path}_Backup";
                if (!File.Exists(versionedPath)) {
                    File.Copy(path, versionedPath);
                }

                return module;
            }
        }
    }
}
