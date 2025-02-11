using System;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    // See: https://docs.microsoft.com/en-us/dotnet/desktop/xaml-services/white-space-processing
    // Must be applied after content has been transformed to a XamlAstXamlPropertyValueNode,
    // and after ResolvePropertyValueAddersTransformer has resolved the Add methods for collection properties
#if !XAMLX_INTERNAL
    public
#endif
    class ApplyWhitespaceNormalization : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode propertyNode)
            {
                var childNodes = propertyNode.Values;
                WhitespaceNormalization.Apply(
                    childNodes,
                    context.Configuration
                );

                var property = propertyNode.Property.GetClrProperty();
                if (!WantsWhitespaceOnlyElements(context.Configuration, property))
                {
                    WhitespaceNormalization.RemoveWhitespaceNodes(childNodes);
                }
            }

            return node;
        }

        private bool WantsWhitespaceOnlyElements(TransformerConfiguration config,
            XamlAstClrProperty property)
        {
            var wellKnownTypes = config.WellKnownTypes;

            var acceptsMultipleElements = false;
            foreach (var setter in property.Setters)
            {
                // Skip any dictionary-like setters
                if (setter.Parameters.Count != 1)
                {
                    continue;
                }

                var parameterType = setter.Parameters[0];
                if (!setter.BinderParameters.AllowMultiple)
                {
                    // If the property can accept a scalar string, it'll get whitespace nodes by default
                    if (parameterType.Equals(wellKnownTypes.String) || parameterType.Equals(wellKnownTypes.Object))
                    {
                        return true;
                    }
                }
                else
                {
                    acceptsMultipleElements = true;
                }
            }

            // A collection-like property will only receive whitespace-only nodes if the
            // property type can be deduced, and that type is annotated as whitespace significant
            if (acceptsMultipleElements
                && property.Getter != null
                && config.IsWhitespaceSignificantCollection(property.Getter.ReturnType))
            {
                return true;
            }

            return false;
        }

    }
}