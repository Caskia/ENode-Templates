using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace BoundedContext.Api
{
    public class Program
    {
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                    WebHost.CreateDefaultBuilder(args)
                        .UseKestrel(options =>
                        {
                            options.AddServerHeader = false;
                        })
                        .UseUrls("http://0.0.0.0:5001")
                        .UseStartup<Startup>();

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }
    }
}