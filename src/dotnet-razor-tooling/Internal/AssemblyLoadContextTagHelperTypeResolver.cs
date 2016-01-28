// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public class AssemblyLoadContextTagHelperTypeResolver : TagHelperTypeResolver
    {
        private static readonly string ITagHelperFullName = typeof(ITagHelper).FullName;

        private readonly AssemblyLoadContext _loadContext;

        public AssemblyLoadContextTagHelperTypeResolver(AssemblyLoadContext loadContext)
        {
            if (loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }

            _loadContext = loadContext;
        }

        protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            var assembly = _loadContext.LoadFromAssemblyName(assemblyName);
            var exportedTypeInfos = assembly.ExportedTypes.Select(type => type.GetTypeInfo()).ToArray();
            return exportedTypeInfos;
        }

        protected override bool IsTagHelper(TypeInfo typeInfo)
        {
            return !typeInfo.IsNested &&
                typeInfo.IsPublic &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                ImplementsITagHelper(typeInfo);
        }

        private bool ImplementsITagHelper(TypeInfo typeInfo)
        {
            if (string.Equals(typeInfo.FullName, ITagHelperFullName, StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var implementedInterface in typeInfo.ImplementedInterfaces)
            {
                if (ImplementsITagHelper(implementedInterface.GetTypeInfo()))
                {
                    return true;
                }
            }

            if (typeInfo.BaseType != null)
            {
                return ImplementsITagHelper(typeInfo.BaseType.GetTypeInfo());
            }

            return false;
        }
    }
}
