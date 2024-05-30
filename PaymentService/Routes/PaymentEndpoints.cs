using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Repositories;
using Serilog;

namespace PaymentService;

public static class PaymentEndpoints
{
    public static void RegisterPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/payment", CreatePayment)
            .Accepts<Payment>("application/json")
            .WithOpenApi();

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
                Example = new OpenApiString("2024-05-27"),
                Schema = new OpenApiSchema { Type = "string", Format = "date" }
            };
            generatedOperation.Parameters.Add(fromDateParameter);

            var toDateParameter = new OpenApiParameter
            {
                Name = "to",
                In = ParameterLocation.Query,
                Description = "Filter payments up to the specified date (inclusive).",
                Required = false,
                Example = new OpenApiString("2024-05-27"),
                Schema = new OpenApiSchema { Type = "string", Format = "date" }
            };
            generatedOperation.Parameters.Add(toDateParameter);

            var minAmountParameter = new OpenApiParameter
            {
                Name = "minAmount",
                In = ParameterLocation.Query,
                Description = "Filter payments with a minimum amount.",
                Required = false,
                Example = new OpenApiString("360.50"),
                Schema = new OpenApiSchema { Type = "number", Format = "decimal" }
            };
            generatedOperation.Parameters.Add(minAmountParameter);

            var maxAmountParameter = new OpenApiParameter
            {
                Name = "maxAmount",
                In = ParameterLocation.Query,
                Description = "Filter payments with a maximum amount.",
                Required = false,
                Example = new OpenApiString("360.50"),
                Schema = new OpenApiSchema { Type = "number", Format = "decimal" }
            };
            generatedOperation.Parameters.Add(maxAmountParameter);

            return generatedOperation;
        }).Produces<List<Payment>>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

        app.MapGet("/payment/{id}", GetPaymentById).WithName("GetPaymentById").WithOpenApi(generatedOperation =>
            {
                var parameter = generatedOperation.Parameters.FirstOrDefault(p => p.Name == "id");

                parameter.Description = "The id of the Payment to fetch";
                parameter.Required = true;

                return generatedOperation;
            }).Produces<Payment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    public static async Task<IResult> GetPaymentById(int id, IPaymentRepository paymentRepo)
    {
        return await paymentRepo.GetPaymentByIdAsync(id) is Payment payment
            ? TypedResults.Ok(payment)
            : (IResult)TypedResults.NotFound();
    }

    public static async Task<IResult> CreatePayment(Validated<Payment> req, IContactRepository contactRepo,
        IPaymentRepository paymentRepo, ApiDbContext db)
    {
        var (isValid, value) = req;

        if (!isValid) return TypedResults.BadRequest(req.Errors);

        using var transaction = db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory"
            ? db.Database.BeginTransaction()
            : null;

        try
        {
            Contact? beneficiary = await contactRepo.GetContactByNameAsync(value.Beneficiary.Name);

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

                await contactRepo.AddContactAsync(beneficiary);
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

            Contact? originator = await contactRepo.GetContactByNameAsync(value.Originator.Name);
            
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

                await contactRepo.AddContactAsync(originator);
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

            await paymentRepo.AddPaymentAsync(value);
            await db.SaveChangesAsync();

            transaction?.Commit();
            
            return TypedResults.Created($"/payment/{value.Id}", value);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application terminated unexpectedly");
            transaction?.Rollback();
            return TypedResults.BadRequest("Something went wrong with storing payment");
        }
    }

    public static async Task<IResult> GetPayments(HttpRequest request, IPaymentRepository paymentRepository)
    {
        StringValues idValues = request.Query["id"];
        StringValues fromDateValue = request.Query["from"];
        StringValues toDateValue = request.Query["to"];
        StringValues minAmountValue = request.Query["minAmount"];
        StringValues maxAmountValue = request.Query["maxAmount"];

        IEnumerable<int> ids = idValues.Count > 0 ? idValues.ToString().Split(',').Select(int.Parse) : null;
        DateTime? fromDate = DateTime.TryParse(fromDateValue, out var from) ? from.ToUniversalTime() : null;
        DateTime? toDate = DateTime.TryParse(toDateValue, out var to) ? to.ToUniversalTime() : null;
        double? minAmount = double.TryParse(minAmountValue, out var min) ? min : null;
        double? maxAmount = double.TryParse(maxAmountValue, out var max) ? max : null;

        try
        {
            List<Payment> payments = await paymentRepository.GetPaymentsAsync(ids, fromDate, toDate, minAmount, maxAmount);
            return TypedResults.Ok(payments);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Something went wrong with getting payments");
            return TypedResults.BadRequest("Something went wrong with getting payments");
        }
    }

}