using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader
{
    public static class AppRun
    {
        public static void RunApp(this WebApplication app)
        {
            app.Logger.LogInformation($"ENV name = {app.Environment.EnvironmentName}");
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

            // app.MapControllers();

            app.Run();
        }
    }
}
