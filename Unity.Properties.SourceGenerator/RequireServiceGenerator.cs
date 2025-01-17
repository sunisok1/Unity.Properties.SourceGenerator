using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Unity.Properties.SourceGenerator
{
    [Generator]
    public class RequireServiceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var requireServiceAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(RequireServiceAttribute).FullName);

            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                var classDeclarationSyntaxes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();


                foreach (var classDeclaration in classDeclarationSyntaxes)
                {
                    var attributes = classDeclaration.AttributeLists
                        .SelectMany(list => list.Attributes)
                        .Where(attribute =>
                            model.GetSymbolInfo(attribute).Symbol is IMethodSymbol methodSymbol &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, requireServiceAttributeSymbol)
                        ).ToList();

                    if (attributes.Count == 0)
                    {
                        continue;
                    }

                    var sourceCode = GeneratePartialCode(classDeclaration, attributes, model);
                    context.AddSource($"{classDeclaration.Identifier.Text}.RequireService.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
        }

        private string GeneratePartialCode(ClassDeclarationSyntax classDeclaration, IEnumerable<AttributeSyntax> attributes, SemanticModel semanticModel)
        {
            var sourceBuilder = new StringBuilder();

            var usings = new HashSet<string>();
            var properties = new List<string>();
            usings.Add("using Framework.Yggdrasil;");
            foreach (var attribute in attributes)
            {
                var serviceTypeArgument = attribute.ArgumentList.Arguments.First();
                if (serviceTypeArgument.Expression is not TypeOfExpressionSyntax typeofExpression) throw new Exception();
                var serviceTypeSymbol = semanticModel.GetTypeInfo(typeofExpression.Type).Type;
                var displayString = serviceTypeSymbol.ToDisplayString();
                var index = displayString.LastIndexOf('.');
                if (displayString[index + 1] is not 'I') throw new Exception("display string must start with 'I'");
                usings.Add($"using {displayString.Substring(0, index)};");
                properties.Add($"        public {displayString.Substring(index + 1)} {displayString.Substring(index + 2)} {{ get; set; }} = Injector.Instance.GetService<{displayString.Substring(index + 1)}>();");
            }

            foreach (var @using in usings)
            {
                sourceBuilder.AppendLine(@using);
            }

            var namespaceName = semanticModel.GetDeclaredSymbol(classDeclaration)?.ContainingNamespace?.ToDisplayString();

            // 如果类在命名空间中，添加命名空间声明
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sourceBuilder.AppendLine($"namespace {namespaceName}");
            }

            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public partial class {classDeclaration.Identifier.Text}");
            sourceBuilder.AppendLine("    {");
            foreach (var property in properties)
            {
                sourceBuilder.AppendLine(property);
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            return sourceBuilder.ToString();
        }
    }
}