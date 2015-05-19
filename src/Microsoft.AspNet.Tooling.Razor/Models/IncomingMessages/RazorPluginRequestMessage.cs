// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Tooling.Razor.Models.IncomingMessages
{
    public class RazorPluginRequestMessage : RazorPluginMessage<JObject>
    {
        public RazorPluginRequestMessage(string messageType, JObject data)
            : base(messageType, data)
        {
        }
    }
}