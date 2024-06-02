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
        //Create a payment
        app.MapPost("/payment", CreatePayment)
            .Accepts<Payment>("application/json")
            .WithOpenApi();

        //Get payments
        app.MapGet("/payment", GetPayments)
            .WithName("GetPayments")
            .WithOpenApi(generatedOperation =>
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
            })
            .Produces<List<Payment>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        //Get Payments by id
        app.MapGet("/payment/{id}", GetPaymentById)
            .WithName("GetPaymentById")
            .WithOpenApi(generatedOperation =>
            {
                var parameter = generatedOperation.Parameters.FirstOrDefault(p => p.Name == "id");

                parameter.Description = "The id of the Payment to fetch";
                parameter.Required = true;

                return generatedOperation;
            }).Produces<Payment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
    /// <summary>
    /// Get payment by id 123
    /// </summary>
    /// <param name="id"></param>
    /// <param name="paymentRepo"></param>
    /// <returns></returns>

    public static async Task<IResult> GetPaymentById(int id, IPaymentRepository paymentRepo)
    {
        return await paymentRepo.GetPaymentByIdAsync(id) is Payment payment
            ? TypedResults.Ok(payment)
            : (IResult)TypedResults.NotFound();
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    /// <param name="req"></param>
    /// <param name="contactRepo"></param>
    /// <param name="paymentRepo"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    public static async Task<IResult> CreatePayment(Validated<Payment> req, IContactRepository contactRepo,
        IPaymentRepository paymentRepo, ApiDbContext db)
    {
        var (isValid, value) = req;

        if (!isValid) return TypedResults.BadRequest(req.Errors);

        //CP This is a good point to discuss design/architecture
        //My starting point is that it looks wrong to me that you are doing this check and starting a db transactian in the API for a number of reasons
        //The most important of which are:
        //1. The two repositories are injected here via their interfaces, we don't really know how each repository is implemented, it could be a nosql db, a file based structure, a postgres db etc
        //2. The point of using repositories is to abstract the data layer, and in this case we have to use the ApiDbContext, which negates the main purpose of the repositories
        //3. The two repositories might exist on different databases or the payment repository could even be retrieving/sending data to an external API, so it might not support transactions
        //
        //One simple solution is to create another interface (I usually call these interfaces/classes "xxxxService" or "xxxxEngine" that will handle these complexities
        //This Payments service class that will implement the interface can accept the Payment request and then we can implement this logic in that class
        //In the case where we know that this specific PaymentsService implementation used a common db and the contact data and the payment data are in the same relational database
        //Then we can do one of the following:
        //1. Use the dbContext directly in the PaymentService to do something like this code here
        //2. Create a "Repository" abstraction specifically for the PaymentService, instead per db entity.
        //This means that instead of using the IContactRepository and IPaymentRepository, the IPaymentRepository could be changed to accept the contact details as well
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
        //Prefer to use a custom DTO class for the parameters, which can provide a number of benefits vs reading the request parameters
        //e.g. a DTO is typed, property names are checked during compilation, documentation can be added for each property, validation can be done with fluent validation/data attributes  etc
        StringValues idValues = request.Query["id"];
        StringValues fromDateValue = request.Query["from"];
        StringValues toDateValue = request.Query["to"];
        StringValues minAmountValue = request.Query["minAmount"];
        StringValues maxAmountValue = request.Query["maxAmount"];

        //CP Nullability warnings usually point to some issues in the code, therefore these should be corrected
        //In this case if idValues.Count <= 0  then ids will be set to null
        //But IEnumerable<int> is not defined as nullable. 
        //To correct this issue, you can define IEnumerable<int> as nullable by adding a ? at the end => IEnumerable<int>?
        IEnumerable<int> ids = idValues.Count > 0 ? idValues.ToString().Split(',').Select(int.Parse) : null;
        DateTime? fromDate = DateTime.TryParse(fromDateValue, out var from) ? from.ToUniversalTime() : null;
        DateTime? toDate = DateTime.TryParse(toDateValue, out var to) ? to.ToUniversalTime() : null;
        double? minAmount = double.TryParse(minAmountValue, out var min) ? min : null;
        double? maxAmount = double.TryParse(maxAmountValue, out var max) ? max : null;

        try
        {
            //CP Nullability #2, since IEnumerable<int> is expected to be nullable some times but the GetPaymentsAsync 
            //does not accept a nullable ids collection, you get this error.
            //To fix this you can either check if ids is null and not call this method if it is null,
            //or change GetPaymentsAsync to accept nullable values and do the nullability check in the GetPaymentsAsync function
            List<Payment> payments = await paymentRepository.GetPaymentsAsync(ids, fromDate, toDate, minAmount, maxAmount);
            return TypedResults.Ok(payments);
        }
        catch (Exception e)
        {
            //CP
            //Point #1 Fatal is usually used when the application crashes and cannot be recovered
            //Prefer to use .Error( for these kind of exceptions
            //Point #2 You re using serilog directly here, which breaks the ILogger abstraction
            //It would be preferable if an ILogger<> is injected here and to use the ILogger to log instead of using Serilog directly
            //There are a few ways to inject the ILogger, that you can look up and choose the best one for your use case
            Log.Fatal(e, "Something went wrong with getting payments");
            //Use Problem instead of a bad request to make sure a consisten response is returned to the client (json formatted)
            return TypedResults.Problem("Something went wrong with getting payments");
        }
    }

}