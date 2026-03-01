using BurmaProjectIdeasYarp.Services;
using BurmaProjectIdeasYarp;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Burma Project Ideas API Gateway",
        Version = "v1",
        Description = "YARP Reverse Proxy Gateway for Burma Project Ideas APIs"
    });
});

// Configure LiteDB
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "yarp_config.db");
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(dbPath));
builder.Services.AddSingleton<YarpConfigService>();
builder.Services.AddSingleton<MigrationService>();

// Custom proxy config provider that uses DynamicProxyConfigProvider
builder.Services.AddSingleton<IProxyConfigProvider, DynamicProxyConfigProvider>();

// Add YARP services
builder.Services.AddReverseProxy();

var app = builder.Build();

// Run migration
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<MigrationService>();
    await migrationService.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseHttpsRedirection();

// Add Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Burma Project Ideas API Gateway v1");
    c.RoutePrefix = "swagger";
});

// Map MVC routes
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapReverseProxy();

app.Run();
