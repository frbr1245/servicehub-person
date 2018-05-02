﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CM = Housing.Foundation.Person.Context.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Housing.Foundation.Library.Interfaces;
using Swashbuckle.AspNetCore.Swagger;
using Housing.Foundation.Library.Models;

namespace Housing.Foundation.Person.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public Dictionary<string, string> SalesforceConfig { get; private set; }
        public Dictionary<string, string> SalesforceURLs { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRepository<CM.Person>, CM.PersonRepository>();
            //add and configure Di with setting class, get connection string and database name from appsettings.json
            //settings will be access via IOptions<Settings>
            services.Configure<Settings>(Options =>
            {
                Options.ConnectionString = Configuration.GetSection("MongoDB:ConnectionString").Value;
                Options.Database = Configuration.GetSection("MongoDB:Database").Value;
                Options.CollectionName = "PersonNew";

                Options.CacheExpirationMinutes = int.Parse(Configuration.GetSection("CacheExpirationMinutes").Value);
            
                Options.ClientId = Configuration.GetSection("Salesforce:client_id").Value;
                Options.ClientSecret = Configuration.GetSection("Salesforce:client_secret").Value;
                Options.Username = Configuration.GetSection("Salesforce:username").Value;
                Options.Password = Configuration.GetSection("Salesforce:password").Value;

                Options.BaseURL = Configuration.GetSection("SalesforceURLs:Base").Value;
                Options.LoginURLExtension = Configuration.GetSection("SalesforceURLs:Login_Extension").Value;
                Options.ResourceBaseExtension = Configuration.GetSection("SalesforceURLs:Resource_Base_Extension").Value;
            });
            services.AddMvc();

            services.AddCors(o => o.AddPolicy("Open", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Revature Housing: Person API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors("Open");

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Revature Housing: Person API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
