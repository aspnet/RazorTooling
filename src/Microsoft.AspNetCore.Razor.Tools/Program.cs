// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Design;
#if DEBUG
using Microsoft.AspNetCore.Razor.Design.Internal;
#endif
using Microsoft.AspNetCore.Razor.Tools.Internal;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class Program
    {
        public static int Main(string[] args)
        {
#if DEBUG
            DebugHelper.HandleDebugSwitch(ref args);
#endif
            var app = new RazorToolingApplication(typeof(Program));

            ResolveTagHelpersCommandBase.Register<ResolveTagHelpersDispatchCommand>(app);

            return app.Execute(args);
        }
    }
}