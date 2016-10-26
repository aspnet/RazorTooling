// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class AssemblyTagHelperDescriptorResolver
    {
        private const string TypeFullName = "Microsoft.AspNetCore.Mvc.DesignTimeMvcServiceCollectionProvider";
        private const string MvcAssemblyName = "Microsoft.AspNetCore.Mvc";
        private const string MethodName = "PopulateServiceCollection";
        private const string ViewComponentNameKey = "ViewComponentName";
        private const string PropertyBagPropertyName = "PropertyBag";

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

            // Temporary workaround to make design time for ViewComponent tag helpers work without needing a VS update.
            // This will be removed in a future version.
            var vcthDescriptors = descriptors.Where(d => IsViewComponentTagHelperDescriptor(d));
            var fakeVcthDescriptors = GetFakeDescriptors(vcthDescriptors);

            var finalDescriptors = descriptors.Except(vcthDescriptors).Union(fakeVcthDescriptors);

            return finalDescriptors;
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

        private IEnumerable<TagHelperDescriptor> GetFakeDescriptors(IEnumerable<TagHelperDescriptor> vcthDescriptors)
        {
            /*
             * Since there are no corresponding tag helper types represented by vcthDescriptors,
             * We create fake descriptors with the same attributes and their types using the types 
             * ViewComponentTagHelperDesignTimeType and ViewComponentTagHelperDesignTimeHelperType which mimic the
             * same design time experience as the original tag helper descriptors.
             */ 
            var fakeDescriptors = new List<TagHelperDescriptor>();
            foreach (var descriptor in vcthDescriptors)
            {
                var fakeType = typeof(ViewComponentTagHelperDesignTimeType).FullName;
                var fakeDescriptor = GetFakeDescriptorForTypeName(descriptor, fakeType);

                var fakeAttributes = new List<TagHelperAttributeDescriptor>();
                fakeDescriptor.Attributes = fakeAttributes;

                var fakeHelperType = typeof(ViewComponentTagHelperDesignTimeHelperType).FullName;
                var fakeHelperDescriptor = GetFakeDescriptorForTypeName(descriptor, fakeHelperType);

                var fakeHelperAttributes = new List<TagHelperAttributeDescriptor>();
                fakeHelperDescriptor.Attributes = fakeHelperAttributes;

                foreach (var attribute in descriptor.Attributes)
                {
                    /*
                     * We use TagHelperAttributeDescriptor.PropertyName to inject code into the generated code for the cshtml.
                     * For example consider FooProperty of type string in ExampleTagHelper and ExampleTagHelper2,
                     * 
                     * __SomeNamespace_ExampleTagHelper.FooProperty = "bar";
                     * __SomeNamespace_ExampleTagHelper2.FooProperty = __SomeNamespace_ExampleTagHelper.FooProperty;
                     * 
                     * "FooProperty" in the above code will be replaced with the specified value as below,
                     * 
                     * __Microsoft_AspNetCore_Razor_Design_Internal_ViewComponentTagHelperDesignTimeType.PlaceholderProperty = null;
                     * Microsoft.AspNetCore.Razor.Design.Internal.ViewComponentTagHelperDesignTimeType.ActionProperty = () => {
                     *     System.String __obj = default(System.String);
                     *     Microsoft.AspNetCore.Razor.Design.Internal.ViewComponentTagHelperDesignTimeType.PlaceholderMethod(__obj); ## This handles "__obj is assigned but never used" warning. 
                     *     __obj = "bar";
                     *     __Microsoft_AspNetCore_Razor_Design_Internal_ViewComponentTagHelperDesignTimeHelperType.PlaceholderProperty = null;
                     * };// ## The comment(//) here is needed to ignore the code that replaces "ExampleTagHelper.FooProperty".
                     */
                    const string fakeVariableName = "__obj";
                    var summary = $"{attribute.TypeName}: {descriptor.TypeName}.{attribute.PropertyName}";
                    var fakeAttributePropertyName = $"{nameof(ViewComponentTagHelperDesignTimeType.PlaceholderProperty)} = null; " +
                            $"{fakeType}.{nameof(ViewComponentTagHelperDesignTimeType.ActionProperty)} = () => {{ " +
                            $"{attribute.TypeName} {fakeVariableName} = default({attribute.TypeName}); " +
                            $"{fakeType}.{nameof(ViewComponentTagHelperDesignTimeType.PlaceholderMethod)}({fakeVariableName}); {fakeVariableName} ";

                    var fakeAttribute = new TagHelperAttributeDescriptor()
                    {
                        IsIndexer = attribute.IsIndexer,
                        IsEnum = attribute.IsEnum,
                        IsStringProperty = attribute.IsStringProperty,
                        Name = attribute.Name,
                        PropertyName = fakeAttributePropertyName,
                        TypeName = attribute.TypeName,
                        DesignTimeDescriptor = new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = attribute.DesignTimeDescriptor?.Summary ?? summary
                        },
                    };
                    fakeAttributes.Add(fakeAttribute);

                    var fakeHelperAttributePropertyName =
                        $"{nameof(ViewComponentTagHelperDesignTimeType.PlaceholderProperty)} = null; }};//";

                    var fakeHelperAttribute = new TagHelperAttributeDescriptor()
                    {
                        IsIndexer = attribute.IsIndexer,
                        IsEnum = attribute.IsEnum,
                        IsStringProperty = attribute.IsStringProperty,
                        Name = attribute.Name,
                        PropertyName = fakeHelperAttributePropertyName,
                        TypeName = attribute.TypeName,
                    };
                    fakeHelperAttributes.Add(fakeHelperAttribute);
                }

                fakeDescriptors.Add(fakeDescriptor);
                fakeDescriptors.Add(fakeHelperDescriptor);
            }

            return fakeDescriptors;
        }

        private static TagHelperDescriptor GetFakeDescriptorForTypeName(TagHelperDescriptor descriptor, string typeName)
        {
            var fakeDescriptor = new TagHelperDescriptor()
            {
                Prefix = descriptor.Prefix,
                TagName = descriptor.TagName,
                TypeName = typeName,
                AssemblyName = descriptor.AssemblyName,
                Attributes = descriptor.Attributes,
                RequiredAttributes = descriptor.RequiredAttributes,
                AllowedChildren = descriptor.AllowedChildren,
                RequiredParent = descriptor.RequiredParent,
                TagStructure = descriptor.TagStructure,
                DesignTimeDescriptor = descriptor.DesignTimeDescriptor
            };

            return fakeDescriptor;
        }

        private static bool IsViewComponentTagHelperDescriptor(TagHelperDescriptor descriptor)
        {
            var propertyBag = descriptor.GetType().GetProperty(PropertyBagPropertyName)?.GetValue(descriptor)
                as IDictionary<string, string>;

            if (propertyBag != null)
            {
                return propertyBag.ContainsKey(ViewComponentNameKey);
            }

            return false;
        }
    }

    public abstract class ViewComponentTagHelperDesignTimeType : TagHelper
    {
        public object PlaceholderProperty { get; set; }

        public static Action ActionProperty { get; set; }

        public static void PlaceholderMethod(object obj)
        {
        }
    }

    public abstract class ViewComponentTagHelperDesignTimeHelperType : ViewComponentTagHelperDesignTimeType
    {
    }
}
