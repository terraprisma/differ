using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tomat.Differ.Nodes;

/// <summary>
///     The JSON representation of a node.
/// </summary>
public sealed class MetaNode {
    [JsonProperty("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("patchDir")]
    public string PatchDir { get; set; } = string.Empty;

    [JsonProperty("parent")]
    public string? Parent { get; set; } = null;

    [JsonProperty("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    [JsonProperty("children")]
    public MetaNode[] Children { get; set; } = Array.Empty<MetaNode>();
}
