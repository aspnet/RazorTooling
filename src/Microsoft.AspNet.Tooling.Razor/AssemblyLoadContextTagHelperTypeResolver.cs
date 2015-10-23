// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class AssemblyLoadContextTagHelperTypeResolver : TagHelperTypeResolver
    {
        private readonly IAssemblyLoadContext _assemblyLoadContext;

        public AssemblyLoadContextTagHelperTypeResolver(IAssemblyLoadContext assemblyLoadContext)
        {
            _assemblyLoadContext = assemblyLoadContext;
        }

        protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            var assembly = _assemblyLoadContext.Load(assemblyName.FullName);

            return assembly.ExportedTypes.Select(type => type.GetTypeInfo());
        }
    }
}
