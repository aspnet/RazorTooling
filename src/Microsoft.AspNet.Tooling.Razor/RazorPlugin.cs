// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Tooling.Razor.Models.IncomingMessages;
using Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages;
using Microsoft.Framework.DesignTimeHost;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class RazorPlugin : IPlugin
    {
        private readonly IPluginMessageBroker _messageBroker;
        private readonly AssemblyLoadContextTagHelperTypeResolver _tagHelperTypeResolver;

        public RazorPlugin(IPluginMessageBroker messageBroker, IAssemblyLoadContext assemblyLoadContext)
        {
            _messageBroker = messageBroker;
            _tagHelperTypeResolver = new AssemblyLoadContextTagHelperTypeResolver(assemblyLoadContext);
        }

        public void ProcessMessage(JObject data)
        {
            var message = data.ToObject<RazorPluginRequestMessage>();

            if (message.MessageType == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatValueMustBeProvidedInMessage(
                        nameof(message.MessageType),
                        nameof(RazorPluginRequestMessage)));
            }

            if (message.Data == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatValueMustBeProvidedInMessage(
                        nameof(message.Data),
                        nameof(RazorPluginRequestMessage)));
            }

            switch (message.MessageType)
            {
                case RazorPluginMessageTypes.ResolveTagHelperDescriptors:
                    var messageData = message.Data.ToObject<ResolveTagHelperDescriptorsRequestData>();

                    if (messageData.AssemblyName == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatValueMustBeProvidedInMessage(
                                nameof(messageData.AssemblyName),
                                RazorPluginMessageTypes.ResolveTagHelperDescriptors));
                    }
                    else if (messageData.SourceLocation == SourceLocation.Undefined)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatValueMustBeProvidedInMessage(
                                nameof(messageData.SourceLocation),
                                RazorPluginMessageTypes.ResolveTagHelperDescriptors));
                    }

                    var assemblyName = messageData.AssemblyName;
                    var errorSink = new ParserErrorSink();
                    var tagHelperTypes = _tagHelperTypeResolver.Resolve(
                        assemblyName,
                        messageData.SourceLocation,
                        errorSink);
                    var tagHelperDescriptors = tagHelperTypes.SelectMany(TagHelperDescriptorFactory.CreateDescriptors);

                    var responseMessage = new ResolveTagHelperDescriptorsMessage(
                        new ResolveTagHelperDescriptorsResponseData
                        {
                            AssemblyName = assemblyName,
                            Descriptors = tagHelperDescriptors,
                            Errors = errorSink.Errors
                        });

                    _messageBroker.SendMessage(responseMessage);
                    break;
            }
        }
    }
}
