using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using FluentValidation;
using PaymentService.Data;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");

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

app.MapGet("/", () => "Hello World!");

app.RegisterPaymentEndpoints();
app.RegisterContactEndpoints();
app.Run();