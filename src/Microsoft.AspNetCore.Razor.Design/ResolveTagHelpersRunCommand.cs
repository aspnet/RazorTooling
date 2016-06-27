// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Design.Internal;

namespace Microsoft.AspNetCore.Razor.Design
{
    public class ResolveTagHelpersRunCommand : ResolveTagHelpersCommandBase
    {
        protected override int OnExecute()
        {
            int protocol;
            if (ProtocolOption.HasValue())
            {
                var protocolOptionValue = ProtocolOption.Value();
                if (!int.TryParse(protocolOptionValue, out protocol))
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            DesignResources.CouldNotParseProvidedProtocol,
                            protocolOptionValue));
                    return 0;
                }
            }
            else
            {
                protocol = AssemblyTagHelperDescriptorResolver.DefaultProtocolVersion;
            }

            var descriptorResolver = CreateDescriptorResolver();
            descriptorResolver.ProtocolVersion = protocol;

            var errorSink = new ErrorSink();
            var resolvedDescriptors = new List<TagHelperDescriptor>();
            for (var i = 0; i < AssemblyNamesArgument.Values.Count; i++)
            {
                var assemblyName = AssemblyNamesArgument.Values[i];
                var descriptors = descriptorResolver.Resolve(assemblyName, errorSink);
                resolvedDescriptors.AddRange(descriptors);
            }

            ReportResults(resolvedDescriptors, errorSink.Errors);

            return 0;
        }

        protected virtual AssemblyTagHelperDescriptorResolver CreateDescriptorResolver() =>
            new AssemblyTagHelperDescriptorResolver();
    }
}
