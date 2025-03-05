using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Unmanaged.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontAllowDefaultInitializationDisposeStructs : DiagnosticAnalyzer
    {
        private const string ID = "U0001";
        private const string Title = "Disposable instance must be initialized";
        private const string Format = "An instance of a disposable type `{0}` cannot be initialized with a default value";
        private const string Category = "Types";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Error;

        private static readonly DiagnosticDescriptor rule;

        static DontAllowDefaultInitializationDisposeStructs()
        {
            rule = new(ID, Title, Format, Category, Severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

        public override void Initialize(AnalysisContext context)
        {
            SyntaxKind syntaxKinds = SyntaxKind.VariableDeclaration;
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, syntaxKinds);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is VariableDeclarationSyntax variableDeclaration)
            {
                //check if the type being declared is disposable
                TypeSyntax type = variableDeclaration.Type;
                ITypeSymbol? typeSymbol = context.SemanticModel.GetTypeInfo(type).Type;
                if (typeSymbol is null)
                {
                    return;
                }

                if (!typeSymbol.IsValueType)
                {
                    return;
                }

                if (!typeSymbol.HasInterface<IDisposable>())
                {
                    return;
                }

                Diagnostic? diagnostic = GetDiagnostic(type, typeSymbol, variableDeclaration);
                if (diagnostic is not null)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static Diagnostic? GetDiagnostic(TypeSyntax type, ITypeSymbol typeSymbol, SyntaxNode node)
        {
            bool assignedToDefault = false;
            foreach (SyntaxNode child in node.ChildNodes())
            {
                if (child is VariableDeclaratorSyntax variableDeclarator)
                {
                    foreach (SyntaxNode grandChild in child.ChildNodes())
                    {
                        if (grandChild is EqualsValueClauseSyntax equalsValueClause)
                        {
                            foreach (SyntaxNode greatGrandChild in grandChild.ChildNodes())
                            {
                                if (greatGrandChild is LiteralExpressionSyntax literalExpression)
                                {
                                    if (literalExpression.Token.Text == "default")
                                    {
                                        assignedToDefault = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (assignedToDefault)
            {
                Location location = node.GetLocation();
                string typeName = type.ToString();
                return Diagnostic.Create(rule, location, typeName);
            }
            else
            {
                return null;
            }
        }
    }
}