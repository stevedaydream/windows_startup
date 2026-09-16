using WindowsStartupManager.Core.Parsing;
using Xunit;

namespace WindowsStartupManager.Core.Tests;

public class CommandLineParserTests
{
    [Fact]
    public void SplitArguments_SplitsUnquotedTokensOnWhitespace()
    {
        var result = CommandLineParser.SplitArguments(@"C:\App\app.exe --flag value");

        Assert.Equal(new[] { @"C:\App\app.exe", "--flag", "value" }, result);
    }

    [Fact]
    public void SplitArguments_KeepsQuotedSegmentTogether()
    {
        var result = CommandLineParser.SplitArguments("\"C:\\Program Files\\App\\app.exe\" --flag");

        Assert.Equal(new[] { @"C:\Program Files\App\app.exe", "--flag" }, result);
    }

    [Fact]
    public void SplitArguments_KeepsEmbeddedQuotedArgumentTogether()
    {
        var result = CommandLineParser.SplitArguments("app.exe --name \"quoted value\"");

        Assert.Equal(new[] { "app.exe", "--name", "quoted value" }, result);
    }

    [Fact]
    public void SplitArguments_HandlesEscapedQuoteInsideQuotedArgument()
    {
        // Raw command line: app.exe --name "quo\"ted"
        var result = CommandLineParser.SplitArguments("app.exe --name \"quo\\\"ted\"");

        Assert.Equal(new[] { "app.exe", "--name", "quo\"ted" }, result);
    }

    [Fact]
    public void SplitArguments_TreatsBackslashesLiterallyWhenNotFollowedByQuote()
    {
        var result = CommandLineParser.SplitArguments(@"C:\Some\Path\app.exe");

        Assert.Equal(new[] { @"C:\Some\Path\app.exe" }, result);
    }

    [Fact]
    public void ExtractExecutablePath_ReturnsQuotedPath()
    {
        var path = CommandLineParser.ExtractExecutablePath("\"C:\\Program Files\\App\\app.exe\" --flag");

        Assert.Equal(@"C:\Program Files\App\app.exe", path);
    }

    [Fact]
    public void ExtractExecutablePath_ProbesUnquotedPathContainingSpaces()
    {
        const string commandLine = @"C:\Program Files\App\app.exe --flag";

        var path = CommandLineParser.ExtractExecutablePath(
            commandLine,
            fileExists: candidate => candidate == @"C:\Program Files\App\app.exe");

        Assert.Equal(@"C:\Program Files\App\app.exe", path);
    }

    [Fact]
    public void ExtractExecutablePath_FallsBackToFirstTokenWhenNoFileMatches()
    {
        const string commandLine = @"C:\App\app.exe --flag";

        var path = CommandLineParser.ExtractExecutablePath(commandLine, fileExists: _ => false);

        Assert.Equal(@"C:\App\app.exe", path);
    }

    [Fact]
    public void ExpandEnvironmentVariables_ExpandsKnownVariable()
    {
        var expanded = CommandLineParser.ExpandEnvironmentVariables("%WINDIR%\\System32");

        Assert.DoesNotContain("%WINDIR%", expanded, StringComparison.OrdinalIgnoreCase);
    }
}