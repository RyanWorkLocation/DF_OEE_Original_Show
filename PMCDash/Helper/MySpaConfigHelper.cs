using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Helper
{
    public static class MyCustomSpa
    {
       
        public static IApplicationBuilder ConfigSpaHost(this IApplicationBuilder @this, IWebHostEnvironment env,
            string folderName, int port = 9581)
        {
            //var logger = @this.ApplicationServices.CreateLogger<Startup>();
            var spaFolderPath = Path.Combine(env.ContentRootPath, "wwwroot", folderName);
            if (env.IsDevelopment())
            {
                //logger.LogInformation("Spa proxy to dev server!");
            }
            else
            {
                //logger.LogInformation($"Host spa files at: {spaFolderPath}");

                if (!Directory.Exists(spaFolderPath))
                {
                    //logger.LogWarning("Spa folder does NOT exits, system will auto create one!");
                    Directory.CreateDirectory(spaFolderPath);
                }


                @this.UseSpaStaticFiles(
                    new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider(spaFolderPath)
                    }
                );
            }

            @this.UseSpa(
                spa =>
                {
                    // put compiled out in wwwroot
                    spa.Options.DefaultPage = "/index.html";

                    if (!env.IsDevelopment())
                    {

                        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions()
                        {
                            FileProvider = new PhysicalFileProvider(spaFolderPath)
                        };
                    }

                    //spa.Options.SourcePath = "ClientApp";
#if DEBUG
                    if (env.IsDevelopment())
                    {
                        spa.UseProxyToSpaDevelopmentServer($"http://localhost:{port}");
                    }
#endif
                }
            );

            return @this;
        }
    }
}
