// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class ResolvedTagHelperDescriptorsResult
    {
        public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

        public IEnumerable<RazorError> Errors { get; set; }
    }
}
