﻿using System;
using System.IO;
using Invest.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Invest.WebApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			var b = InitLayer();

            // Add framework services.
            services.AddSingleton(b);
            services.AddMvc();
        }

        private static Builder InitLayer()
        {
	        var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	        var fileName = Path.Combine(docFolder, "stocksData.json");

	        var builder = new Builder();

	        var accounts = builder.LoadAccountsFromJson(fileName);
	        builder.SetAccounts(accounts);

	        var portolios = builder.LoadPortfoliosFromJson(fileName);
	        builder.SetPortfolios(portolios);
			
	        builder.AddStocks(new JsonStocksLoader(fileName));
			
	        var dir = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal)), "Downloads");
	        builder.AddReport(new VtbBrokerReport(dir, builder));

	        //builder.AddReport(new SberBrokerReport(){});
	        //builder.AddReport(new AlfaBrokerReport(){});
			
	        builder.Calc();

			return builder;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=TickerIndex}/{id?}");
            });
        }
    }
}
