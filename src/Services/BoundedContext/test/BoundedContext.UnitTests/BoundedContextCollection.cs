using Xunit;

namespace BoundedContext.UnitTests
{
    [CollectionDefinition(nameof(BoundedContextCollection))]
    public class BoundedContextCollection : ICollectionFixture<BoundedContextFixture>
    {
    }
}