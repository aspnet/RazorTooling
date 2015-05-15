// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages
{
    public class PluginProtocolMessage : RazorPluginMessage<string>
    {
        public PluginProtocolMessage(Version protocol)
            : base(messageType: RazorPluginMessageTypes.PluginProtocol, data: protocol.ToString())
        {
        }
    }
}