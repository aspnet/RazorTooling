// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Tooling.Razor.Models
{
    public abstract class RazorPluginMessage<T>
    {
        public RazorPluginMessage(string messageType, T data)
        {
            MessageType = messageType;
            Data = data;
        }

        public string MessageType { get; }
        public T Data { get; set; }
    }
}