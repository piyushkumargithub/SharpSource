using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using SharpSource.Utilities;

namespace SharpSource.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SwitchDoesNotHandleAllEnumOptionsAnalyzer : DiagnosticAnalyzer
{
    private static readonly string Message = "Missing enum member in switched cases.";
    private static readonly string Title = "Add cases for missing enum member.";

    public static DiagnosticDescriptor Rule
        => new(DiagnosticId.SwitchDoesNotHandleAllEnumOptions, Title, Message, Categories.Correctness, DiagnosticSeverity.Warning, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.SwitchStatement);
    }

    private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
    {
        var switchBlock = (SwitchStatementSyntax)context.Node;

        if (context.SemanticModel.GetTypeInfo(switchBlock.Expression).Type is not INamedTypeSymbol enumType || enumType.TypeKind != TypeKind.Enum)
        {
            return;
        }

        var labelSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var section in switchBlock.Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label.IsKind(SyntaxKind.CaseSwitchLabel))
                {
                    var switchLabel = (CaseSwitchLabelSyntax)label;
                    var symbol = context.SemanticModel.GetSymbolInfo(switchLabel.Value).Symbol;
                    if (symbol == null)
                    {
                        // potentially malformed case statement
                        // or an integer being cast to an enum type
                        return;
                    }

                    labelSymbols.Add(symbol);
                }
            }
        }

        foreach (var member in enumType.GetMembers())
        {
            // skip `.ctor`
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            if (!labelSymbols.Contains(member))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, switchBlock.Expression.GetLocation()));
                return;
            }
        }
    }
}