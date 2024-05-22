using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        app.MapGet("/payment/{id}", GetPaymentById);
    }

    static async Task<IResult> GetPaymentById(int id, ApiDbContext db)
    {
        return await db.Payments.FindAsync(id)
            is Payment payment
            ? TypedResults.Ok(payment)
            : (IResult) TypedResults.NotFound();
    }
    
    static async Task<IResult> CreatePayment(Validated<Payment> req, ApiDbContext db)
    {
        //add protection regarding duplicate requests
        //throttling?
        var (isValid, value) = req;
        
        if (!isValid) return TypedResults.BadRequest(req.Errors);

        var transaction = db.Database.BeginTransaction();
        
        try
        {
            Contact beneficiary = await db.Contacts.Include("Address")
                .Include("Account").FirstOrDefaultAsync( c => c.Name == value.Beneficiary.Name);

            if (beneficiary == null)
            {
                beneficiary = new Contact
                {
                    Name = value.Beneficiary.Name,
                    Address = new Address
                    {
                        AddressLine1 = value.Beneficiary.Address.AddressLine1,
                        AddressLine2 = value.Beneficiary.Address.AddressLine2,
                        AddressLine3 = value.Beneficiary.Address.AddressLine3,
                        City = value.Beneficiary.Address.City,
                        PostalCode = value.Beneficiary.Address.PostalCode,
                        CountryCode = value.Beneficiary.Address.CountryCode
                    },
                    Account = new BankAccount
                    {
                        Bic = value.Beneficiary.Account.Bic,
                        AccountNumber = value.Beneficiary.Account.AccountNumber
                    }
                };

                db.Contacts.Add(beneficiary);
            }
            else
            {
                Address address = beneficiary.Address;
                address.AddressLine1 = value.Beneficiary.Address.AddressLine1;
                address.AddressLine2 = value.Beneficiary.Address.AddressLine2;
                address.AddressLine3 = value.Beneficiary.Address.AddressLine3;
                address.City = value.Beneficiary.Address.City;
                address.PostalCode = value.Beneficiary.Address.PostalCode;
                address.CountryCode = value.Beneficiary.Address.CountryCode;
                
            }

            value.Beneficiary = beneficiary;
            
            Contact originator = await db.Contacts.Include("Address")
                .Include("Account").FirstOrDefaultAsync( c => c.Name == value.Originator.Name);

            if (originator == null)
            {
                originator = new Contact
                {
                    Name = value.Originator.Name,
                    Address = new Address
                    {
                        AddressLine1 = value.Originator.Address.AddressLine1,
                        AddressLine2 = value.Originator.Address.AddressLine2,
                        AddressLine3 = value.Originator.Address.AddressLine3,
                        City = value.Originator.Address.City,
                        PostalCode = value.Originator.Address.PostalCode,
                        CountryCode = value.Originator.Address.CountryCode
                    },
                    Account = new BankAccount
                    {
                        Bic = value.Originator.Account.Bic,
                        AccountNumber = value.Originator.Account.AccountNumber
                    }
                };

                db.Contacts.Add(originator);
            }
            else
            {
                Address address = originator.Address;
                address.AddressLine1 = value.Originator.Address.AddressLine1;
                address.AddressLine2 = value.Originator.Address.AddressLine2;
                address.AddressLine3 = value.Originator.Address.AddressLine3;
                address.City = value.Originator.Address.City;
                address.PostalCode = value.Originator.Address.PostalCode;
                address.CountryCode = value.Originator.Address.CountryCode;
                
            }

            value.Originator = originator;
            
            db.Add(value);
            await db.SaveChangesAsync();
            
            transaction.Commit();
            return TypedResults.Created($"/payment/{value.Id}", value);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application terminated unexpectedly");
            return TypedResults.BadRequest("Something went wrong with storing payment");
        }
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
    }
}