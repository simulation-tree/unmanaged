using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Unmanaged.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontAllowSerializableTypesWithReadWriteValue : DiagnosticAnalyzer
    {
        private const string ID = "U0002";
        private const string Title = "Bypassing serialization of the provided type";
        private const string Format = "The given type `{0}` implements ISerializable, and cannot be used here because it would bypass its serialization implementation. Use `{1}` instead.";
        private const string Category = "Types";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static readonly DiagnosticDescriptor rule;

        static DontAllowSerializableTypesWithReadWriteValue()
        {
            rule = new(ID, Title, Format, Category, Severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is InvocationExpressionSyntax invocationExpression)
            {
                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    string methodName = memberAccess.Name.Identifier.Text;
                    bool isReading = methodName == "ReadValue";
                    bool isWriting = methodName == "WriteValue";
                    if (isReading || isWriting)
                    {
                        string suggestedMethod = isReading ? "ReadObject" : "WriteObject";
                        if (memberAccess.Name is GenericNameSyntax genericName)
                        {
                            TypeSyntax typeArgument = genericName.TypeArgumentList.Arguments[0];
                            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(typeArgument, context.CancellationToken);
                            if (typeInfo.Type is ITypeSymbol typeParameter)
                            {
                                if (typeParameter.HasInterface("Unmanaged.ISerializable"))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(rule, invocationExpression.GetLocation(), typeParameter.Name, suggestedMethod));
                                    return;
                                }
                            }
                        }
                        else if (memberAccess.Name is IdentifierNameSyntax)
                        {
                            ArgumentSyntax parameter = invocationExpression.ArgumentList.Arguments[0];
                            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(parameter.Expression, context.CancellationToken);
                            if (typeInfo.Type is ITypeSymbol parameterType && parameterType.HasInterface("Unmanaged.ISerializable"))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(rule, invocationExpression.GetLocation(), parameterType.Name, suggestedMethod));
                                return;
                            }
                            else
                            {
                                //couldnt figure out the type from the parameter
                            }
                        }
                    }
                }
            }
        }
    }
}