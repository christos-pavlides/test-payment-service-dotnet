using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Models;
using PaymentService.Tests.Helpers;

namespace PaymentService.Tests;

public class PaymentInMemoryTests
{
    [Fact]
    public async Task GetPaymentRetrunsNotFoundIfNotExists()
    {
        await using var context = new MockDb().CreateDbContext();
        
        var result = await PaymentEndpoints.GetPaymentById(1, context);
        
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task CreatePayment()
    {
        await using var context = new MockDb().CreateDbContext();
        
        Payment newPayment = new Payment
        {
            Amount = 388.50,
            PaymentCurrency = "EUR",
            Originator = new Contact
            {
                Name = "Test Testone",
                Address = new Address
                {
                    AddressLine1 = "Markou Drakou",
                    AddressLine2 = "3",
                    AddressLine3 = "karanikki",
                    City = null,
                    PostalCode = "5320",
                    CountryCode = "CY"
                },
                Account = new BankAccount
                {
                    AccountNumber = "ASD123123",
                    Bic = "BCYO12312"
                }
            },
            Beneficiary = new Contact
            {
                Name = "Vladimir Putin",
                Address = new Address
                {
                    AddressLine1 = "Test test",
                    AddressLine2 = "12312asdasd",
                    AddressLine3 = "teasdaasdasd",
                    City = "Larnaca",
                    PostalCode = "123asdasd",
                    CountryCode = "CY"
                },
                Account = new BankAccount
                {
                    AccountNumber = "aaaaaaaa3333333",
                    Bic = "BCYO12312"
                }
            },
            ChargesBearer = ChargesBearer.Beneficiary,
            Details = "This is a necessary payment"
        };

        
        
        //todo: fix chargesBearer enum
        var paymentJson = @"
{
    ""amount"": 388.50,
    ""paymentCurrency"": ""EUR"",
    ""originator"": {
        ""name"": ""Con Kakouyshiasio"",
        ""address"": {
            ""addressLine1"": ""Markou Drakou"",
            ""addressLine2"": ""3"",
            ""addressLine3"": ""karanikki"",
            ""city"": null,
            ""postalCode"": null,
            ""countryCode"": ""CY""
        },
        ""account"": {
            ""accountNumber"": ""ASD123123"",
            ""bic"": ""BCYO12312""
        }
    },
    ""beneficiary"": {
        ""name"": ""Vladimir Putin"",
        ""address"": {
            ""addressLine1"": ""Test test"",
            ""addressLine2"": ""12312asdasd"",
            ""addressLine3"": ""teasdaasdasd"",
            ""city"": ""Larnaca"",
            ""postalCode"": ""123asdasd"",
            ""countryCode"": ""CY""
        },
        ""account"": {
            ""accountNumber"": ""aaaaaaaa3333333"",
            ""bic"": ""BCYO12312""
        }
    },
    ""chargesBearer"": 1,
    ""details"": ""This is a necessary payment""
}";
        
        // var validatedPayment = await GenerateValidatedObject(paymentJson);
        var validatedPayment = await GenerateValidatedObjectFromObject(newPayment);
        
        
        var result = await PaymentEndpoints.CreatePayment(validatedPayment, context);

        Assert.IsType<Created<Payment>>(result);
    }

    private async Task<Validated<Payment>> GenerateValidatedObject(string jsonPayment)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayment));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.ContentType = "application/json";
        
        var validationResult = new ValidationResult();
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<Payment>>(new PaymentValidator());
        var serviceProvider = services.BuildServiceProvider();
        httpContext.RequestServices = serviceProvider;

        var parameterInfo = typeof(PaymentEndpoints).GetMethod("CreatePayment").GetParameters()[0];
        
        // todo: fails because of JSON serializer enum option
        var validatedPayment = await Validated<Payment>.BindAsync(httpContext, parameterInfo);
        
        return validatedPayment;
    }

    private async Task<Validated<Payment>> GenerateValidatedObjectFromObject(Payment payment)
    {
        var value = payment;
        var validator = new PaymentValidator();
        
        var results = await validator.ValidateAsync(value);

        return new Validated<Payment>(value, results);
    }
}