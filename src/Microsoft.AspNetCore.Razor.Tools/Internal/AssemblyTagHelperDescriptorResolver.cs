// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.Tools;

namespace Microsoft.AspNetCore.Razor.Tools.Internal
{
    public class AssemblyTagHelperDescriptorResolver
    {
        private readonly TagHelperDescriptorFactory _descriptorFactory = new TagHelperDescriptorFactory(designTime: true);
        private readonly TagHelperTypeResolver _tagHelperTypeResolver;

        public AssemblyTagHelperDescriptorResolver()
        {
            _tagHelperTypeResolver = new TagHelperTypeResolver();
        }

        public static int DefaultProtocolVersion { get; } = 1;

        public int ProtocolVersion { get; set; } = DefaultProtocolVersion;

        public IEnumerable<TagHelperDescriptor> Resolve(string assemblyName, ErrorSink errorSink)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (ProtocolVersion == 1)
            {
                var tagHelperTypes = GetTagHelperTypes(assemblyName, errorSink);
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
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.InvalidProtocolValue,
                        typeof(TagHelperDescriptor).FullName, ProtocolVersion));
            }
        }

        /// <summary>
        /// Protected virtual for testing.
        /// </summary>
        protected virtual IEnumerable<Type> GetTagHelperTypes(string assemblyName, ErrorSink errorSink)
        {
            return _tagHelperTypeResolver.Resolve(assemblyName, SourceLocation.Zero, errorSink);
        }
    }
}
