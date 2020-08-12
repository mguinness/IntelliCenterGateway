using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace IntelliCenterGateway
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
            services.AddRazorPages(options => {
                options.Conventions.AuthorizePage("/Index");
            }).AddRazorRuntimeCompilation();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddJwtBearer(options =>
                {
                    options.Audience = Configuration["Token:Audience"];
                    options.ClaimsIssuer = Configuration["Token:Issuer"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token:SigningKey"] + Environment.MachineName)),
                        ValidateIssuer = false,
                        ValidateAudience = true
                    };
                });

            services.AddAuthorization(options =>
                {
                    if (Configuration.GetSection("Configuration:PrivateCIDR").Exists())
                    {
                        var cidr = Configuration["Configuration:PrivateCIDR"].Split('/');
                        var addr = IPAddress.Parse(cidr[0]);
                        var mask = Int32.Parse(cidr[1]);

                        options.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .AddRequirements(new IPRequirement(new IPNetwork(addr, mask)))
                            .Build();
                    }
                });

            services.AddSignalR();
            services.AddHttpContextAccessor();
            services.AddSingleton<IAuthorizationHandler, IPAddressHandler>();
            services.AddSingleton<TelnetBackgroundService>();
            services.AddHostedService<BackgroundServiceStarter<TelnetBackgroundService>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<GatewayHub>("/stream");
            });
        }
    }
}
