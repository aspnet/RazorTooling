// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DesignTimeHost;

namespace Microsoft.AspNet.Tooling.Razor.Tests
{
    public class TestPluginMessageBroker : IPluginMessageBroker
    {
        private readonly Action<object> _onSendMessage;

        public TestPluginMessageBroker()
            : this((_) => { })
        {
        }

        public TestPluginMessageBroker(Action<object> onSendMessage)
        {
            _onSendMessage = onSendMessage;
        }

        public void SendMessage(object data)
        {
            _onSendMessage(data);
        }
    }
}