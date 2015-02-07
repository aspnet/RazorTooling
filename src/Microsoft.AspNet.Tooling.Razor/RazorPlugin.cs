// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Tooling.Razor.Models.IncomingMessages;
using Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages;
using Microsoft.Framework.DesignTimeHost;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class RazorPlugin : IPlugin
    {
        private readonly IPluginMessageBroker _messageBroker;
        private readonly TagHelperTypeResolver _tagHelperTypeResolver;

        public RazorPlugin(IPluginMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
            _tagHelperTypeResolver = new TagHelperTypeResolver();
        }

        public void ProcessMessage(JObject data)
        {
            var message = data.ToObject<RazorPluginRequestMessage>();

            switch (message.MessageType)
            {
                case RazorPluginMessageTypes.ResolveTagHelperDescriptors:
                    var assemblyName = message.Data;
                    var tagHelperTypes = _tagHelperTypeResolver.Resolve(
                        assemblyName,
                        // TODO: Need to send error data back to the caller to show to the user.
                        documentLocation: SourceLocation.Zero,
                        errorSink: new ParserErrorSink());
                    var tagHelperDescriptors = tagHelperTypes.SelectMany(TagHelperDescriptorFactory.CreateDescriptors);

                    var responseMessage = new ResolveTagHelperDescriptorsMessage(
                        new ResolveTagHelperDescriptorsMessageData
                        {
                            AssemblyName = assemblyName,
                            Descriptors = tagHelperDescriptors
                        });

                    _messageBroker.SendMessage(responseMessage);
                    break;
            }
        }
    }
}
