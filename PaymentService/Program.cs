using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Repositories;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

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

    //CP Add the xml comments to the Open API docs
    //This adds description of API endpoints, description of the dto class, including their properties etc
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
        var filePath = Path.Combine(AppContext.BaseDirectory, "PaymentService.xml");
        c.IncludeXmlComments(filePath);
    });

    //CP Add problem details middleware converts all empty error responses to use the Problem details pattern
    //Without this the default behavior is to return an empty response which is not ideal when consuming this from the api client
    //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errors?view=aspnetcore-8.0
    //ExceptionHandlerMiddleware: Generates a problem details response when a custom handler is not defined.
    //StatusCodePagesMiddleware: Generates a problem details response by default.
    //DeveloperExceptionPageMiddleware: Generates a problem details response in development when the Accept request HTTP header doesn't include text/html.
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    //CP Use status pages to return a specific response for default empty errors such as 404 errors
    //Without this the default behavior is to return an empty response which is not ideal when consuming this from the api client
    //In combination with builder.Services.AddProblemDetails(), this will return a Problem details response
    app.UseStatusCodePages();
    //If you need to customize status code pages, then this can be done here
    //For example we can configure this middleware to always return a Problem details response by configuring the middlware like so:
    //async statusCodeContext
    //    => await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
    //            .ExecuteAsync(statusCodeContext.HttpContext));

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