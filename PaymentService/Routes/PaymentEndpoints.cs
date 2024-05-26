using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using PaymentService.Data;
using PaymentService.Models;
using Serilog;

namespace PaymentService;

public static class PaymentEndpoints
{
    public static void RegisterPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/payment", CreatePayment).Accepts<Payment>("application/json").WithOpenApi();
        app.MapGet("/payment", GetPayments).WithName("GetPayments").WithOpenApi(generatedOperation =>
        {
            var idParameter = new OpenApiParameter
            {
                Name = "id",
                In = ParameterLocation.Query,
                Description = "Filter payments by id or ids (comma delimited).",
                Required = false,
                Example = new OpenApiString("1,3"),
                Schema = new OpenApiSchema { Type = "string", Format = "number" }
            };
            generatedOperation.Parameters.Add(idParameter);
            
            var fromDateParameter = new OpenApiParameter
            {
                Name = "from",
                In = ParameterLocation.Query,
                Description = "Filter payments from the specified date (inclusive).",
                Required = false,
                Schema = new OpenApiSchema { Type = "string", Format = "date" }
            };
            generatedOperation.Parameters.Add(fromDateParameter);

            var toDateParameter = new OpenApiParameter
            {
                Name = "to",
                In = ParameterLocation.Query,
                Description = "Filter payments up to the specified date (inclusive).",
                Required = false,
                Schema = new OpenApiSchema { Type = "string", Format = "date" }
            };
            generatedOperation.Parameters.Add(toDateParameter);

            var minAmountParameter = new OpenApiParameter
            {
                Name = "minAmount",
                In = ParameterLocation.Query,
                Description = "Filter payments with a minimum amount.",
                Required = false,
                Schema = new OpenApiSchema { Type = "number", Format = "decimal" }
            };
            generatedOperation.Parameters.Add(minAmountParameter);

            var maxAmountParameter = new OpenApiParameter
            {
                Name = "maxAmount",
                In = ParameterLocation.Query,
                Description = "Filter payments with a maximum amount.",
                Required = false,
                Schema = new OpenApiSchema { Type = "number", Format = "decimal" }
            };
            generatedOperation.Parameters.Add(maxAmountParameter);

            return generatedOperation;
        });
        app.MapGet("/payment/{id}", GetPaymentById).WithName("GetPaymentById").WithOpenApi(generatedOperation =>
            {
                var parameter = generatedOperation.Parameters.FirstOrDefault(p => p.Name == "id");

                parameter.Description = "The id of the Payment to fetch";
                parameter.Required = true;

                return generatedOperation;
            }).Produces<Payment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    static async Task<IResult> GetPaymentById(int id, ApiDbContext db)
    {
        return await db.Payments.FindAsync(id)
            is Payment payment
            ? TypedResults.Ok(payment)
            : (IResult)TypedResults.NotFound();
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
                .Include("Account").FirstOrDefaultAsync(c => c.Name == value.Beneficiary.Name);

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
                .Include("Account").FirstOrDefaultAsync(c => c.Name == value.Originator.Name);

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

    static async Task<IResult> GetPayments(HttpRequest request, ApiDbContext db)
    {
        StringValues id = request.Query["id"];
        StringValues fromDate = request.Query["from"];
        StringValues toDate = request.Query["to"];
        StringValues minAmount = request.Query["minAmount"];
        StringValues maxAmount = request.Query["maxAmount"];

        try
        {
            IQueryable<Payment> payments = db.Payments;

            if (id.Count > 0)
            {
                List<string> ids = id.ToString().Split(',').ToList();
                payments = payments.Where(p => ids.Contains(p.Id.ToString()));
            }

            if (!String.IsNullOrWhiteSpace(fromDate))
            {
                DateTime from = DateTime.Parse(fromDate.ToString());
                payments = payments.Where(p => p.CreatedAt >= from);
            }

            if (!String.IsNullOrWhiteSpace(toDate))
            {
                DateTime to = DateTime.Parse(fromDate.ToString());
                payments = payments.Where(p => p.CreatedAt <= to);
            }

            if (!String.IsNullOrWhiteSpace(minAmount))
            {
                double min = double.Parse(minAmount.ToString());
                payments = payments.Where(p => p.Amount >= min);
            }

            if (!String.IsNullOrWhiteSpace(maxAmount))
            {
                double max = double.Parse(maxAmount.ToString());
                payments = payments.Where(p => p.Amount <= max);
            }

            return TypedResults.Ok(
                await payments
                    .Include(p => p.Beneficiary.Address)
                    .Include(p => p.Beneficiary.Account)
                    .Include(p => p.Originator.Address)
                    .Include(p => p.Originator.Account)
                    .ToListAsync()
            );
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Something went wrong with getting payments");
            return TypedResults.BadRequest("Something went wrong with getting payments");
        }
    }
}