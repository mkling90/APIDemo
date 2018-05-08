using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Diagnostics;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Serialization;
using AspNetCoreRateLimit;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // setupAction.ReturnHttpNotAcceptable = so a accept header will be invalid if we dont support requested type
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

                //support custom media type
                var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
                if(jsonOutputFormatter != null)
                {
                    jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.mike.hateoas+json");
                }
                var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();
                if(jsonInputFormatter != null)
                {
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.mike.author.full+json");
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.mike.author.authorwithdateofdeath+json");
                }

            })
            .AddJsonOptions(options =>
                { options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); }
                );  //allows options serializing to and from json

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();

            //To build out the url helper to build the paging header.  The url helper requires the context
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>(); //context will be null if its not singleton
            services.AddScoped<IUrlHelper, UrlHelper>(implementationFactory =>
            {
                var actionContext = implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                return new UrlHelper(actionContext);
            });

            //lightweight, stateless service, so use transient here
            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<ITypeHelperService, TypeHelperService>();

            // Caching (note: using Marvin.Cache.Headers to get ETag support)
            //services.AddHttpCacheHeaders();
            //add caching options
            services.AddHttpCacheHeaders((expirationOptions) =>
            {
                expirationOptions.MaxAge = 600; //set expiration options
            },
             (validationOptions) =>
             {
                 validationOptions.AddMustRevalidate = true; //add validation options 
             });

            //Adding a cache store
            services.AddResponseCaching();

            //rate Limiting  (nuget AspNetCoreRateLimit)
            //2 choices - rate limiting by ip, and by client
            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>((opt) =>
            {
                //would be in config files in actual code
                opt.GeneralRules = new List<RateLimitRule>()
                {
                    new RateLimitRule()
                    {
                        Endpoint="*",
                        Limit = 10,
                        Period="5m"
                    },  // limit any client to 10 requests for each 5 minutes
                    new RateLimitRule()
                    {
                        Endpoint="*",
                        Limit = 2,
                        Period="10s"
                    }  // also limit any client to 2 requests for each 10 seconds
                };
            });
            //need both a ip policy store and rate limit counter store
            //need singleton so they can be stored acrosss requests
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, 
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            //use built-in logger factory
            // loggerFactory.AddConsole(); <- not needed in core 2
            //loggerFactory.AddProvider(new NLog.Extensions.Logging.NLogLoggerProvider());
            //loggerFactory.AddNLog(); <- changes to adding into the BuildWebHost method

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //Global exception handling
                app.UseExceptionHandler( appBuilder =>
                {
                    // add code to handle the exception
                    appBuilder.Run(async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if(exceptionHandlerFeature != null)
                        {
                            var logger = loggerFactory.CreateLogger("Global exception logger");
                            logger.LogError(500 ,exceptionHandlerFeature.Error, exceptionHandlerFeature.Error.Message);
                        }
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Fault happened, try later.");
                    });  
                });
            }

            libraryContext.EnsureSeedDataForContext();

            //rate limiting goes before any other request limiting
            app.UseIpRateLimiting();

            // Caching (note: using Marvin.Cache.Headers to get ETag support)
            //Order important, add before the mvc middleware, it may need to stop requests heading to the mvc middleware

            app.UseResponseCaching(); // add cache store, should be before header generation
            app.UseHttpCacheHeaders(); //     <-  Generates the headers, but isn't the actual cache component.  ETag should work if client passes 'If-None-match' header

            app.UseMvc(); 
        }
    }
}
