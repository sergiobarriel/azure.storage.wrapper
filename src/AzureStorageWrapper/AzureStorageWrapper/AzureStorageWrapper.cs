﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AzureStorageWrapper.Commands;
using AzureStorageWrapper.Exceptions;
using AzureStorageWrapper.Responses;

namespace AzureStorageWrapper
{
    public class AzureStorageWrapper : AzureStorageWrapperBase, IAzureStorageWrapper
    {
        private readonly IUriService _uriService = new UriService();

        public AzureStorageWrapper(AzureStorageWrapperConfiguration configuration) : base(configuration)
        {
        }

        public async Task<BlobReference> UploadBlobAsync(UploadBlob command)
        {
            Validate(command);

            await CreateContainerIfNotExists(command.Container);
            
            var container = GetContainer(command.Container);
            
            // var fileName = SanitizeFileName(command.Name);

            var blobClient = command.UseVirtualFolder
                ? container.GetBlobClient($"{RandomString()}/{command.Name}.{command.Extension}")
                : container.GetBlobClient($"{command.Name}.{command.Extension}");

            await blobClient.UploadAsync(command.GetContent(), overwrite: true);

            command.Metadata ??= new Dictionary<string, string>();
            command.Metadata.TryAdd("_timestamp", $"{DateTime.UtcNow}");

            var sanitizedDictionary = SanitizeDictionary(command.Metadata);
            
            await blobClient.SetMetadataAsync(sanitizedDictionary);

            var sasUri = await GetSasUriAsync(new GetSasUri()
            {
                Uri = blobClient.Uri.AbsoluteUri,
                ExpiresIn = Configuration.DefaultSasUriExpiration,
            });

            var blobReference = new BlobReference()
            {
                Container = command.Container,
                Name = command.Name,
                Extension = command.Extension,
                Uri = blobClient.Uri.AbsoluteUri,
                SasUri = sasUri,
                Metadata = sanitizedDictionary,
                SasExpires = DateTime.UtcNow.AddSeconds(Configuration.DefaultSasUriExpiration)
            };

            return blobReference;
        }
        
        public async Task<BlobReference> DownloadBlobReferenceAsync(DownloadBlobReference command)
        {
            Validate(command);

            var containerName = _uriService.GetContainer(command.Uri);
            var fileName = GetFileName(command.Uri);

            var container = GetContainer(containerName);
            
            if (! await container.ExistsAsync())
                throw new AzureStorageWrapperException($"container {containerName} doesn't exists!");
            
            var blobClient = container.GetBlobClient($"{fileName.name}.{fileName.extension}");

            if (! await blobClient.ExistsAsync())
                throw new AzureStorageWrapperException($"blob {fileName.name}.{fileName.extension} doesn't exists!");

            var blobProperties = await blobClient.GetPropertiesAsync();

            return new BlobReference()
            {
                Container = blobClient.BlobContainerName,
                Name = fileName.name,
                Extension = fileName.extension,
                Uri = blobClient.Uri.AbsoluteUri,
                SasUri = await GetSasUriAsync(new GetSasUri()
                {
                    Uri = command.Uri,
                    ExpiresIn = command.ExpiresIn <= 0
                        ? Configuration.DefaultSasUriExpiration
                        : command.ExpiresIn,
                }),
                SasExpires = DateTime.MaxValue,
                Metadata = blobProperties.Value.Metadata,
            };
        }

        public async Task DeleteBlobAsync(DeleteBlob command)
        {
            Validate(command);

            var containerName = _uriService.GetContainer(command.Uri);
            var fileName = GetFileName(command.Uri);

            var container = GetContainer(containerName);

            if (!await container.ExistsAsync())
                throw new AzureStorageWrapperException($"container {containerName} doesn't exists!");

            var blobClient = container.GetBlobClient($"{fileName.name}.{fileName.extension}");

            if (!await blobClient.ExistsAsync())
                throw new AzureStorageWrapperException($"blob {fileName.name}.{fileName.extension} doesn't exists!");

            await blobClient.DeleteIfExistsAsync();
        }

        private BlobContainerClient GetContainer(string containerName)
        {
            return new BlobContainerClient(Configuration.ConnectionString, containerName);
        }

        private async Task CreateContainerIfNotExists(string containerName)
        {
            var container = GetContainer(containerName);
            
            if (!await container.ExistsAsync())
            {
                if (Configuration.CreateContainerIfNotExists)
                    await container.CreateIfNotExistsAsync();
                
                else throw new AzureStorageWrapperException($"container {containerName} doesn't exists!");
            }
        }
        
        public async Task<Blob> DownloadBlobAsync(DownloadBlob command)
        {
            using var httpClient = new HttpClient();
            
            var response = await httpClient.GetAsync(command.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var fileName = _uriService.GetFileName(command.Uri);
                var fileExtension = _uriService.GetFileExtension(command.Uri);
                
                throw new AzureStorageWrapperException($"something went wrong when downloading blob {fileName}.{fileExtension}: {response.ReasonPhrase}");
            }
            
            var stream = await response.Content.ReadAsStreamAsync();

            return new Blob()
            {
                Stream = stream
            };
        }

        private async Task<string> GetSasUriAsync(GetSasUri command)
        {
            Validate(command);

            var fileName = GetFileName(command.Uri);

            var container = GetContainer(_uriService.GetContainer(command.Uri));

            var blobClient = container.GetBlobClient($"{fileName.name}.{fileName.extension}");

            if (!await blobClient.ExistsAsync()) return null;

            var blobSasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddSeconds(command.ExpiresIn));

            return blobSasUri.AbsoluteUri;
        }
        
        private (string name, string extension) GetFileName(string uri)
        {
            var unescapedUriRepresentation = Uri.UnescapeDataString(uri);

            var container = _uriService.GetContainer(unescapedUriRepresentation);
            var host = _uriService.GetHost(unescapedUriRepresentation);
            var extension = _uriService.GetFileExtension(unescapedUriRepresentation);

            var temp = unescapedUriRepresentation
                .Replace($"{host}/", string.Empty)
                .Replace($"{container}/", string.Empty)
                .Replace($".{extension}", string.Empty);
            
            return (name: temp, extension: extension);
        }
    }
}
