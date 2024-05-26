
/*
* STEPS:
* Get app running locally
* Move this route to controller
* Create city class and put city object
* Create route to get city info
* Download Postman to do the same
* Create route to put weather history
* Create route to get weather history
* Create a list of cities that I care about
* Get one day worth of weather for each city, see what is missing, and fix
* Make it so that script parses every city and casts results into weatherhistory objects
* Write to db (BACKFILL)

* CREATE A NEW LAMBDA WITHIN THIS SOLUTION (FIGURE OUT HOW TO CREATE NEW PROJECTS)
* HAVE THE LAMBDA DO SOMETHING SIMPLE -- YOU INVOKE IT, AND IT PRINTS SOMETHING
* CONNECT IT TO THE DATABASE - SOMETHING SIMPLE, JUST GET A RECORD FROM THE DB
* CONNECT IT TO CALL GETWEATHERHISTORYBYCITY
* WRITE ACTUAL MAD FUNCTION FOR WEATHER SCORE. foo.
....* ... 
*/

using Jubilado.Persistence;
using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Amazon.SecretsManager;
using Amazon.DynamoDBv2;


ProcessStartInfo processStartInfo = new ProcessStartInfo
{
    FileName = "/bin/bash", // For Unix-like systems
    Arguments = $"-c \"kill -9 $(lsof -t -i:5084)\"",
    RedirectStandardOutput = true,
    UseShellExecute = false
};

// Start the process
Process process = new Process
{
    StartInfo = processStartInfo
};
process.Start();

// Read the output
string output = process.StandardOutput.ReadToEnd();

// Wait for the process to exit
process.WaitForExit();
var autoFacBuilder = new ContainerBuilder();
autoFacBuilder.RegisterType<DataLayer>().As<IDataLayer>();
autoFacBuilder.RegisterType<CityCreatorService>().As<ICityCreatorService>();
autoFacBuilder.RegisterType<Backfiller>().As<IBackfiller>();
autoFacBuilder.RegisterType<CityWeatherHistoryApiCaller>().As<ICityWeatherHistoryApiCaller>();
var container = autoFacBuilder.Build();

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        // Register your Autofac services here
        containerBuilder.RegisterType<CityCreatorService>().As<ICityCreatorService>();
        containerBuilder.RegisterType<AmazonSecretsManagerClient>().As<IAmazonSecretsManager>();
        containerBuilder.RegisterType<CredentialService>().AsSelf();

        containerBuilder.Register(context =>
        {
            var credentialService = context.Resolve<CredentialService>();
            return credentialService.GetInitializedDynamoClient().GetAwaiter().GetResult();
        }).As<IAmazonDynamoDB>();
        containerBuilder.RegisterType<DataLayer>().As<IDataLayer>();
        containerBuilder.RegisterType<CityWeatherHistoryApiCaller>().As<ICityWeatherHistoryApiCaller>();
        containerBuilder.RegisterType<CityGetterService>().As<ICityGetterService>();
    });


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseRouting();

app.MapGet("/foo", async context =>
{
    await context.Response.WriteAsync("Hello World!");
});

app.MapControllers();

app.UseHttpsRedirection(); 
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Backfiller.Execute();
// Backfiller.ExecuteWeatherScoreBackfill();

// var backfiller = container.Resolve<IBackfiller>();
// backfiller.CreateGenericCityWeatherScoreSK();

// TO DO:

// Remove db credentials
// Add Tests
// Next API integration?

app.Run();

