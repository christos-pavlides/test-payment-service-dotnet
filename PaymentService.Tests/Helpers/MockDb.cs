using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaymentService.Data;

namespace PaymentService.Tests.Helpers;

public class MockDb : IDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase($"InMemoryTestDb-{DateTime.Now.ToFileTimeUtc()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApiDbContext(options);
    }
}