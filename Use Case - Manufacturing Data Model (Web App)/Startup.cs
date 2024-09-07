using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

        var clientID = Configuration["APS_CLIENT_ID"];
        var clientSecret = Configuration["APS_CLIENT_SECRET"];
        var callbackURL = Configuration["APS_CALLBACK_URL"];
        var endpoint = Configuration["GRAPHQL_ENDPOINT"];
        var customPropertyName = Configuration["GRAPHQL_CUSTOM_PROPERTY_NAME"];
        if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(callbackURL)  
            || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(customPropertyName))
        {
            throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, APS_CALLBACK_URL, GRAPHQL_ENDPOINT or GRAPHQL_CUSTOM_PROPERTY_NAME.");
        }
        services.AddSingleton(new APS(clientID, clientSecret, callbackURL, endpoint, customPropertyName));

        var bcClientId = Configuration["BC_CLIENT_ID"];
        var bcClientSecret = Configuration["BC_CLIENT_SECRET"];
        var bcTanantId = Configuration["BC_TENANT_ID"];
        var bcEnvironment = Configuration["BC_ENVIRONMENT"];
        var bcCompany = Configuration["BC_COMPANY"];
        if (string.IsNullOrEmpty(bcClientId) || string.IsNullOrEmpty(bcClientSecret) || string.IsNullOrEmpty(bcTanantId)
            || string.IsNullOrEmpty(bcEnvironment) || string.IsNullOrEmpty(bcCompany))
        {
            throw new ApplicationException("Missing required environment variables BC_CLIENT_ID, BC_CLIENT_SECRET, BC_TENANT_ID, BC_COMPANY.");
        }
        services.AddSingleton(new BC(bcClientId, bcClientSecret, bcTanantId, bcEnvironment, bcCompany));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                context.Response.Redirect("/index.html");
                await Task.CompletedTask;
            });
            endpoints.MapGet("/partslist", async context =>
            {
                context.Response.Redirect("/index2.html^");
                await Task.CompletedTask;
            });
            endpoints.MapControllers();
        });
    }
}