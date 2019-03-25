using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ImmutableByConvention
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ImmutableByConventionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IBC1000";

        // TODO: Consider just using plain old strings...
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NoPropertyMutationTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.NoPropertyMutationMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NoPropertyMutationDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Mutation";

        private static DiagnosticDescriptor NoPropertyMutationRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(NoPropertyMutationRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.SimpleAssignment);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var firstChild = context.Operation.Children.FirstOrDefault() as IPropertyReferenceOperation;

            if (firstChild == null) return;
            if (!(firstChild.Syntax is MemberAccessExpressionSyntax)) return;

            var property = firstChild.Property;
            var containingTypeAttributes = property.ContainingType.GetAttributes();

            if (containingTypeAttributes.Any(a =>
                a.AttributeClass.Name == "ImmutableByConventionAttribute" ||
                a.AttributeClass.Name == "ImmutableByConventionPlaceholderAttribute"))
            {
                var propertyName = property.Name;
                var location = context.Operation.Syntax.GetLocation();
                var diagnostic = Diagnostic.Create(NoPropertyMutationRule, location, propertyName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
