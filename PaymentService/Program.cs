using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using FluentValidation;
using PaymentService.Data;
using Serilog;
using Serilog.Events;

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    
    // // Configure Serilog
    // Log.Logger = new LoggerConfiguration()
    //     .MinimumLevel.Debug()
    //     .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    //     .Enrich.FromLogContext()
    //     .WriteTo.Console()
    //     .CreateLogger();
    //
    // builder.Host.UseSerilog(); // Add Serilog to the host
    

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
    // app.UseSerilogRequestLogging();
    
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
