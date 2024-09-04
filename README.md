![AzureStorageWrapper poster](https://raw.githubusercontent.com/sergiobarriel/AzureStorageWrapper/main/images/poster.png)

# AzureStorageWrapper

**AzureStorageWrapper** it's a wrapper for Azure Storage **blob** service, aimed at simplifying the file upload process and obtaining links for downloading them.

[![run tests](https://github.com/sergiobarriel/AzureStorageWrapper/actions/workflows/run-tests.yml/badge.svg?branch=dev)](https://github.com/sergiobarriel/AzureStorageWrapper/actions/workflows/run-tests.yml) 
[![run tests and deploy](https://github.com/sergiobarriel/AzureStorageWrapper/actions/workflows/run-tests-and-deploy.yml/badge.svg?branch=main)](https://github.com/sergiobarriel/AzureStorageWrapper/actions/workflows/run-tests-and-deploy.yml)
![downloads](https://img.shields.io/nuget/dt/AzureStorageWrapper)

📦 View package on [NuGet Gallery](https://www.nuget.org/packages/AzureStorageWrapper/)
📦 View package on [nuget.info](https://nuget.info/packages/AzureStorageWrapper)

# Usage

## Dependency injection

To add **AzureStorageWrapper** to dependencies container, just use the method `AddAzureStorageWrapper(...)`

```csharp
public void ConfigureServices(IServiceCollection serviceCollection)
{
    serviceCollection.AddAzureStorageWrapper(configuration =>
    {
        configuration.ConnectionString = "azure-storage-connection-string"
        configuration.MaxSasUriExpiration = 600;
        configuration.DefaultSasUriExpiration = 300;
        configuration.CreateContainerIfNotExists = true;
    });
}
```

These are the *main* properties:
- **ConnectionString**: The connection string of your Azure Storage Account. You can export by following [this document](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-keys-manage?tabs=azure-portal#view-account-access-keys)
- **MaxSasUriExpiration**: You can set a maximum duration value for the Shared Access Signature (SAS) of an Azure Storage file to prevent someone from attempting to generate a token with a longer expiration time. Expressed in seconds.
- **DefaultSasUriExpiration**: You can download a file using AzureStorageWrapper without specifying the `ExpiresIn` property. By doing so, this value will be automatically set. Expressed in seconds
- **CreateContainerIfNotExists**: When uploading a file to Azure Storage, you need to specify the container, which may not exist and can be created automatically. You can set it to `true` or `false` based on your requirements. Please consider this property if you have automated your infrastructure with any Infrastructure as Code (IaC) mechanism because it affects the state of your infrastructure.

Then you can inject `IAzureStorageWrapper` into your services through constructor:

```csharp
public class MyService
{
    private IAzureStorageWrapper _storageWrapper;

    public MyService(IAzureStorageWrapper storageWrapper)
    {
        _storageWrapper = storageWrapper;
    }
}
```

## Upload blobs

There are **3 options** to upload blobs, all the ways follow the same pattern:

- You need to specify the *file name* and *extension*.
- You need to specify the *container* where you want to store the file.
- You can add additional *metadata* with relevant information.

The file will be placed in *Base64*, *Bytes* or *Stream* property.

### Base64

```csharp
var base64 = "SGVsbG8g8J+Zgg==";

var command = new UploadBase64()
{
    Base64 = base64,
    Container = "files",
    Name = "hello",
    Extension = "md",
    Metadata = new Dictionary<string, string>() { {"key", "value"} }
};

var response = await _azureStorageWrapper.UploadBlobAsync(command);
```

### Byte []

```csharp
var bytes = Convert.FromBase64String("SGVsbG8g8J+Zgg==");

var command = new UploadBytes()
{
    Bytes = bytes,
    Container = "files",
    Name = "hello",
    Extension = "md",
    Metadata = new Dictionary<string, string>() { {"key", "value"} }
};

var response = await _azureStorageWrapper.UploadBlobAsync(command);
```

### Stream

```csharp
var stream = new MemoryStream(Convert.FromBase64String("SGVsbG8g8J+Zgg=="));

var command = new UploadStream()
{
    Stream = stream,
    Container = "files",
    Name = "hello",
    Extension = "md",
    Metadata = new Dictionary<string, string>() { {"key", "value"} }
};

var response = await _azureStorageWrapper.UploadBlobAsync(command);
```

## Response after upload blobs

Regardless of the chosen upload mechanism, you will always receive this response after upload a file.

```csharp
public class BlobReference
{
    public string Container { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public string Uri { get; set; }
    public string SasUri { get; set; }
    public DateTime SasExpires { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
}
```

In example, if you upload the file `hello.md` file to container `files` you will receive a response like:

```json
{
    "Container": "files",
    "Name": "hello",
    "Extension": "md",
    "Uri": "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md",
    "SasUri": "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md?sv=2021-10-04\u0026se=2023-09-03T16%3A17%3A02Z\u0026sr=b\u0026sp=r\u0026sig=8hs8AzxABevSTc5y%2BhOWDDN%2FH5qFSpA8Omj4uqoxzms%3D",
    "SasExpires": "2023-09-03T16:17:02.8220993Z",
    "Metadata": {
        "key": "value",
        "_timestamp": "03/09/2023 16:11:02"
    }
}
```

It is your responsibility to save the reference (URI property) of the file you have uploaded to Azure Storage somewhere, as you will need it for later downloads.

## Virtual Folders

The upload commands have a property called `UseVirtualFolder` which by default has a value of `true` but you can set it to `false` if you wish.

**Be careful.** If you make that change, the files will NOT be saved in virtual directories, and file names may collide, causing files to be overwritten.

In this case, you must be responsible for establishing your own mechanism to generate unique file names.

```csharp
var base64 = "SGVsbG8g8J+Zgg==";

var command = new UploadBase64()
{
    Base64 = base64,
    Container = "files",
    Name = "hello",
    Extension = "md",
    UseVirtualFolder = false // be careful!
};

var response = await _azureStorageWrapper.UploadBlobAsync(command);
```

## Download blobs

To download a blob reference, you need specify the *Uri*.

```csharp
var command = new DownloadBlobReference()
{
    Uri = "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md"
    ExpiresIn = 60,
};

var response = await _azureStorageWrapper.DownloadBlobReferenceAsync(command);
```

The response when *downloading* file reference resembles the response when *uploading* files:

```json
{
    "Container": "files",
    "Name": "hello",
    "Extension": "md",
    "Uri": "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md",
    "SasUri": "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md?sv=2021-10-04\u0026se=2023-09-03T16%3A17%3A02Z\u0026sr=b\u0026sp=r\u0026sig=8hs8AzxABevSTc5y%2BhOWDDN%2FH5qFSpA8Omj4uqoxzms%3D",
    "SasExpires": "2023-09-03T16:17:02.8220993Z",
    "Metadata": {
        "_timestamp": "03/09/2023 16:11:02"
    }
}
```

## Delete blobs

You can delete a blob by specifying the *Uri*.

```csharp
var command = new DeleteBlob()
{
    Uri = "https://accountName.blob.core.windows.net/files/5a19306fc5014a4/hello.md"
};

await _azureStorageWrapper.DeleteBlobAsync(command);
```

## Enumerate blobs

You can list all blobs in a container by using the method `EnumerateAllBlobsAsync` or paginate the results by using `EnumerateBlobsAsync` method. The second one requires the `Size` property to be set. 

In both cases, the response `BlobReferenceCollection` will contain a collection of `BlobReference` elements.

### Without pagination

```csharp
var command = new EnumerateAllBlobs()
{
    Container = "files"
};

var response = await _azureStorageWrapper.EnumerateAllBlobsAsync(command);


```
### With pagination

```csharp
var command = new EnumerateBlobs()
{
    Container = "files"
    Size = 10,
};

var response = await _azureStorageWrapper.EnumerateBlobsAsync(command);

```
Then you can request additional pages by using the `ContinuationToken` property in the next request.

```csharp
var firstCommand = new EnumerateBlobs()
{
    Container = "files"
    Size = 10,
};

var firstResponse = await _azureStorageWrapper.EnumerateBlobsAsync(firstCommand);

var secondCommand = new EnumerateBlobs()
{
    Container = "files"
    Size = 10,
    ContinuationToken = firstResponse.ContinuationToken
};

var secondResponse = await _azureStorageWrapper.EnumerateBlobsAsync(secondCommand);
```
# Branch strategy

```mermaid
sequenceDiagram
    autonumber
    main branch->>+feature branch: new branch
    feature branch->>+feature branch: commit
    feature branch->>+feature branch: push
    feature branch-->>Github Actions: build & test
    note over Github Actions: build-test.yml
    feature branch->>+main branch: PR
    main branch-->>Github Actions: build, test & deploy
    note over Github Actions: build-test-deploy.yml
```

# Contributors / Collaborators

These individuals have contributed to the repository through suggestions, error corrections, or by opening issues. Thanks 😊

- [ginodcs](https://github.com/ginodcs)
- [christian-cell](https://github.com/christian-cell)

# Sponsor

If you like the project, you can consider making a donation at [ko-fi.com](https://ko-fi.com/sergiobarriel)

# Support

You can contact me via Twitter [@sergiobarriel](https://twitter.com/sergiobarriel), or if you have an [issue](https://github.com/sergiobarriel/AzureStorageWrapper/issues), you can open one 🙂
