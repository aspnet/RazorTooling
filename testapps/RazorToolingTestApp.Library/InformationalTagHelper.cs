// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorToolingTestApp.Library
{
    public class InformationalTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content.SetContent($"This is information from the {typeof(InformationalTagHelper).FullName}.");
        }
    }
}
