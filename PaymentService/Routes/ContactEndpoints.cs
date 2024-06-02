using Microsoft.EntityFrameworkCore;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService;

public static class ContactEndpoints
{
    public static void RegisterContactEndpoints(this WebApplication app)
    {
        //CP Endpoints must define the accept/produces
        app.MapPost("/contact", CreateContact)
            .WithOpenApi()
            .Accepts<Contact>("application/json");
        app.MapGet("/contact", GetContacts)
            .WithOpenApi();

    }

    static async Task<IResult> CreateContact(Validated<Contact> req, IContactRepository contactRepo, ApiDbContext db)
    {
        var (isValid, value) = req;

        if (!isValid)
        {
            //CP: Add the errors to the response
            return TypedResults.ValidationProblem(req.Errors);
        }

        await contactRepo.AddContactAsync(value);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/contact/{value.Id}", value);
    }

    static async Task<IResult> GetContacts(ApiDbContext db)
    {
        return TypedResults.Ok(await db.Contacts.Include(c => c.Address).ToListAsync());
    }

}