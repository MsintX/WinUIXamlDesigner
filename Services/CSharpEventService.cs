using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using WinUIXamlDesigner.Models;

namespace WinUIXamlDesigner.Services;

public sealed class CSharpEventService
{
    public string UpdateEventHandler(string source, ControlNode node, string? oldEventName, string? newEventName, bool removeOldAutoMethod)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        if (string.IsNullOrWhiteSpace(newEventName))
        {
            if (removeOldAutoMethod && !string.IsNullOrWhiteSpace(oldEventName))
                root = RemoveGeneratedMethod(root, oldEventName!);
            return Formatter.Format(root, new AdhocWorkspace()).ToFullString();
        }

        if (!string.IsNullOrWhiteSpace(oldEventName) && !string.Equals(oldEventName, newEventName, StringComparison.Ordinal))
            root = RenameSymbol(root, oldEventName!, newEventName!);

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == newEventName);

        if (method is null)
        {
            var argsType = node.SupportsClick ? "RoutedEventArgs" : "TappedRoutedEventArgs";
            var generated = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    newEventName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender")).WithType(SyntaxFactory.ParseTypeName("object")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")).WithType(SyntaxFactory.ParseTypeName(argsType))
                })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("// TODO\n")));

            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Modifiers.Any(SyntaxKind.PartialKeyword))
                ?? root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode is null)
                throw new InvalidOperationException("没有找到 code-behind class。");
            root = root.ReplaceNode(classNode, classNode.AddMembers(generated));
        }

        return Formatter.Format(root, new AdhocWorkspace()).ToFullString();
    }

    public string EnsureLoadedHandler(string source, string rootElementName = "ContentRoot", string methodName = "Window_Loaded")
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Modifiers.Any(SyntaxKind.PartialKeyword))
            ?? root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classNode is null) throw new InvalidOperationException("没有找到 code-behind class。");

        var existing = classNode.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.Identifier.ValueText == methodName);
        if (!existing)
        {
            var method = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender")).WithType(SyntaxFactory.ParseTypeName("object")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")).WithType(SyntaxFactory.ParseTypeName("RoutedEventArgs"))
                })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("// TODO\n")));
            root = root.ReplaceNode(classNode, classNode.AddMembers(method));
        }

        classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == classNode.Identifier.ValueText) ?? classNode;
        var ctor = classNode.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
        if (ctor is null) throw new InvalidOperationException("没有找到页面构造函数。");

        var statement = $"{rootElementName}.Loaded += {methodName};";
        var hasSubscription = ctor.Body?.Statements.Any(s => s.ToString().Contains(statement, StringComparison.Ordinal)) == true;
        if (!hasSubscription)
        {
            var initIndex = ctor.Body?.Statements
                .Select((s, i) => (s, i))
                .FirstOrDefault(x => x.s.ToString().Contains("InitializeComponent", StringComparison.Ordinal)).i ?? -1;
            var newStatement = SyntaxFactory.ParseStatement(statement);
            var newBody = initIndex >= 0
                ? ctor.Body!.WithStatements(ctor.Body.Statements.Insert(initIndex + 1, newStatement))
                : ctor.Body!.AddStatements(newStatement);
            root = root.ReplaceNode(ctor, ctor.WithBody(newBody));
        }
        return Formatter.Format(root, new AdhocWorkspace()).ToFullString();
    }

    private static SyntaxNode RenameSymbol(SyntaxNode root, string oldName, string newName)
    {
        return root.ReplaceTokens(
            root.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == oldName),
            (token, _) => SyntaxFactory.Identifier(token.LeadingTrivia, newName, token.TrailingTrivia));
    }

    private static SyntaxNode RemoveGeneratedMethod(SyntaxNode root, string eventName)
    {
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == eventName &&
                                 m.Modifiers.Any(SyntaxKind.PrivateKeyword) &&
                                 m.Body?.Statements.Any(s => s.ToString().Contains("// TODO", StringComparison.Ordinal)) == true);
        return method is null ? root : root.RemoveNode(method, SyntaxRemoveOptions.KeepExteriorTrivia) ?? root;
    }
}
