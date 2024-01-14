using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetPatcher.Utility {
    public class ResourceUtility {
        public static void ExtractResource(string projectOutputDirectory, string name, Resource res, string projectDir) {
            var path = Path.Combine(projectOutputDirectory, projectDir, name);
            DirectoryUtility.CreateParentDirectory(path);

            var s = res.TryOpenStream();
            s.Position = 0;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                s.CopyTo(fs);
        }

        public static WorkTask ExtractResourceAsync(string projectOutputDirectory, string name, Resource res, string projectDir) {
            return new WorkTask(
                () => {
                    ExtractResource(projectOutputDirectory, name, res, projectDir);
                }
            );
        }

        public static IEnumerable<(string path, Resource r)> GetResourceFiles(PEFile module) {
            return module.Resources.Where(r => r.ResourceType == ResourceType.Embedded)
                .Select(res => (DirectoryUtility.GetOutputPath(res.Name, module), res));
        }
    }
}
