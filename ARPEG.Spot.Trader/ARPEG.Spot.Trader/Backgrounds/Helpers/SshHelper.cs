﻿using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ARPEG.Spot.Trader.Backgrounds.Helpers;

public class SshHelper
{
    public static IEnumerable<(string unicast, IPAddress broadcast)> ConnectWiFi(
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

    private static IEnumerable<(string unicast, IPAddress broadcast)> FetchAllMyIps(SshClient ssh,
        ILogger logger)
    {
        using var cmd = ssh.RunCommand(@$"ifconfig | grep -C1 -E 'eth0|p2p0' | grep 'inet '| sed -e 's/^ *inet \([0-9.]*\) .*broadcast \([0-9.]*\)$/\1,\2/g'");
        if (cmd.ExitStatus == 0)
        {
            logger.LogInformation("My IPs {ips}", cmd.Result);
            return FetchUnicastAndMulticast(cmd);
        }

        return Enumerable.Empty<(string unicast, IPAddress broadcast)>();
    }

    private static IEnumerable<(string unicast, IPAddress broadcast)> FetchUnicastAndMulticast(SshCommand cmd)
    {
        foreach (var row in cmd.Result.Split('\n'))
        {
            var ips = row.Split(',');
            if (ips.Length == 2
                && IPAddress.TryParse(ips[0], out _)
                && IPAddress.TryParse(ips[1], out var broadcast))
                yield return (ips[0], broadcast);
        }
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
        sn = sn.Replace(" ", "");
        using var cmd = client.RunCommand($"sed -i 's/tradersn: .*$/tradersn: {sn}/g' promtailconfig.yml");
        if (cmd.ExitStatus == 0)
            logger.LogInformation(cmd.Result);
        else
            logger.LogError(cmd.Error);
    }
    
    public static void SetupTemporalLog(ILogger logger)
    {
        using var client = GetClient();
        using var cmd = client.RunCommand($"sed -i 's/tradersn: arpeg-1/tradersn: arpeg-{Guid.NewGuid()}/g' promtailconfig.yml");
        if (cmd.ExitStatus == 0)
            logger.LogInformation(cmd.Result);
        else
            logger.LogError(cmd.Error);
    }
}