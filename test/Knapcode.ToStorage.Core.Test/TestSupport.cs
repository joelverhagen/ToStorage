using System;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ToStorage.Core.Test
{
    public static class TestSupport
    {
        public const string ConnectionString = "UseDevelopmentStorage=true";

        public static string GetTestContainer()
        {
            return "testcontainer" + Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        public static void DeleteContainer(string container)
        {
            var context = new CloudContext(ConnectionString, container);
            context.BlobContainer.DeleteIfExistsAsync().Wait();
        }
    }
}
