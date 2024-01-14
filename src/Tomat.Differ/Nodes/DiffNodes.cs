using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tomat.Differ.Nodes;

public abstract class DiffNode {
    protected const string KIND_DEPOT = "depot";
    protected const string KIND_MOD = "mod";

    public abstract string Kind { get; }

    public string Name => MetaNode.Name;

    public string PatchDir => MetaNode.PatchDir;

    public DiffNode? Parent { get; set; }

    public List<DiffNode> Children { get; } = new();

    public MetaNode MetaNode { get; }

    protected DiffNode(MetaNode metaNode) {
        MetaNode = metaNode;
    }

    public static DiffNode FromMeta(MetaNode meta, string rootDir) {
        DiffNode node = meta.Kind switch {
            KIND_DEPOT => new DepotNode(meta),
            KIND_MOD => new ModNode(meta),
            _ => throw new JsonException($"The specified JSON has an invalid kind: {meta.Kind}"),
        };

        foreach (var childMeta in meta.Children) {
            var childNode = FromMeta(childMeta, rootDir);
            node.Children.Add(childNode);
            childNode.Parent = node;
        }

        if (!Path.IsPathFullyQualified(meta.PatchDir))
            meta.PatchDir = Path.Combine(rootDir, meta.PatchDir);

        return node;
    }
}

public sealed class DepotNode : DiffNode {
    public override string Kind => KIND_DEPOT;

    public string PathToExecutable => MetaNode.Data["pathToExecutable"] as string ?? string.Empty;

    public int AppId => (int) (long) MetaNode.Data["appId"];

    public int DepotId => (int) (long) MetaNode.Data["depotId"];

    public DepotNode(MetaNode metaNode) : base(metaNode) { }
}

public sealed class ModNode : DiffNode {
    public override string Kind => KIND_MOD;

    public ModNode(MetaNode metaNode) : base(metaNode) { }
}
