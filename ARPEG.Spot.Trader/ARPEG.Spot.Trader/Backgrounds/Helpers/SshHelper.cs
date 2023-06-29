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
        string login,
        string password,
        ILogger logger)
    {
        var serverAddress = "172.17.0.1";

        if (Debugger.IsAttached) serverAddress = "192.168.55.140";

        var client = new SshClient(serverAddress, 22, login, password);
        client.Connect();

        if (FetchSolarWiFi(client, logger))
        {
            SetPromtailId(client, logger);
        }

        if (!WiFiIsConnected(client, logger))
        {
            WifiConnection(client, password, logger);
        }

        return FetchAllMyIps(client, logger);
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

    private static void SetPromtailId(SshClient ssh, ILogger logger)
    {
        using var cmd = ssh.RunCommand($"nmcli -f ssid dev wifi | grep Solar | sed 's/ *$//g' | head -1 |xargs -I % sed -i s/arpeg-1/arpeg-%/g promtailconfig.yml");
        if (cmd.ExitStatus == 0)
            logger.LogInformation(cmd.Result);
        else
            logger.LogError(cmd.Error);
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
    
    private static bool FetchSolarWiFi(SshClient ssh,
        ILogger logger)
    {
        logger.LogInformation("Fetch Solar WiFi");
        using var cmd = ssh.RunCommand($"nmcli -f ssid dev wifi");
        if (cmd.ExitStatus == 0)
        {
            logger.LogInformation(cmd.Result);
            return cmd.Result.Contains("Solar");
        }
        else
        {
            logger.LogError(cmd.Error);
            return false;
        }
    }
}