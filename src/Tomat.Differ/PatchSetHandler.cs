using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

using Reaganism.CDC.Decompilation;
using Reaganism.CDC.Diffing;
using Reaganism.CDC.Patching;
using Reaganism.CDC.Utilities.Extensions;

using Tomat.Differ.Nodes;
using Tomat.Differ.Transformation;
using Tomat.Differ.Transformation.Transformers;

namespace Tomat.Differ;

public sealed class PatchSetHandler {
    private const string downloads_dir = "downloads";
    private const string decompilation_dir = "decompiled";
    private const string cloned_dir = "cloned";
    private static readonly string[] decompiled_libraries = { "ReLogic", "RailSDK.Net", "SteelSeriesEngineWrapper" };

    public PatchSet PatchSet { get; }

    public PatchSetHandler(PatchSet patchSet) {
        PatchSet = patchSet;
    }

    public IEnumerable<DiffNode> GetAllNodes() {
        return PatchSet.GetAllNodes();
    }

    public void DownloadDepots(string username, string password, string regex) {
        File.WriteAllText("filelist.txt", "regex:" + regex);

        var hashset = new HashSet<(int, int)>();
        foreach (var node in GetNodesOfType<DepotNode>()) {
            if (hashset.Contains((node.AppId, node.DepotId)))
                continue;

            hashset.Add((node.AppId, node.DepotId));
            DownloadManifest(username, password, node.AppId, node.DepotId);
        }
    }

    public IEnumerable<DiffNode> GetNodeWithName(string name) {
        return GetAllNodes().Where(node => node.Name == name);
    }

    public IEnumerable<T> GetNodesOfType<T>() where T : DiffNode {
        return GetAllNodes().OfType<T>();
    }

    public void DecompileDepots(IEnumerable<DepotNode> nodes) {
        foreach (var node in nodes) {
            var dir = Path.Combine(decompilation_dir, node.Name);
            Console.WriteLine($"Decompiling {node.Name}");
            Console.WriteLine("Transforming assemblies");

            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            Directory.CreateDirectory(dir);

            var depotDir = Path.Combine(downloads_dir, node.AppId.ToString(), node.DepotId.ToString());
            if (!Directory.Exists(depotDir))
                throw new Exception($"Depot {node.Name} was not downloaded!");

            var clonedDir = Path.Combine(cloned_dir, node.Name);
            if (Directory.Exists(clonedDir))
                Directory.Delete(clonedDir, true);

            CopyRecursively(depotDir, clonedDir);

            var exePath = Path.Combine(clonedDir, node.PathToExecutable);

            var context = AssemblyTransformer.GetAssemblyContextWithUniversalAssemblyResolverFromPath(exePath);
            AssemblyTransformer.TransformAssembly(context, new DecompilerParityTransformer());

            var decompilerSettings = new DecompilerSettings
            {
                CSharpFormattingOptions = FormattingOptionsFactory.CreateKRStyle(),
                Ranges = false,
            };
            ProjectDecompiler.Decompile(exePath, dir, decompilerSettings, decompiled_libraries);
        }
    }

    public void DiffNodes(IEnumerable<DiffNode> nodes) {
        foreach (var node in nodes) {
            var parent = node.Parent;

            // If there is no parent node to diff against, create an empty
            // directory for this node and continue, since there's nothing to
            // diff against.
            if (parent is null) {
                Directory.CreateDirectory(node.PatchDir);
                continue;
            }

            Console.WriteLine($"Diffing {node.Name}");

            if (Directory.Exists(node.PatchDir))
                Directory.Delete(node.PatchDir, true);

            Directory.CreateDirectory(node.PatchDir);

            var differSettings = new DifferSettings(Path.Combine(decompilation_dir, parent.Name), Path.Combine(decompilation_dir, node.Name), node.PatchDir)
                                .IgnoreCommonDirectories()
                                .HandleCommonFileTypes();
            ProjectDiffer.Diff(differSettings);
        }
    }

    public void PatchNodes(IEnumerable<DiffNode> nodes) {
        foreach (var node in nodes) {
            var parent = node.Parent;

            // If there is no parent node to patch against, create an empty
            // directory for this node and continue, since there's nothing to
            // patch against.
            if (parent is null) {
                Directory.CreateDirectory(Path.Combine(decompilation_dir, node.Name));
                continue;
            }

            Console.WriteLine($"Patching {node.Name}");

            if (!Directory.Exists(node.PatchDir))
                Directory.CreateDirectory(node.PatchDir);

            if (Directory.Exists(Path.Combine(decompilation_dir, node.Name)))
                Directory.Delete(Path.Combine(decompilation_dir, node.Name), true);

            var patcherSettings = new PatcherSettings(Path.Combine(decompilation_dir, parent.Name), Path.Combine(decompilation_dir, node.Name), node.PatchDir);
            ProjectPatcher.Patch(patcherSettings);
        }
    }

    private static void CopyRecursively(string fromDir, string toDir) {
        foreach (var dir in Directory.GetDirectories(fromDir, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(fromDir, toDir));

        foreach (var file in Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(fromDir, toDir), true);
    }

    private static void DownloadManifest(string username, string password, int appId, int depotId) {
        var dir = Path.Combine(downloads_dir, appId.ToString(), depotId.ToString());

        if (Directory.Exists(dir))
            Directory.Delete(dir, true);

        DepotDownloader.Program.Main(
            new[] {
                "-app",
                appId.ToString(),
                "-depot",
                depotId.ToString(),
                "-filelist",
                "filelist.txt",
                "-username",
                username,
                "-password",
                password,
                "-dir",
                dir,
                //"-remember-password",
            }
        );
    }
}
