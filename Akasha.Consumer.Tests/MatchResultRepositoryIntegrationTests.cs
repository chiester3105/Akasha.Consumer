using Testcontainers.PostgreSql;

namespace Akasha.Consumer.Tests
{
    public class MatchResultRepositoryIntegrationTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer =
            new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
    
        

        public async Task DisposeAsync()
        {
            
        }

        public async Task InitializeAsync()
        {
            
        }
    }
}
