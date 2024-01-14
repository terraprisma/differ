﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Xml;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetPatcher.Utility {
    public class ProjectFileUtility {
        public static WorkTask WriteProjectFile(PEFile module, string outputType, string projectOutputDirectory, IEnumerable<string> sources, IEnumerable<string> resources, Action<XmlTextWriter> writeSpecificConfig) {
            var name = AssemblyUtility.GetAssemblyTitle(module);
            var filename = name + ".csproj";
            return new WorkTask(
                () => {
                    var path = Path.Combine(projectOutputDirectory, name, filename);
                    DirectoryUtility.CreateParentDirectory(path);

                    using (var sw = new StreamWriter(path))
                    using (var w = new XmlTextWriter(sw)) {
                        w.Formatting = Formatting.Indented;
                        w.WriteStartElement("Project");
                        w.WriteAttributeString("Sdk", "Microsoft.NET.Sdk");

                        w.WriteStartElement("Import");
                        w.WriteAttributeString("Project", "../Configuration.targets");
                        w.WriteEndElement(); // </Import>

                        w.WriteStartElement("PropertyGroup");
                        w.WriteElementString("OutputType", outputType);
                        w.WriteElementString("Nullable", "enable");
                        w.WriteElementString("Version", new AssemblyName(module.FullName).Version.ToString());

                        IDictionary<string, string> attribs = AssemblyUtility.GetCustomAttributes(module);

                        foreach (KeyValuePair<string, string> attrib in attribs) {
                            switch (attrib.Key) {
                                case nameof(AssemblyCompanyAttribute):
                                    w.WriteElementString("Company", attrib.Value);
                                    break;

                                case nameof(AssemblyCopyrightAttribute):
                                    w.WriteElementString("Company", attrib.Value);
                                    break;
                            }
                        }

                        w.WriteElementString("RootNamespace", module.Name);
                        w.WriteEndElement(); // </PropertyGroup>

                        writeSpecificConfig(w);

                        // resources
                        w.WriteStartElement("ItemGroup");
                        foreach (var r in ApplyWildcards(resources, sources.ToArray())
                                     .OrderBy(r => r)) {
                            w.WriteStartElement("EmbeddedResource");
                            w.WriteAttributeString("Include", $"{r}");
                            w.WriteEndElement();
                        }

                        w.WriteEndElement(); // </ItemGroup>
                        w.WriteEndElement(); // </Project>

                        sw.Write(Environment.NewLine);
                    }
                }
            );
        }

        public static WorkTask WriteCommonConfigurationFile(string projectOutputDirectory) {
            var filename = "Configuration.targets";
            return new WorkTask(
                () => {
                    var path = Path.Combine(projectOutputDirectory, filename);
                    DirectoryUtility.CreateParentDirectory(path);

                    using (var sw = new StreamWriter(path))
                    using (var w = new XmlTextWriter(sw)) {
                        w.Formatting = Formatting.Indented;
                        w.WriteStartElement("Project");

                        w.WriteStartElement("PropertyGroup");
                        w.WriteElementString("Configurations", "Debug;Release");
                        w.WriteElementString("AssemblySearchPaths", "$(AssemblySearchPaths);{GAC}");
                        w.WriteElementString("AllowUnsafeBlocks", "true");
                        w.WriteElementString("Optimize", "true");
                        w.WriteEndElement(); // </PropertyGroup>

                        w.WriteStartElement("PropertyGroup");
                        w.WriteAttributeString("Condition", "$(Configuration.Contains('Debug'))");
                        w.WriteElementString("Optimize", "false");
                        w.WriteElementString("DefineConstants", "$(DefineConstants);DEBUG");
                        w.WriteEndElement();

                        w.WriteEndElement(); // </Project>

                        sw.Write(Environment.NewLine);
                    }
                }
            );
        }

        public static IEnumerable<string> ApplyWildcards(IEnumerable<string> include, IReadOnlyList<string> exclude) {
            var wildpaths = new HashSet<string>();
            foreach (var path in include) {
                if (wildpaths.Any(path.StartsWith))
                    continue;

                var wpath = path;
                var cards = "";
                while (wpath.Contains('/')) {
                    var parent = wpath.Substring(0, wpath.LastIndexOf('/'));
                    if (exclude.Any(e => e.StartsWith(parent)))
                        break; //can't use parent as a wildcard

                    wpath = parent;
                    if (cards.Length < 2)
                        cards += "*";
                }

                if (wpath != path) {
                    wildpaths.Add(wpath);
                    yield return $"{wpath}/{cards}";
                }
                else {
                    yield return path;
                }
            }
        }

        public static void AddEmbeddedLibrary(Resource res, string projectOutputDirectory, DecompilerUtility.ExtendedProjectDecompiler decompiler, DecompilerSettings settings, IAssemblyResolver resolver, List<WorkTask> items) {
            using var s = res.TryOpenStream();
            s.Position = 0;
            var module = new PEFile(res.Name, s, PEStreamOptions.PrefetchEntireImage);
            AddLibrary(module, projectOutputDirectory, decompiler, settings, resolver, items);
        }

        public static void AddLibrary(PEFile module, string projectOutputDirectory, DecompilerUtility.ExtendedProjectDecompiler decompiler, DecompilerSettings settings, IAssemblyResolver resolver, List<WorkTask> items) {
            var files = new HashSet<string>();
            HashSet<string> resources = new HashSet<string>();
            DecompilerUtility.DecompileModule(module, resolver, decompiler, items, files, resources, projectOutputDirectory, settings);
            items.Add(
                WriteProjectFile(
                    module,
                    "Library",
                    projectOutputDirectory,
                    files,
                    resources,
                    w => {
                        w.WriteStartElement("ItemGroup");
                        foreach (var r in module.AssemblyReferences.OrderBy(r => r.Name)) {
                            if (r.Name == "mscorlib") continue;

                            w.WriteStartElement("Reference");
                            w.WriteAttributeString("Include", r.Name);
                            w.WriteEndElement();
                        }

                        w.WriteEndElement();
                    }
                )
            );
        }

        public static WorkTask WriteProjectFile(PEFile module, string projectOutputDirectory, IEnumerable<string> sources, IEnumerable<string> resources, ICollection<string>? decompiledLibraries) {
            return WriteProjectFile(
                module,
                "WinExe",
                projectOutputDirectory,
                sources,
                resources,
                w => {
                    //configurations
                    w.WriteStartElement("ItemGroup");
                    foreach (var r in module.AssemblyReferences.OrderBy(r => r.Name)) {
                        if (r.Name == "mscorlib") continue;

                        if (decompiledLibraries?.Contains(r.Name) ?? false) {
                            w.WriteStartElement("ProjectReference");
                            w.WriteAttributeString("Include", $"../{r.Name}/{r.Name}.csproj");
                            w.WriteEndElement();

                            w.WriteStartElement("EmbeddedResource");
                            w.WriteAttributeString("Include", $"../{r.Name}/bin/$(Configuration)/$(TargetFramework)/{r.Name}.dll");
                        }
                        else {
                            w.WriteStartElement("Reference");
                            w.WriteAttributeString("Include", $"{r.Name}");
                        }

                        w.WriteEndElement();
                    }

                    w.WriteEndElement(); // </ItemGroup>
                }
            );
        }
    }
}
