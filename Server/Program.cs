using Draw.Rodeo.Server.Hubs;
using Draw.Rodeo.Server.Services;
using Microsoft.AspNetCore.ResponseCompression;

namespace Company.WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 10 * 1024 * 1024;
                hubOptions.EnableDetailedErrors = true;
            });
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
            });

            builder.Services.AddSingleton<WordManager>();
            builder.Services.AddSingleton<PlayerManager>();
            builder.Services.AddSingleton<LobbyManager>();
            builder.Services.AddSingleton<HubManager>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.MapHub<LobbyHub>("/lobbyHub");

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");


            app.UseWebSockets();

            app.Run();
        }
    }
}