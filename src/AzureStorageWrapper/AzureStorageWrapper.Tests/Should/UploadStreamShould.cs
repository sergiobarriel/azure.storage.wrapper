﻿using AzureStorageWrapper;
using AzureStorageWrapper.Commands;
using Xunit;

namespace AzureStorageWrapper.Tests.Should
{
    public class UploadStreamShould : BaseShould
    {
        private readonly IAzureStorageWrapper _azureStorageWrapper;

        public UploadStreamShould(IAzureStorageWrapper azureStorageWrapper)
        {
            _azureStorageWrapper = azureStorageWrapper;
        }

        [Fact]
        public async Task UploadEmptyStream_ShouldThrowException()
        {
            var stream = new MemoryStream(Convert.FromBase64String(string.Empty));

            var command = new UploadStream()
            {
                Stream = stream,
                Container = "tests",
                Name = "hello",
                Extension = "md",
                Metadata = new Dictionary<string, string>()
                    {{"tests", "tests"}}
            };

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                _ = await _azureStorageWrapper.UploadBlobAsync(command);
            });
        }

        [Theory]
        [MemberData(nameof(WrongFileProperties))]
        public async Task UploadStreamFile_WithWrongFileProperties_ShouldThrowException(string container, string fileName, string fileExtension)
        {
            var stream = new MemoryStream(Convert.FromBase64String("SGVsbG8gd29ybGQh"));

            var command = new UploadStream()
            {
                Stream = stream,
                Container = container,
                Name = fileName,
                Extension = fileExtension,
                Metadata = new Dictionary<string, string>()
                    {{"tests", "tests"}}
            };

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                _ = await _azureStorageWrapper.UploadBlobAsync(command);
            });
        }

        [Theory]
        [MemberData(nameof(WrongMetadata))]
        public async Task UploadStreamFile_WithWrongMetadata_ShouldUploadFile(Dictionary<string, string> properties)
        {
            var stream = new MemoryStream(Convert.FromBase64String("SGVsbG8gd29ybGQh"));

            var command = new UploadStream()
            {
                Stream = stream,
                Container = "tests",
                Name = "hello",
                Extension = "md",
                Metadata = properties
            };

            var response = await _azureStorageWrapper.UploadBlobAsync(command);

            Assert.NotNull(response);

            Assert.True(await PingAsync(response.SasUri));
        }

        [Fact]
        public async Task UploadStream_ShouldUploadFile()
        {
            var stream = new MemoryStream(Convert.FromBase64String("SGVsbG8gd29ybGQh"));

            var response = await _azureStorageWrapper.UploadBlobAsync(new UploadStream()
            {
                Stream = stream,
                Container = "tests",
                Name = "hello",
                Extension = "md",
                Metadata = new Dictionary<string, string>()
                    {{"tests", "tests"}}
            });

            Assert.NotNull(response);

            Assert.True(await PingAsync(response.SasUri));
        }
    }
}