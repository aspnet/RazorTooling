// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class AssemblyTagHelperDescriptorResolver
    {
        private readonly TagHelperDescriptorFactory _descriptorFactory = new TagHelperDescriptorFactory(designTime: true);

        public int Protocol { get; set; } = 1;

        public IEnumerable<TagHelperDescriptor> Resolve(
            string assemblyName,
            IAssemblyLoadContext assemblyLoadContext,
            ErrorSink errorSink)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (assemblyLoadContext == null)
            {
                throw new ArgumentNullException(nameof(assemblyLoadContext));
            }

            if (Protocol == 1)
            {
                var tagHelperTypes = GetTagHelperTypes(assemblyName, assemblyLoadContext, errorSink);
                var tagHelperDescriptors = new List<TagHelperDescriptor>();
                foreach (var tagHelperType in tagHelperTypes)
                {
                    var descriptors = _descriptorFactory.CreateDescriptors(assemblyName, tagHelperType, errorSink);
                    tagHelperDescriptors.AddRange(descriptors);
                }

                return tagHelperDescriptors;
            }
            else
            {
                // Unknown protocol
                throw new InvalidOperationException(
                    Resources.FormatInvalidProtocolValue(typeof(TagHelperDescriptor).FullName, Protocol));
            }
        }

        /// <summary>
        /// Protected virtual for testing.
        /// </summary>
        protected virtual IEnumerable<ITypeInfo> GetTagHelperTypes(
            string assemblyName,
            IAssemblyLoadContext assemblyLoadContext,
            ErrorSink errorSink)
        {
            var tagHelperTypeResolver = new AssemblyLoadContextTagHelperTypeResolver(assemblyLoadContext);
            var tagHelperTypes = tagHelperTypeResolver.Resolve(assemblyName, SourceLocation.Zero, errorSink);

            return tagHelperTypes;
        }
    }
}
