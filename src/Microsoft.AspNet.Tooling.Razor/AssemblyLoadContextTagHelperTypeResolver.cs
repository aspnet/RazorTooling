// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class AssemblyLoadContextTagHelperTypeResolver : TagHelperTypeResolver
    {
        protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            return assembly.ExportedTypes.Select(type => type.GetTypeInfo());
        }
    }
}
