using System;
using System.IO;
using Invest.Core;
using Invest.Core.Enums;
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

	        builder.AddCurRates(
		        //new CbrCurrencyRate(new[]{ Currency.Usd, Currency.Eur }){ StartDate = new DateTime(2019, 1,1), EndDate = DateTime.Today }
				new FakeCurrencyRate(new[]{ Currency.Usd, Currency.Eur }){ StartDate = new DateTime(2019, 12,1) }
			);
			
	        builder.AddStocks(new JsonStocksLoader(fileName));
			
	        var dir = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal)));
	        builder.AddReport(new VtbBrokerReport(Path.Combine(dir, @"Downloads\vtb"), builder));
	        builder.AddReport(new SberBrokerReport(Path.Combine(dir, @"Downloads\sbr"), builder));
	        builder.AddReport(new AlfaBrokerReport(Path.Combine(dir, @"Downloads\ab"), builder));
			
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
