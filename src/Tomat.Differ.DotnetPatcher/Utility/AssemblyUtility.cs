using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetPatcher.Utility {
    public class AssemblyUtility {
        private class AttributeTypeProvider : ICustomAttributeTypeProvider<object> {
            public object GetPrimitiveType(PrimitiveTypeCode typeCode) => null;

            public object GetSystemType() => throw new NotImplementedException();

            public object GetSZArrayType(object elementType) => throw new NotImplementedException();

            public object GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw new NotImplementedException();

            public object GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw new NotImplementedException();

            public object GetTypeFromSerializedName(string name) => throw new NotImplementedException();

            public PrimitiveTypeCode GetUnderlyingEnumType(object type) => throw new NotImplementedException();

            public bool IsSystemType(object type) => throw new NotImplementedException();
        }

        private static ConditionalWeakTable<PEFile, string> assemblyTitleCache = new ConditionalWeakTable<PEFile, string>();

        public static string GetAssemblyTitle(PEFile module) {
            if (!assemblyTitleCache.TryGetValue(module, out var title))
                assemblyTitleCache.Add(module, title = GetCustomAttributes(module)[nameof(AssemblyTitleAttribute)]);

            return title;
        }

        private static string[] knownAttributes = { nameof(AssemblyCompanyAttribute), nameof(AssemblyCopyrightAttribute), nameof(AssemblyTitleAttribute) };

        public static IDictionary<string, string> GetCustomAttributes(PEFile module) {
            Dictionary<string, string>? dict = new Dictionary<string, string>();

            var reader = module.Reader.GetMetadataReader();
            var attribs = reader.GetAssemblyDefinition()
                .GetCustomAttributes()
                .Select(reader.GetCustomAttribute);

            foreach (var attrib in attribs) {
                var ctor = reader.GetMemberReference((MemberReferenceHandle)attrib.Constructor);
                var attrTypeName = reader.GetString(
                    reader.GetTypeReference((TypeReferenceHandle)ctor.Parent)
                        .Name
                );

                if (!knownAttributes.Contains(attrTypeName))
                    continue;

                CustomAttributeValue<object> value = attrib.DecodeValue(new AttributeTypeProvider());
                dict[attrTypeName] = (string)value.FixedArguments.Single()
                    .Value;
            }

            return dict;
        }
    }
}
