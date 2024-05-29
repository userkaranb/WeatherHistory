using Jubilado.Persistence;
using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Amazon.SecretsManager;
using Amazon.DynamoDBv2;
using Microsoft.EntityFrameworkCore.Metadata;

public class Program
{
    public static void Main(string[] args)
    {
        // Kill process running on port 5084
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"kill -9 $(lsof -t -i:5084)\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        try
        {
            var process = new Process { StartInfo = processStartInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing process: {ex.Message}");
        }

        var builder = WebApplication.CreateBuilder(args);

        // Autofac configuration
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterType<DataLayer>().As<IDataLayer>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<CityCreatorService>().As<ICityCreatorService>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<CityWeatherHistoryApiCaller>().As<ICityWeatherHistoryApiCaller>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<CityGetterService>().As<ICityGetterService>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<TableLoader>().As<ITableLoader>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<AmazonSecretsManagerClient>().As<IAmazonSecretsManager>().SingleInstance();
                containerBuilder.RegisterType<CredentialService>().AsSelf().SingleInstance();

                containerBuilder.Register(context =>
                {
                    var credentialService = context.Resolve<CredentialService>();
                    return credentialService.GetInitializedDynamoClient().GetAwaiter().GetResult();
                }).As<IAmazonDynamoDB>().SingleInstance();
            });

        // Add services to the container
        builder.Services.AddRazorPages();
        builder.Services.AddControllers();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();

        app.Run();
    }
}
