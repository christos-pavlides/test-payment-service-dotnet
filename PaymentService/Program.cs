using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using FluentValidation;
using FluentValidation.Results;
using static Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ContactDb>(opt => opt.UseInMemoryDatabase("ContactList"));
builder.Services.AddDbContext<PaymentDb>(opt => opt.UseInMemoryDatabase("PaymentList"));
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