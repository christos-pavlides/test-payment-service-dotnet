using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService;

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
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/payment", CreatePayment);
app.MapGet("/payment", GetPayments);
app.MapPost("/contact", CreateContact);
app.MapGet("/contact", GetContacts);

app.Run();

static async Task<IResult> CreateContact(Contact contact, ContactDb db)
{
    db.Add(contact);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/contact/{contact.Id}", contact);
}

static async Task<IResult> GetContacts(ContactDb db)
{
    return TypedResults.Ok(await db.Contacts.Include(c => c.Address).ToListAsync());
}

static async Task<IResult> CreatePayment(Payment payment, PaymentDb db)
{
    db.Add(payment);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/payment/{payment.Id}", payment);
}

static async Task<IResult> GetPayments(PaymentDb db)
{
    return TypedResults.Ok(
        await db.Payments
            .Include(p => p.Beneficiary.Address)
            .Include(p => p.Beneficiary.Account)
            .Include(p => p.Originator.Address)
            .Include(p => p.Originator.Account)
            .ToListAsync()
        );
}