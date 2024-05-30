using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using FluentValidation;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Repositories;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog();
    
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
    builder.Services.AddScoped<IContactRepository, ContactRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseSerilogRequestLogging();

    // Register Endpoints
    app.MapGet("/", () => "Hello World!").ExcludeFromDescription();
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