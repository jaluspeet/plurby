using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Plurby.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(kestrel =>
                    {
                        kestrel.AddServerHeader = false; 
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
