// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
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

        public RazorPlugin(IPluginMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public int Protocol { get; set; } = 1;

        public bool ProcessMessage(JObject data, IAssemblyLoadContext assemblyLoadContext)
        {
            var message = data.ToObject<RazorPluginRequestMessage>();

            if (message.MessageType == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatValueMustBeProvidedInMessage(
                        nameof(message.MessageType),
                        nameof(RazorPluginRequestMessage)));
            }

            switch (message.MessageType)
            {
                case RazorPluginMessageTypes.ResolveTagHelperDescriptors:
                    if (message.Data == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatValueMustBeProvidedInMessage(
                                nameof(message.Data),
                                RazorPluginMessageTypes.ResolveTagHelperDescriptors));
                    }

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
                    var errorSink = new ErrorSink();
                    var tagHelperTypeResolver = new AssemblyLoadContextTagHelperTypeResolver(assemblyLoadContext);
                    var tagHelperTypes = tagHelperTypeResolver.Resolve(
                        assemblyName,
                        messageData.SourceLocation,
                        errorSink);
                    var tagHelperDescriptors = tagHelperTypes.SelectMany(
                        type => TagHelperDescriptorFactory.CreateDescriptors(assemblyName, type, errorSink));

                    var responseMessage = new ResolveTagHelperDescriptorsMessage(
                        new ResolveTagHelperDescriptorsResponseData
                        {
                            AssemblyName = assemblyName,
                            Descriptors = tagHelperDescriptors,
                            Errors = errorSink.Errors
                        });

                    _messageBroker.SendMessage(responseMessage);
                    break;
                default:
                    // Unknown message.
                    return false;
            }

            return true;
        }
    }
}
