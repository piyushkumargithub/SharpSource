using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpSource.Utilities;

public static class TestHelpers
{
    private static readonly string[] MethodAttributes = { "Test", "TestMethod", "Fact", "Theory" };

    public static bool HasTestAttribute(this MethodDeclarationSyntax method)
    {
        var attributes = method.AttributeLists.FirstOrDefault()?.Attributes;

        if (attributes == null)
        {
            return false;
        }

        foreach (var attribute in attributes.Value)
        {
            var attributeName = attribute.Name.ToString();
            if (MethodAttributes.Contains(attributeName))
            {
                return true;
            }
        }

        return false;
    }
}