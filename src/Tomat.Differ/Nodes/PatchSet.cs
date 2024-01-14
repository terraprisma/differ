using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Tomat.Differ.Nodes;

public sealed class PatchSet {
    public List<PatchSet> Dependencies { get; } = new();

    public List<DiffNode> Nodes { get; } = new();

    public IEnumerable<DiffNode> GetTopLevelNodes() {
        foreach (var node in Nodes) {
            if (node.Parent is null)
                yield return node;
        }

        foreach (var dependency in Dependencies) {
            foreach (var node in dependency.GetTopLevelNodes())
                yield return node;
        }
    }

    public IEnumerable<DiffNode> GetAllNodes(DiffNode? node = null) {
        if (node is null) {
            foreach (var child in GetTopLevelNodes().SelectMany(GetAllNodes))
                yield return child;
        }
        else {
            yield return node;

            foreach (var childChild in node.Children.SelectMany(GetAllNodes))
                yield return childChild;
        }
    }

    public static PatchSet FromFile(string path) {
        if (!File.Exists(path))
            throw new FileNotFoundException("The specified file does not exist.", path);

        return FromJson(File.ReadAllText(path), Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty);
    }

    public static PatchSet FromJson(string json, string rootDir) {
        var meta = JsonConvert.DeserializeObject<MetaSet>(json);
        if (meta is null)
            throw new JsonException("The specified JSON is invalid.");

        return FromMeta(meta, rootDir);
    }

    public static PatchSet FromMeta(MetaSet meta, string rootDir) {
        var patchSet = new PatchSet();

        foreach (var dependencyPath in meta.Dependencies) {
            var fullyQualifiedDependencyPath = dependencyPath;
            if (!Path.IsPathFullyQualified(fullyQualifiedDependencyPath))
                fullyQualifiedDependencyPath = Path.Combine(rootDir, fullyQualifiedDependencyPath);

            var dependency = FromFile(fullyQualifiedDependencyPath);
            patchSet.Dependencies.Add(dependency);
        }

        foreach (var nodeMeta in meta.Nodes) {
            var node = DiffNode.FromMeta(nodeMeta, rootDir);
            patchSet.Nodes.Add(node);

            if (node.Parent is not null || node.MetaNode.Parent is null)
                continue;

            var parent = patchSet.GetAllNodes().First(n => n.MetaNode.Name == node.MetaNode.Parent);
            node.Parent = parent;
            parent.Children.Add(node);
        }

        return patchSet;
    }
}
