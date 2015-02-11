// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Tooling.Razor.Models.IncomingMessages
{
    public class ResolveTagHelperDescriptorsRequestData
    {
        public string AssemblyName { get; set; }
        public SourceLocation SourceLocation { get; set; } = SourceLocation.Undefined;
    }
}