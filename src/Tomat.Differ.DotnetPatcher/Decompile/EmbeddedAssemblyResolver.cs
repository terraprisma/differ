using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;

namespace Tomat.Differ.DotnetPatcher.Decompile {
    public class EmbeddedAssemblyResolver : IAssemblyResolver {
        private readonly PEFile baseModule;
        private readonly UniversalAssemblyResolver _resolver;
        private readonly Dictionary<string, PEFile> cache = new Dictionary<string, PEFile>();

        public EmbeddedAssemblyResolver(PEFile baseModule, string targetFramework, string sourceOutputDirectory) {
            this.baseModule = baseModule;
            _resolver = new UniversalAssemblyResolver(baseModule.FileName, true, targetFramework, streamOptions: PEStreamOptions.PrefetchMetadata);
            _resolver.AddSearchDirectory(Path.GetDirectoryName(baseModule.FileName));
        }

        public PEFile Resolve(IAssemblyReference name) {
            lock (this) {
                if (cache.TryGetValue(name.FullName, out var module))
                    return module;

                var resName = name.Name + ".dll";
                var res = baseModule.Resources.Where(r => r.ResourceType == ResourceType.Embedded)
                    .SingleOrDefault(r => r.Name.EndsWith(resName));

                if (res != null)
                    module = new PEFile(res.Name, res.TryOpenStream());

                if (module == null)
                    module = _resolver.Resolve(name);

                cache[name.FullName] = module;

                return module;
            }
        }

        public PEFile ResolveModule(PEFile mainModule, string moduleName) => _resolver.ResolveModule(mainModule, moduleName);

        public Task<PEFile?> ResolveAsync(IAssemblyReference reference) {
            return Task.Run<PEFile?>(() => Resolve(reference));
        }

        public Task<PEFile?> ResolveModuleAsync(PEFile mainModule, string moduleName) {
            return Task.Run<PEFile?>(() => ResolveModule(mainModule, moduleName));
        }
    }
}
