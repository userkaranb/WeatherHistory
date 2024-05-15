
/*
* STEPS:
* Get app running locally
* Move this route to controller
* Create city class and put city object
* Create route to get city info
* Download Postman to do the same
* Create route to put weather history
* Create route to get weather history
* Create a script to Put multiple city and weather info's
* ...
*/

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("here");
// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.Run();
