using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Tests.Helpers;

namespace PaymentService.Tests;

public class PaymentInMemoryTests
{
    private static List<Contact> FakeContacts()
    {
        return new List<Contact>()
        {
            new Contact
            {
                Name = "Test One",
                Address = new Address
                {
                    AddressLine1 = "Test One Str",
                    AddressLine2 = "3",
                    AddressLine3 = "line 3",
                    City = null,
                    PostalCode = "532032",
                    CountryCode = "CY"
                },
                Account = new BankAccount
                {
                    AccountNumber = "ASD123123",
                    Bic = "BCYO12312"
                }
            },
            new Contact
            {
                Name = "Test Two",
                Address = new Address
                {
                    AddressLine1 = "Test Two Str",
                    AddressLine2 = "14",
                    AddressLine3 = null,
                    City = null,
                    PostalCode = "532032",
                    CountryCode = "CY"
                },
                Account = new BankAccount
                {
                    AccountNumber = "ASD333",
                    Bic = "BCYO12312"
                }
            },
            new Contact
            {
                Name = "Test Three",
                Address = new Address
                {
                    AddressLine1 = "Test Three Str",
                    AddressLine2 = "1",
                    AddressLine3 = null,
                    City = null,
                    PostalCode = "532032",
                    CountryCode = "CY"
                },
                Account = new BankAccount
                {
                    AccountNumber = "ASD44444",
                    Bic = "BCYO12312"
                }
            }
        };
    }

