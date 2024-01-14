using System;
using Newtonsoft.Json;

namespace Tomat.Differ.Nodes;

/// <summary>
///     The JSON representation of a patchset.
/// </summary>
public class MetaSet {
    /// <summary>
    ///     An array of paths to dependency patchsets.
    /// </summary>
    [JsonProperty("dependencies")]
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     An array of nodes.
    /// </summary>
    [JsonProperty("nodes")]
    public MetaNode[] Nodes { get; set; } = Array.Empty<MetaNode>();
}
