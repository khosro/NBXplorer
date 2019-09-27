using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBXplorer.Logging;
#if NETCOREAPP21
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Hosting;
#endif

namespace NBXplorer
{
	public class Startup
	{
		public Startup(IConfiguration conf)
		{
			Configuration = conf;
		}
		public IConfiguration Configuration
		{
			get;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddHttpClient();
			services.AddHttpClient(nameof(RPCClientProvider), httpClient =>
			{
				httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
			});
			services.AddNBXplorer();
			services.AddEthereumLike1();
			services.ConfigureNBxplorer(Configuration);
			IMvcCoreBuilder builder = services.AddMvcCore();
#if NETCOREAPP21
			builder.AddJsonFormatters();
#else
			builder.AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
				new Serializer(null).ConfigureSerializer(options.SerializerSettings);
			});
#endif
			builder.AddMvcOptions(o => o.InputFormatters.Add(new NoContentTypeInputFormatter()))
			.AddAuthorization()
			.AddFormatterMappings();
			services.AddAuthentication("Basic")
				.AddNBXplorerAuthentication();
		}

		public void Configure(IApplicationBuilder app, IServiceProvider prov,
			IWebHostEnvironment env,
			ILoggerFactory loggerFactory, IServiceProvider serviceProvider,
			CookieRepository cookieRepository)
		{
			cookieRepository.Initialize();
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			Logs.Configure(loggerFactory);

#if !NETCOREAPP21
			app.UseRouting();
#endif
			app.UseAuthentication();
#if !NETCOREAPP21
			app.UseAuthorization();
#endif
			app.UseWebSockets();
			// app.UseMiddleware<LogAllRequestsMiddleware>();
#if NETCOREAPP21
			app.UseMvc();
#else
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
#endif
		}
	}
}