    private static HttpContext CreateMockHttpContext() =>
        new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream(),
            },
        };

    [Fact]
    public async Task GetPaymentRetrunsNotFoundIfNotExists()
    {
        await using var context = new MockDb().CreateDbContext();
        var paymentRepoMock = new PaymentRepository(context);

        var result = await PaymentEndpoints.GetPaymentById(1, paymentRepoMock);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task CreatePaymentStoresValidPayment()
    {
        await using var context = new MockDb().CreateDbContext();
        var contactRepoMock = new ContactRepository(context);
        var paymentRepoMock = new PaymentRepository(context);
        var mockHttpContext = CreateMockHttpContext();

        var originator = FakeContacts().Find(c => c.Name == "Test One");
        var beneficiary = FakeContacts().Find(c => c.Name == "Test Two");

        Payment newPayment = new Payment
        {
            Amount = 388.50,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        var validatedPayment = await GenerateValidatedObjectFromObject(newPayment);

        var createResult =
            await PaymentEndpoints.CreatePayment(validatedPayment, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult);

        await createResult.ExecuteAsync(mockHttpContext);

        mockHttpContext.Response.Body.Position = 0;

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var responsePayment =
            await JsonSerializer.DeserializeAsync<Payment>(mockHttpContext.Response.Body, jsonOptions);
        var getResult = await PaymentEndpoints.GetPaymentById(responsePayment.Id, paymentRepoMock);
        Assert.IsType<Ok<Payment>>(getResult);
        Assert.Equal(newPayment.Amount, responsePayment.Amount);
        Assert.Equal(newPayment.Beneficiary.Name, responsePayment.Beneficiary.Name);
        Assert.Equal(newPayment.Originator.Name, responsePayment.Originator.Name);
        Assert.Equal(newPayment.PaymentCurrency, responsePayment.PaymentCurrency);
    }

    [Fact]
    public async Task CreatePaymentDoesNotStoreInvalidPayment()
    {
        await using var context = new MockDb().CreateDbContext();
        var contactRepoMock = new ContactRepository(context);
        var paymentRepoMock = new PaymentRepository(context);

        var originator = FakeContacts().Find(c => c.Name == "Test One");
        var beneficiary = FakeContacts().Find(c => c.Name == "Test Two");

        var newPayment = new Payment
        {
            Amount = 390.00,
            //empty currency - payment invalid
            PaymentCurrency = "",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        var validatedPayment = await GenerateValidatedObjectFromObject(newPayment);

        var createResult =
            await PaymentEndpoints.CreatePayment(validatedPayment, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<BadRequest<IDictionary<string, string[]>>>(createResult);
    }

    [Fact]
    public async Task GetAllCreatedPayments()
    {
        await using var context = new MockDb().CreateDbContext();
        var contactRepoMock = new ContactRepository(context);
        var paymentRepoMock = new PaymentRepository(context);
        var mockHttpContext = CreateMockHttpContext();

        var originator = FakeContacts().Find(c => c.Name == "Test One");
        var beneficiary = FakeContacts().Find(c => c.Name == "Test Two");

        Payment newPayment = new Payment
        {
            Amount = 388.50,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        Payment newPayment2 = new Payment
        {
            Amount = 400.00,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        Payment newPayment3 = new Payment
        {
            Amount = 260.33,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        //Create the 3 payments
        var validatedPayment = await GenerateValidatedObjectFromObject(newPayment);
        var createResult =
            await PaymentEndpoints.CreatePayment(validatedPayment, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult);

        var validatedPayment2 = await GenerateValidatedObjectFromObject(newPayment2);
        var createResult2 =
            await PaymentEndpoints.CreatePayment(validatedPayment2, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult2);

        var validatedPayment3 = await GenerateValidatedObjectFromObject(newPayment3);
        var createResult3 =
            await PaymentEndpoints.CreatePayment(validatedPayment3, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult3);
        
        //Get All payments
        var getResult = await PaymentEndpoints.GetPayments(mockHttpContext.Request, paymentRepoMock);

        await getResult.ExecuteAsync(mockHttpContext);
        mockHttpContext.Response.Body.Position = 0;
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var responsePayments =
            await JsonSerializer.DeserializeAsync<List<Payment>>(mockHttpContext.Response.Body, jsonOptions);

        Assert.NotNull(responsePayments);
        Assert.Equal(3, responsePayments.Count);
    }
    
    [Fact]
    public async Task GetAllCreatedPaymentsFiltered()
    {
        await using var context = new MockDb().CreateDbContext();
        var contactRepoMock = new ContactRepository(context);
        var paymentRepoMock = new PaymentRepository(context);
        var mockHttpContext = CreateMockHttpContext();

        var originator = FakeContacts().Find(c => c.Name == "Test One");
        var beneficiary = FakeContacts().Find(c => c.Name == "Test Two");

        Payment newPayment = new Payment
        {
            Amount = 388.50,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        Payment newPayment2 = new Payment
        {
            Amount = 400.00,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        Payment newPayment3 = new Payment
        {
            Amount = 260.33,
            PaymentCurrency = "EUR",
            Originator = originator,
            Beneficiary = beneficiary,
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        //Create the 3 payments
        var validatedPayment = await GenerateValidatedObjectFromObject(newPayment);
        var createResult =
            await PaymentEndpoints.CreatePayment(validatedPayment, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult);

        var validatedPayment2 = await GenerateValidatedObjectFromObject(newPayment2);
        var createResult2 =
            await PaymentEndpoints.CreatePayment(validatedPayment2, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult2);

        var validatedPayment3 = await GenerateValidatedObjectFromObject(newPayment3);
        var createResult3 =
            await PaymentEndpoints.CreatePayment(validatedPayment3, contactRepoMock, paymentRepoMock, context);

        Assert.IsType<Created<Payment>>(createResult3);
        
        //Get filtered payments by amount
        mockHttpContext.Request.QueryString = new QueryString(
            "?maxAmount=390.00");
        var getResult = await PaymentEndpoints.GetPayments(mockHttpContext.Request, paymentRepoMock);

        await getResult.ExecuteAsync(mockHttpContext);
        mockHttpContext.Response.Body.Position = 0;
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var responsePayments =
            await JsonSerializer.DeserializeAsync<List<Payment>>(mockHttpContext.Response.Body, jsonOptions);

        Assert.NotNull(responsePayments);
        Assert.Equal(2, responsePayments.Count);
    }

    
    private async Task<Validated<Payment>> GenerateValidatedObjectFromObject(Payment payment)
    {
        var value = payment;
        var validator = new PaymentValidator();

        var results = await validator.ValidateAsync(value);

        return new Validated<Payment>(value, results);
    }
}