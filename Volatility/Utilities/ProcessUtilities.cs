using System.Diagnostics;
using System.Text;

namespace Volatility.Utilities;

internal static class ProcessUtilities
{
    public static string RunAndCapture(string fileName, string arguments, string? workingDirectory = null)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = startInfo };
        StringBuilder output = new();

        process.Start();
        output.Append(process.StandardOutput.ReadToEnd());
        output.Append(process.StandardError.ReadToEnd());
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
        }

        return output.ToString();
    }
}
