namespace Gifty.Tests.Integration;

using Xunit;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection : ICollectionFixture<TestApiFactory>
{
}
