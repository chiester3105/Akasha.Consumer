using Akasha.Consumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Akasha.Consumer.Tests
{
    public class MatchResultRepositoryTests
    {
        private readonly Mock<ILogger<MatchResultRepository>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly MatchResultRepository _repo;

        public MatchResultRepositoryTests()
        {
            _loggerMock = new Mock<ILogger<MatchResultRepository>>();
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c.GetConnectionString("Postgres"))
                .Returns("Host=localhost;Database=test");

            _repo = new MatchResultRepository(
                _loggerMock.Object,
                _configMock.Object
            );
        }

        [Fact]
        public async Task ProcessMessageAsync_WithNullRecord_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _repo.ProcessMessageAsync(null!)
            );
        }
    }
}