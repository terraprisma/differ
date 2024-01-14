using System;
using System.Linq;
using Spectre.Console;
using Tomat.Differ;
using Tomat.Differ.Nodes;

const string file_exclusion_regex = @"^.*(?<!\.xnb)(?<!\.xwb)(?<!\.xsb)(?<!\.xgs)(?<!\.bat)(?<!\.txt)(?<!\.xml)(?<!\.msi)$";
const string install_depots = "Install Depots";
const string decompile_depots = "Decompile Depots";
const string diff_all_depots = "Diff All Depots";
const string diff_all_mods = "Diff All Mods";
const string diff_patch = "Diff Single Workspace";
const string patch_all_mods = "Patch All Mods";
const string patch_patch = "Patch Single Workspace";

if (args.Length != 1)
    Console.WriteLine("No patches file provided, assuming relative patches.json");

var differ = new PatchSetHandler(PatchSet.FromFile(args.Length >= 1 ? args[0] : "patches.json"));
var task = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select task").AddChoices(install_depots, decompile_depots, diff_all_depots, diff_all_mods, diff_patch, patch_all_mods, patch_patch));

switch (task) {
    // Reinstalls all depots.
    case install_depots:
        Console.Write("Steam username: ");
        var username = Console.ReadLine()!;
        Console.Write("Steam password: ");
        var password = Console.ReadLine()!;
        differ.DownloadDepots(username, password, file_exclusion_regex);
        break;

    // Decompiles all depots.
    case decompile_depots:
        differ.DecompileDepots(differ.GetNodesOfType<DepotNode>());
        break;

    // Diffs all depots.
    case diff_all_depots:
        differ.DiffNodes(differ.GetNodesOfType<DepotNode>());
        break;

    // Diffs all mods.
    case diff_all_mods:
        differ.DiffNodes(differ.GetNodesOfType<ModNode>());
        break;

    // Diffs a single workspace.
    case diff_patch:
        differ.DiffNodes(differ.GetNodeWithName(selectNode()));
        break;

    // Applies patches to all mods.
    case patch_all_mods:
        differ.PatchNodes(differ.GetNodesOfType<ModNode>());
        break;

    // Applies patches to a single workspace.
    case patch_patch:
        differ.PatchNodes(differ.GetNodeWithName(selectNode()));
        break;

    default:
        throw new Exception("No task selected");
}

return;

string selectNode() {
    return AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select node").AddChoices(differ.GetAllNodes().Select(x => x.Name)));
}
