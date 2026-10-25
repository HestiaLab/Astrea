using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Astrea.Tests.Browser;

internal sealed class BrowserAppHost : IAsyncDisposable
{
    private readonly Process _process;

    private BrowserAppHost(Process process, string baseUrl)
    {
        _process = process;
        BaseUrl = baseUrl;
    }

    public static string RepositoryRoot { get; } =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public string BaseUrl { get; }

    public static async Task<BrowserAppHost> StartAsync(
        string projectRelativePath,
        Action<string>? log = null)
    {
        var repositoryRoot = RepositoryRoot;
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var port = GetAvailablePort();
        var baseUrl = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, projectRelativePath));
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseUrl);

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {projectRelativePath}.");
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                log?.Invoke(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                log?.Invoke(args.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await WaitForHttpAsync(baseUrl, process);
        return new BrowserAppHost(process, baseUrl);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }

        _process.Dispose();
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForHttpAsync(string baseUrl, Process process)
    {
        using var client = new HttpClient();
        for (var attempt = 0; attempt < 120; attempt++)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The host process exited with code {process.ExitCode}.");
            }

            try
            {
                using var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Host is still starting.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The host did not become ready within 30 seconds.");
    }
}
