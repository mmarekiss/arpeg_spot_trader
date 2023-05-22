using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using ARPEG.Spot.Trader;
using ARPEG.Spot.Trader.Constants;



if (Environment.GetEnvironmentVariable("Env") == "Dev")
{
    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
    {
        IPInterfaceProperties ipProps = nic.GetIPProperties();
        // check if localAddr is in ipProps.UnicastAddresses
        Console.WriteLine(String.Join("; ",ipProps.UnicastAddresses.Select(x=>x.Address)));
    }
}


CultureInfo.CurrentCulture = new CultureInfo("cs");
CultureInfo.CurrentUICulture = new CultureInfo("cs");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile(AppSettings.UserAppSettingsFile, optional:true, reloadOnChange: true);

builder.Services.AddOptions();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();