using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SpaServices.Webpack;
using NSwag;
using NSwag.Generation.Processors.Security;
using PMCDash.Repos;
using PMCDash.Services;
using PMCDash.Helper;
using PMCDash.Formatter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO;
using Microsoft.Extensions.FileProviders;
namespace PMCDash
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
                        options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
                            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                            // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
                            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

                            // 一般我們都會驗證 Issuer
                            ValidateIssuer = true,
                            ValidIssuer = Configuration.GetValue<string>("JwtBearerSettings:Issuer"),

                            // 通常不太需要驗證 Audience
                            ValidateAudience = false,

                            // 一般我們都會驗證 Token 的有效期間
                            ValidateLifetime = true,

                            // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                            ValidateIssuerSigningKey = false,

                            // "1234567890123456" 應該從 IConfiguration 取得
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("JwtBearerSettings:SignKey")))
                        };
                    });
            services.AddCors(options =>
                options.AddDefaultPolicy(x => x.AllowAnyOrigin()
                                               .AllowAnyMethod()
                                               .AllowAnyHeader())
            );
            services.AddControllers(options => options.OutputFormatters.Insert(0, new JsonOutputFormatter()));
            services.AddOpenApiDocument(document =>
            {
                document.Title = "PMC API";
                var openApiSecurityScheme = new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    Description = "請輸入Token，格式如: Bearer {Token}",
                    In = OpenApiSecurityApiKeyLocation.Header
                };
                document.AddSecurity("JwtBearer",
                                     Enumerable.Empty<string>(),
                                     openApiSecurityScheme);

                document.OperationProcessors
                        .Add(new AspNetCoreOperationSecurityScopeProcessor("JwtBearer"));
            });
            

            services.AddSingleton<DistributionRepo>();

            services.AddScoped<DeviceDistributionService>();
            services.AddScoped<AccountService>();
            services.AddScoped<AlarmService>();
            services.AddSingleton<JwtProviderHelper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHsts();
            app.UseAuthentication();
            app.UseOpenApi(settings =>
            {
                settings.PostProcess = (document, request) => { /*document.Host = @"ab902b19b9e8.ngrok.io";*/ };
            });
            app.UseSwaggerUi3();
            app.UseCors();
            app.UseRouting();
            app.UseStaticFiles();         
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.ConfigSpaHost(env, "ClientApp", 8070);
        }       
    }
}
