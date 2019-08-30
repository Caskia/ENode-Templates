using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BoundedContext.Api
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseKestrel(options =>
                            {
                                options.AddServerHeader = false;
                            })
                            .UseUrls("http://0.0.0.0:5001")
                            .UseStartup<Startup>();
                        });

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}