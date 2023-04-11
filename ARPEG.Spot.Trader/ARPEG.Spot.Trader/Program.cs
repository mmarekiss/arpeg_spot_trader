// See https://aka.ms/new-console-template for more information

using ARPEG.Spot.Trader;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

var builder = WebApplication
    .CreateBuilder(args);

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
