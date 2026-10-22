#:package Microsoft.CodeAnalysis.CSharp@5.9.0
#:property ManagePackageVersionsCentrally=false
// Checks the method length rule of CODE_STYLE: at most N lines (default 50) in a method body.
// Counted: lines of the body between { and }, or of the expression after =>, blank lines included.
// Not counted: lines with only a comment, preprocessor lines (#region...).
// Checked: methods, constructors, destructors, operators, accessors, local functions,
// expression-bodied properties and indexers. Lambdas and local functions count in their method too.
// Skipped: bin/, obj/, Migrations/, .git/, node_modules/, *.g.cs, *.g.i.cs, *.Designer.cs.
// Usage: dotnet run --file scripts/ci/method-length.cs -- [folder] [--max 50]
// Exit code: 0 - all good, 1 - too long methods found, 2 - wrong arguments.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

return MethodLength.Run(args);

static class MethodLength
{
    private static readonly string[] SkippedFolders = ["bin", "obj", "Migrations", ".git", "node_modules"];
    private static readonly string[] SkippedEndings = [".g.cs", ".g.i.cs", ".designer.cs"];

    public static int Run(string[] args)
    {
        if (!TryParseArgs(args, out var root, out var max))
        {
            Console.Error.WriteLine("Usage: dotnet run --file scripts/ci/method-length.cs -- [folder] [--max 50]");
            return 2;
        }

        var problems = new List<string>();
        foreach (var file in FindFiles(root))
        {
            problems.AddRange(CheckFile(root, file, max));
        }

        foreach (var problem in problems)
        {
            Console.WriteLine(problem);
        }

        Console.WriteLine(problems.Count == 0
            ? $"method-length: OK (max {max})"
            : $"method-length: {problems.Count} method(s) longer than {max} lines");
        return problems.Count == 0 ? 0 : 1;
    }

    private static bool TryParseArgs(string[] args, out string root, out int max)
    {
        root = ".";
        max = 50;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--max")
            {
                if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out max) || max < 1)
                {
                    return false;
                }

                i++;
            }
            else
            {
                root = args[i];
            }
        }

        return Directory.Exists(root);
    }

    private static IEnumerable<string> FindFiles(string root)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
        return Directory.EnumerateFiles(root, "*.cs", options)
            .Where(file => !IsSkipped(Path.GetRelativePath(root, file)))
            .Order(StringComparer.Ordinal);
    }

    private static bool IsSkipped(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var inSkippedFolder = parts[..^1].Any(part => SkippedFolders.Contains(part, StringComparer.OrdinalIgnoreCase));
        var generated = SkippedEndings.Any(ending => relativePath.EndsWith(ending, StringComparison.OrdinalIgnoreCase));
        return inSkippedFolder || generated;
    }

    private static IEnumerable<string> CheckFile(string root, string file, int max)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
        var syntaxRoot = tree.GetRoot();
        var text = tree.GetText();
        var codeLines = LinesWithCode(syntaxRoot, text);
        var commentLines = LinesWithComments(syntaxRoot, text);

        foreach (var (node, name, lines) in Members(syntaxRoot, text))
        {
            var count = CountLines(text, lines, codeLines, commentLines);
            if (count > max)
            {
                var line = text.Lines.GetLineFromPosition(node.SpanStart).LineNumber + 1;
                yield return $"{Path.GetRelativePath(root, file)}:{line}: '{name}' has {count} lines (max {max})";
            }
        }
    }

    private static IEnumerable<(SyntaxNode Node, string Name, Range Lines)> Members(SyntaxNode syntaxRoot, SourceText text)
    {
        foreach (var node in syntaxRoot.DescendantNodes())
        {
            var member = node switch
            {
                BaseMethodDeclarationSyntax method => (Name: MethodName(method), method.Body, method.ExpressionBody),
                AccessorDeclarationSyntax accessor => (Name: AccessorName(accessor), accessor.Body, accessor.ExpressionBody),
                LocalFunctionStatementSyntax local => (Name: local.Identifier.Text, local.Body, local.ExpressionBody),
                PropertyDeclarationSyntax property => (Name: property.Identifier.Text, Body: null, property.ExpressionBody),
                IndexerDeclarationSyntax indexer => (Name: "this[]", Body: null, indexer.ExpressionBody),
                _ => (Name: (string?)null, Body: null, ExpressionBody: null),
            };

            if (member.Name is null)
            {
                continue;
            }

            if (member.Body is { } block)
            {
                yield return (node, member.Name, BlockInside(block, text));
            }
            else if (member.ExpressionBody is { } arrow)
            {
                yield return (node, member.Name, LinesOf(arrow.Expression.Span, text));
            }
        }
    }

    // Lines between the { line and the } line; a body written on one line counts as that line.
    private static Range BlockInside(BlockSyntax block, SourceText text)
    {
        var open = text.Lines.GetLineFromPosition(block.OpenBraceToken.SpanStart).LineNumber;
        var close = text.Lines.GetLineFromPosition(block.CloseBraceToken.SpanStart).LineNumber;
        return close == open ? open..open : (open + 1)..(close - 1);
    }

    private static Range LinesOf(TextSpan span, SourceText text)
    {
        var first = text.Lines.GetLineFromPosition(span.Start).LineNumber;
        var last = text.Lines.GetLineFromPosition(Math.Max(span.Start, span.End - 1)).LineNumber;
        return first..last;
    }

    private static string MethodName(BaseMethodDeclarationSyntax method) => method switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax c => c.Identifier.Text,
        DestructorDeclarationSyntax d => "~" + d.Identifier.Text,
        OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text,
        ConversionOperatorDeclarationSyntax c => "operator " + c.Type,
        _ => method.Kind().ToString(),
    };

    private static string AccessorName(AccessorDeclarationSyntax accessor)
    {
        var owner = accessor.Parent?.Parent switch
        {
            PropertyDeclarationSyntax p => p.Identifier.Text,
            EventDeclarationSyntax e => e.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            _ => "?",
        };
        return $"{owner}.{accessor.Keyword.Text}";
    }

    private static HashSet<int> LinesWithCode(SyntaxNode syntaxRoot, SourceText text)
    {
        var lines = new HashSet<int>();
        foreach (var token in syntaxRoot.DescendantTokens())
        {
            if (token.Span.IsEmpty)
            {
                continue;
            }

            AddLines(lines, text, token.Span);
        }

        return lines;
    }

    private static HashSet<int> LinesWithComments(SyntaxNode syntaxRoot, SourceText text)
    {
        var lines = new HashSet<int>();
        foreach (var trivia in syntaxRoot.DescendantTrivia(descendIntoTrivia: true))
        {
            var isComment = trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.DisabledTextTrivia);
            if (isComment)
            {
                AddLines(lines, text, trivia.Span);
            }
        }

        return lines;
    }

    private static void AddLines(HashSet<int> lines, SourceText text, TextSpan span)
    {
        var range = LinesOf(span, text);
        for (var line = range.Start.Value; line <= range.End.Value; line++)
        {
            lines.Add(line);
        }
    }

    private static int CountLines(SourceText text, Range lines, HashSet<int> codeLines, HashSet<int> commentLines)
    {
        var count = 0;
        for (var line = lines.Start.Value; line <= lines.End.Value; line++)
        {
            var blank = string.IsNullOrWhiteSpace(text.Lines[line].ToString());
            if (codeLines.Contains(line) || (blank && !commentLines.Contains(line)))
            {
                count++;
            }
        }

        return count;
    }
}
