using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService;

public static class ContactEndpoints
{
    public static void RegisterContactEndpoints(this WebApplication app)
    {
        app.MapPost("/contact", CreateContact);
        app.MapGet("/contact", GetContacts);

    }
    
    static async Task<IResult> CreateContact(Validated<Contact> req, ApiDbContext db)
    {
        var (isValid, value) = req;

        if (!isValid)
        {
            return TypedResults.BadRequest(new ProblemDetails {Title = "Validation Failed"});
        }
        
        db.Add(value);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/contact/{value.Id}", value);
    }

    static async Task<IResult> GetContacts(ApiDbContext db)
    {
        return TypedResults.Ok(await db.Contacts.Include(c => c.Address).ToListAsync());
    }

}