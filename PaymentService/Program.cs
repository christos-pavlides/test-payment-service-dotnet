using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using FluentValidation;
using PaymentService.Data;
using Serilog;

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    var conn = builder.Configuration.GetConnectionString("DefaultConnection");

    //Register Services
    builder.Services.AddDbContext<ApiDbContext>(opt => opt.UseNpgsql(conn));
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Services.AddControllers().AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    var app = builder.Build();

    //Logging
    app.UseSerilogRequestLogging();
    
    // Register Endpoints
    app.MapGet("/", () => "Hello World!");
    app.RegisterPaymentEndpoints();
    app.RegisterContactEndpoints();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
