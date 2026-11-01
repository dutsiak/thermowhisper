using System.Xml.Linq;

namespace ThermoWhisper.Application.Tests;

public class TextKeysResourcesTests
{
    private const string EnglishResx = "ThermoWhisper/Localization/AppResources.resx";

    public static TheoryData<string> ResxFiles => new() { EnglishResx };

    [Theory]
    [MemberData(nameof(ResxFiles))]
    public void Every_text_key_has_a_non_empty_value(string relativePath)
    {
        var path = Path.Combine(FindSolutionFolder(), relativePath);
        Assert.True(File.Exists(path), $"Resource file not found: {path}");

        var values = XDocument.Load(path).Descendants("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value"));

        foreach (var key in TextKeyValues())
        {
            Assert.True(values.TryGetValue(key, out var value), $"{relativePath}: missing key '{key}'");
            Assert.False(string.IsNullOrWhiteSpace(value), $"{relativePath}: empty value for key '{key}'");
        }
    }

    private static IEnumerable<string> TextKeyValues() =>
        typeof(TextKeys).GetFields().Where(field => field.IsLiteral).Select(field => (string)field.GetRawConstantValue()!);

    private static string FindSolutionFolder()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "ThermoWhisper.slnx")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("ThermoWhisper.slnx not found above " + AppContext.BaseDirectory);
    }
}
