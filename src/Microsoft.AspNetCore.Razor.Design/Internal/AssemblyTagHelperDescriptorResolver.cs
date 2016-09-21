// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class AssemblyTagHelperDescriptorResolver
    {
        private const string TypeFullName = "Microsoft.AspNetCore.Mvc.DesignTimeMvcServiceCollectionProvider";
        private const string MvcAssemblyName = "Microsoft.AspNetCore.Mvc";
        private const string MethodName = "PopulateServiceCollection";

        private readonly TagHelperDescriptorFactory _tagHelperDescriptorFactory;
        private readonly TagHelperTypeResolver _tagHelperTypeResolver;

        private bool _isInitialized;
        private Action<IServiceCollection, string> _populateMethodDelegate;

        public AssemblyTagHelperDescriptorResolver()
            : this(new TagHelperTypeResolver())
        {
        }

        public AssemblyTagHelperDescriptorResolver(TagHelperTypeResolver tagHelperTypeResolver)
        {
            _tagHelperTypeResolver = tagHelperTypeResolver;
            _tagHelperDescriptorFactory = new TagHelperDescriptorFactory(designTime: true);
        }

        public static int DefaultProtocolVersion { get; } = 1;

        public int ProtocolVersion { get; set; } = DefaultProtocolVersion;

        public IEnumerable<TagHelperDescriptor> Resolve(string assemblyName, ErrorSink errorSink)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (ProtocolVersion != 1)
            {
                // Unknown protocol
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        DesignResources.InvalidProtocolValue,
                        typeof(TagHelperDescriptor).FullName, ProtocolVersion));
            }

            EnsureMvcInitialized();

            ITagHelperDescriptorResolver resolver;
            if (_populateMethodDelegate != null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton<ITagHelperTypeResolver>(_tagHelperTypeResolver);
                serviceCollection.AddSingleton<ITagHelperDescriptorFactory>(_tagHelperDescriptorFactory);

                // Populate the service collection
                _populateMethodDelegate(serviceCollection, assemblyName);

                var services = serviceCollection.BuildServiceProvider();
                resolver = services.GetRequiredService<ITagHelperDescriptorResolver>();
            }
            else
            {
                // MVC assembly does not exist. Manually create the resolver.
                resolver = new TagHelperDescriptorResolver(_tagHelperTypeResolver, _tagHelperDescriptorFactory);
            }

            var directiveDescriptors = new[]
            {
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = $"*, {assemblyName}"
                }
            };
            var context = new TagHelperDescriptorResolutionContext(directiveDescriptors, errorSink);
            var descriptors = resolver.Resolve(context);

            return descriptors;
        }

        private void EnsureMvcInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            var providerClass = Type.GetType($"{TypeFullName}, {MvcAssemblyName}");

            // Get the method from the type
            var populateMethod = providerClass?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(methodInfo =>
                {
                    if (string.Equals(MethodName, methodInfo.Name, StringComparison.Ordinal))
                    {
                        var methodParams = methodInfo.GetParameters();
                        return methodParams.Length == 2
                            && methodParams[0].ParameterType.Equals(typeof(IServiceCollection))
                            && methodParams[1].ParameterType.Equals(typeof(string));
                    }
                    return false;
                });

            _populateMethodDelegate = (Action<IServiceCollection, string>) populateMethod
                ?.CreateDelegate(typeof(Action<IServiceCollection, string>));
        }
    }
}
