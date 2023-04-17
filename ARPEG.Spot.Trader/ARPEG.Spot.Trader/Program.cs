// See https://aka.ms/new-console-template for more information

using System.Globalization;
using ARPEG.Spot.Trader;
using ARPEG.Spot.Trader.Constants;

CultureInfo.CurrentCulture = new CultureInfo("cs");
CultureInfo.CurrentUICulture = new CultureInfo("cs");

var builder = WebApplication
    .CreateBuilder(args);

builder.Configuration
    .AddJsonFile(AppSettings.UserAppSettingsFile, optional:true, reloadOnChange: true);

builder.Logging.AddConfiguration(builder.Configuration);

builder.Services.AddServices(builder.Configuration);

try
{
    builder.Build().RunApp();
}
catch (Exception exc)
{
    Console.WriteLine(exc.Message);

    Console.WriteLine("======================");
    Console.WriteLine(exc.StackTrace);
}
