using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;
using Serilog;

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
        try
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
        catch (Exception e)
        {
            Log.Fatal(e, "Application terminated unexpectedly");
            return TypedResults.BadRequest("Something went wrong with getting payments");
        }
        finally
        {
            Log.CloseAndFlush();
        }
        
    }
}