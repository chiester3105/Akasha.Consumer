using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akasha.Consumer.Services;
using Akasha.Consumer.Workers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Akasha.Consumer.Tests
{
    public class KafkaConsumerWorkerTests
    {
        [Fact]
        public async Task ProcessMessageAsync_CallsRepo_WhenMessageRecieved()
        {
            

        }
    }
}

