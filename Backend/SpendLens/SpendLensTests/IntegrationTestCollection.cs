namespace SpendLensTests;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection
    : ICollectionFixture<PostgresFixture>;