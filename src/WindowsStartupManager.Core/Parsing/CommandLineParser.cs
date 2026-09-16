using System.Text;

namespace WindowsStartupManager.Core.Parsing;

/// <summary>
/// Parses Windows startup-entry command strings: splitting into arguments using the
/// same quoting rules as CommandLineToArgvW, expanding environment variables, and
/// locating the executable path even when it is unquoted and contains spaces.
/// </summary>
public static class CommandLineParser
{
    public static string ExpandEnvironmentVariables(string input) => Environment.ExpandEnvironmentVariables(input);

    public static IReadOnlyList<string> SplitArguments(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
        {
            return Array.Empty<string>();
        }

        var args = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var backslashCount = 0;

        for (var i = 0; i <= commandLine.Length; i++)
        {
            var c = i < commandLine.Length ? commandLine[i] : '\0';

            if (c == '\\')
            {
                backslashCount++;
                continue;
            }

            if (c == '"')
            {
                current.Append('\\', backslashCount / 2);
                if (backslashCount % 2 == 0)
                {
                    inQuotes = !inQuotes;
                }
                else
                {
                    current.Append('"');
                }
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                current.Append('\\', backslashCount);
                backslashCount = 0;
            }

            if (c == '\0')
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                }
                break;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        return args;
    }

    /// <summary>
    /// Resolves the executable path from a raw startup command. Handles quoted paths
    /// directly; for unquoted paths that contain spaces (common in legacy Run entries)
    /// it probes progressively longer prefixes against <paramref name="fileExists"/>.
    /// </summary>
    public static string? ExtractExecutablePath(string commandLine, Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return null;
        }

        var expanded = ExpandEnvironmentVariables(commandLine).Trim();

        if (expanded.StartsWith('"'))
        {
            var closing = expanded.IndexOf('"', 1);
            if (closing > 1)
            {
                return expanded.Substring(1, closing - 1);
            }
        }

        var tokens = expanded.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }

        var candidate = new StringBuilder();
        for (var i = 0; i < tokens.Length; i++)
        {
            if (candidate.Length > 0)
            {
                candidate.Append(' ');
            }
            candidate.Append(tokens[i]);
            var probe = candidate.ToString();
            if (LooksLikeExecutable(probe) && fileExists(probe))
            {
                return probe;
            }
        }

        return tokens[0];
    }

    private static bool LooksLikeExecutable(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".com", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".dll", StringComparison.OrdinalIgnoreCase);
    }
}