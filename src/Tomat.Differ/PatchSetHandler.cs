using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using Tomat.Differ.DotnetPatcher.Decompile;
using Tomat.Differ.Nodes;
using Tomat.Differ.Transformation;
using Tomat.Differ.Transformation.Transformers;

[assembly: InternalsVisibleTo("Tomat.Differ.Build")]

namespace Tomat.Differ;

public sealed class PatchSetHandler {
    private const string decompilation_dir = "decompiled";
    private const string patches_dir = "patches";
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

    public void DecompileDepots() {
        foreach (var node in GetNodesOfType<DepotNode>()) {
            var dir = Path.Combine(decompilation_dir, node.Name);
            Console.WriteLine($"Decompiling {node.Name}");
            Console.WriteLine("Transforming assemblies");

            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            Directory.CreateDirectory(dir);

            var depotDir = Path.Combine("downloads", node.AppId.ToString(), node.DepotId.ToString());
            if (!Directory.Exists(depotDir))
                throw new Exception($"Depot {node.Name} was not downloaded!");

            var clonedDir = Path.Combine(cloned_dir, node.Name);
            if (Directory.Exists(clonedDir))
                Directory.Delete(clonedDir, true);

            CopyRecursively(depotDir, clonedDir);

            var exePath = Path.Combine(clonedDir, node.PathToExecutable);

            var context = AssemblyTransformer.GetAssemblyContextWithUniversalAssemblyResolverFromPath(exePath);
            AssemblyTransformer.TransformAssembly(context, new DecompilerParityTransformer());

            var decompiler = new Decompiler(
                exePath,
                dir,
                new DecompilerSettings {
                    CSharpFormattingOptions = FormattingOptionsFactory.CreateKRStyle(),
                    Ranges = false,
                }
            );

            decompiler.Decompile(decompiled_libraries);
        }
    }

    public IEnumerable<DiffNode> GetNodeWithName(string name) {
        return GetAllNodes().Where(node => node.Name == name);
    }

    public IEnumerable<T> GetNodesOfType<T>() where T : DiffNode {
        return GetAllNodes().OfType<T>();
    }

    private void DecompileAndDiffDepotNodes(DiffNode node, DiffNode? parent = null) {
        //foreach (var child in node.Children)
        //    DecompileAndDiffDepotNodes(child, node);
        //
        //if (parent is null) {
        //    // Create an empty patches directory for the root node.
        //    Directory.CreateDirectory(Path.Combine(patches_dir, node.WorkspaceName));
        //    return;
        //}
        //
        //if (Environment.GetEnvironmentVariable("SKIP_DIFFING") == "1")
        //    return;
        //
        //Console.WriteLine($"Diffing {node.WorkspaceName}...");
        //
        //var patchDirName = Path.Combine(patches_dir, node.WorkspaceName);
        //if (Directory.Exists(patchDirName))
        //    Directory.Delete(patchDirName, true);
        //
        //Directory.CreateDirectory(patchDirName);
        //
        //var differ = new DotnetPatcher.Diff.Differ(Path.Combine(decompilation_dir, parent.WorkspaceName), patchDirName, dirName);
        //differ.Diff();
    }

    private void DiffModNodes(DiffNode node, DiffNode? parent = null) {
        // if (Environment.GetEnvironmentVariable("ONLY_NODE") is { } onlyNode && node.WorkspaceName != onlyNode) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it isn't the expected node ({onlyNode})...");
        //     foreach (var child in node.Children)
        //         DiffModNodes(child, node);
        // 
        //     return;
        // }
        // 
        // if (node is not ModDiffNode modNode) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it isn't a mod node...");
        //     foreach (var child in node.Children)
        //         DiffModNodes(child, node);
        // 
        //     return;
        // }
        // 
        // if (parent is null) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it is a root node...");
        //     return;
        // }
        // 
        // Console.WriteLine($"Diffing {node.WorkspaceName}...");
        // 
        // var patchDirName = Path.Combine("patches", node.WorkspaceName);
        // if (Directory.Exists(patchDirName))
        //     Directory.Delete(patchDirName, true);
        // 
        // Directory.CreateDirectory(patchDirName);
        // 
        // var differ = new DotnetPatcher.Diff.Differ(Path.Combine("decompiled", parent.WorkspaceName), patchDirName, Path.Combine("decompiled", node.WorkspaceName));
        // differ.Diff();
        // 
        // foreach (var child in node.Children)
        //     DiffModNodes(child, node);
    }

    private void PatchModNodes(DiffNode node, DiffNode? parent = null) {
        // if (Environment.GetEnvironmentVariable("ONLY_NODE") is { } onlyNode && node.WorkspaceName != onlyNode) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it isn't the expected node ({onlyNode})...");
        //     foreach (var child in node.Children)
        //         PatchModNodes(child, node);
        // 
        //     return;
        // }
        // 
        // if (node is not ModDiffNode /*modNode*/) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it isn't a mod node...");
        //     foreach (var child in node.Children)
        //         PatchModNodes(child, node);
        // 
        //     return;
        // }
        // 
        // if (parent is null) {
        //     Console.WriteLine($"Skipping {node.WorkspaceName} since it is a root node...");
        //     return;
        // }
        // 
        // Console.WriteLine($"Patching {node.WorkspaceName}...");
        // 
        // var patchDirName = Path.Combine("patches", node.WorkspaceName);
        // if (!Directory.Exists(patchDirName))
        //     Directory.CreateDirectory(patchDirName);
        // 
        // if (Directory.Exists(Path.Combine("decompiled", node.WorkspaceName)))
        //     Directory.Delete(Path.Combine("decompiled", node.WorkspaceName), true);
        // 
        // var patcher = new Patcher(Path.Combine("decompiled", parent.WorkspaceName), Path.Combine("patches", node.WorkspaceName), Path.Combine("decompiled", node.WorkspaceName));
        // patcher.Patch();
        // 
        // foreach (var child in node.Children)
        //     PatchModNodes(child, node);
    }

    private static void CopyRecursively(string fromDir, string toDir) {
        foreach (var dir in Directory.GetDirectories(fromDir, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(fromDir, toDir));

        foreach (var file in Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(fromDir, toDir), true);
    }

    private static void DownloadManifest(string username, string password, int appId, int depotId) {
        var dir = Path.Combine("downloads", appId.ToString(), depotId.ToString());

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
