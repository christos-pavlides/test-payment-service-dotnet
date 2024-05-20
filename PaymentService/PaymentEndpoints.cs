using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService;

public static class PaymentEndpoints
{
    public static void RegisterPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/payment", CreatePayment);
        app.MapGet("/payment", GetPayments);
    }
    
    static async Task<IResult> CreatePayment(Validated<Payment> req, ApiDbContext db)
    {
        var (isValid, value) = req;
        
        if (!isValid) return TypedResults.BadRequest(req.Errors);
        db.Add(value);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/payment/{value.Id}", value);
    }

    static async Task<IResult> GetPayments(ApiDbContext db)
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
}