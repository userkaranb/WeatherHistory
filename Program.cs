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
        var builder = WebApplication.CreateBuilder(args);

        // Autofac registration
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterType<CityCreatorService>().As<ICityCreatorService>();
                containerBuilder.RegisterType<AmazonSecretsManagerClient>().As<IAmazonSecretsManager>();
                containerBuilder.RegisterType<CredentialService>().AsSelf();

                containerBuilder.Register(context =>
                {
                    var credentialService = context.Resolve<CredentialService>();
                    return credentialService.GetInitializedDynamoClient().GetAwaiter().GetResult();
                }).As<IAmazonDynamoDB>();

                containerBuilder.RegisterType<TableLoader>().As<ITableLoader>();
                containerBuilder.RegisterType<DataLayer>().As<IDataLayer>();
                containerBuilder.RegisterType<CityWeatherHistoryApiCaller>().As<ICityWeatherHistoryApiCaller>();
                containerBuilder.RegisterType<CityGetterService>().As<ICityGetterService>();
            });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
