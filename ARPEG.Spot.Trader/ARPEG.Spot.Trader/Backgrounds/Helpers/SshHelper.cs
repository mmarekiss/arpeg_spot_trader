using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ARPEG.Spot.Trader.Backgrounds.Helpers;

public class SshHelper
{
    public static IEnumerable<string> ConnectWiFi(
        ILogger logger)
    {
        var client = GetClient();

        if (!WiFiIsConnected(client, logger))
        {
            WifiConnection(client, "rock", logger);
        }

        return FetchAllMyIps(client, logger);
    }

    private static SshClient GetClient()
    {
        var login = "rock";
        var password = "rock";
        
        var serverAddress = "172.17.0.1";

        if (Debugger.IsAttached) serverAddress = "192.168.55.140";

        var client = new SshClient(serverAddress, 22, login, password);
        client.Connect();
        return client;
    }

    private static IEnumerable<string> FetchAllMyIps(SshClient ssh,
        ILogger logger)
    {
        using var cmd = ssh.RunCommand(@$"ifconfig | grep 'inet '| sed -e 's/^ *inet \([0-9.]*\) .*$/\1/g'");
        if (cmd.ExitStatus == 0)
        {
            return cmd.Result.Split('\n').Where(x => IPAddress.TryParse(x, out _));
        }

        return Enumerable.Empty<string>();
    }

    private static void WifiConnection(SshClient ssh,
        string pass,
        ILogger logger)
    {
        IDictionary<TerminalModes, uint> modes =
            new Dictionary<TerminalModes, uint>();

        modes.Add(TerminalModes.ECHO, 53);

        using var shellStream = ssh.CreateShellStream("xterm", 80, 24, 800, 600, 1024, modes);
        shellStream.WriteLine(
            "sudo nmcli -f ssid dev wifi | grep Solar | sed 's/ *$//g' | head -1 | xargs -I % sudo nmcli dev wifi connect % password '12345678'");
        var output = shellStream.Expect(new Regex(@"([$#>:])"));
        logger.LogInformation("Connect To WiFi command {WiFiCommand}", output);
        shellStream.WriteLine(pass);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        output = shellStream.Expect(new Regex(@"[$>]"));
    }
    
    private static bool WiFiIsConnected(SshClient ssh,
        ILogger logger)
    {
        logger.LogInformation("Check WiFi state");
        using var cmd = ssh.RunCommand($"nmcli device | grep Solar");
        if (cmd.ExitStatus == 0)
        {
            logger.LogInformation(cmd.Result);
            return cmd.Result.Contains("Solar") && cmd.Result.Contains("connected");
        }
        else
        {
            logger.LogError(cmd.Error);
            return false;
        }
    }

    public static void SetupGwSnLog(string sn, ILogger logger)
    {
        using var client = GetClient();
        using var cmd = client.RunCommand($"sed -i s/arpeg-1/arpeg-{sn}/g promtailconfig.yml");
        if (cmd.ExitStatus == 0)
            logger.LogInformation(cmd.Result);
        else
            logger.LogError(cmd.Error);
    }
}