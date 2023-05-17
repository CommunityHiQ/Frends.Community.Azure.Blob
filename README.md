# Frends.Community.Azure.Blob

Frends Community Task for Azure Blob related operations.

Task operations that use Azure DataMovement library for managing blobs.
https://github.com/Azure/azure-storage-net-data-movement

[![Actions Status](https://github.com/CommunityHiQ/Frends.Community.Azure.Blob/workflows/PackAndPushAfterMerge/badge.svg)](https://github.com/CommunityHiQ/Frends.Community.Azure.Blob/actions) ![MyGet](https://img.shields.io/myget/frends-community/v/Frends.Community.Azure.Blob) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

- [Installing](#installing)
- [Tasks](#tasks)
     - [UploadFileAsync](#uploadfileasync)
	 - [ListBlobs](#listblobs)
     - [DownloadBlobAsync](#downloadblobasync)
     - [ReadBlobContentAsync](#readblobcontentasync)
     - [DeleteBlobAsync](#deleteblobasync)
     - [DeleteContainerAsync](#deletecontainerasync)
- [Building](#building)
- [Contributing](#contributing)
- [Change Log](#change-log)

# Installing

You can install the task via Frends UI Task View or you can find the NuGet package from the following NuGet feed https://www.myget.org/F/frends-community/api/v3/index.json and in Gallery view in MyGet https://www.myget.org/feed/frends-community/package/nuget/Frends.Community.Azure.Blob

# Tasks

## UploadFileAsync
Uploads file to a target container. If the container doesn't exist, it will be created before the upload operation.\
**This feature does not work with .netStandard 2.0**

### Task parameters

### Input

| Property      | Type     | Description                                                                              | Example                |
|---------------|----------|------------------------------------------------------------------------------------------|------------------------|
| Source File   | `string` | Full path to file that is uploaded.                                                      | 'c:\temp\uploadMe.xml' |
| Contents Only | `bool`   | Reads file content as string and treats content as selected Encoding.                    | true                   |
| Compress      | `bool`   | Applies gzip compression to file or file content.                                        | true                   |
| Tags          | Tag[]    | Index tags for blob. Should be set to null if the storage account does not support tags. | See [Tag](#tag)        |

### Destination properties

| Property                              | Type                                | Description                                                                                    | Example                      |
|---------------------------------------|-------------------------------------|------------------------------------------------------------------------------------------------|------------------------------|
| Connection method                     | emun: ConnectionString, AccessToken | Method used for authentication.                                                                | ConnectionString             |
| Connection String                     | `string`                            | Connection string to Azure storage. Used if Connection method is connection string.            | 'UseDevelopmentStorage=true' |
| Application ID                        | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.      | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID                             | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                          | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret                         | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token. | 'somesecretclientsecret123'  |
| Storage account name                  | `string`                            | Name of the storage account. Used if connection method is access token.                        | 'myteststorageaccount'       |
| Container Name                        | `string`                            | Name of the azure blob storage container where the data will be uploaded. If the container doesn't exist, then it will be created. See [Naming and Referencing Containers](https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata) for naming conventions. | 'my-container' |
| Create container if it does not exist | bool                                | Tries to create the container if it does not exist.                                            | false                        |
| Blob Type                             | enum: Append, Block or Page         | Azure blob type to upload.                                                                     | Block                        |
| Rename To                             | `string`                            | If value is set, uploaded file will be renamed to this.                                        | 'newFileName.xml'            |
| Overwrite                             | `bool`                              | Should upload operation overwrite existing file with same name.                                | true                         |
| ParallelOperations                    | `int`                               | The number of the concurrent operations.                                                       | 64                           |
| Content-Type                          | `string`                            | Forces any content-type to file. If empty, tries to guess based on extension and MIME-type.    | text/xml                     |
| Content-Encoding                      | `string`                            | File content is treated as this. Does not affect file encoding when Contents Only is true. If compression is enabled, Content-Type is set as 'gzip'. | utf8 |

### Tag

| Property | Type     | Description             | Example  |
|----------|----------|-------------------------|----------|
| Name     | `string` | Name of the index tag.  | TagName  |
| Value    | `string` | Value of the index tag. | TagValue |

### Returns

Task returns an object with following properties

| Property   | Type     | Description                 | Example                                                |
|------------|----------|-----------------------------|--------------------------------------------------------|
| SourceFile | `string` | Full path of file uploaded. | '/container/file.txt'                                  |
| Uri        | `string` | Uri to uploaded blob.       | 'https://storage.blob.core.windows.net/container/file.txt' |

## ListBlobs
List blobs in a container.

### Task parameters

### Source

| Property             | Type                                | Description                                                                                                       | Example                      |
|----------------------|-------------------------------------|-------------------------------------------------------------------------------------------------------------------|------------------------------|
| Connection method    | emun: ConnectionString, AccessToken | Method used for authentication.                                                                                   | ConnectionString             |
| Connection String    | `string`                            | Connection string to Azure storage. Used if connection method is connection string.                               | 'UseDevelopmentStorage=true' |
| Application ID       | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.                         | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID            | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                                             | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret        | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token.                    | 'somesecretclientsecret123'  |
| Storage account name | `string`                            | Name of the storage account. Used if connection method is access token.                                           | 'myteststorageaccount'       |
| Container Name       | `string`                            | Name of the azure blob storage container from where the data will be downloaded.                                  | 'my-container'               |
| Flat blob listing    | `bool`                              | Specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory. | true                         |
| Prefix               | `string`                            | Blob prefix used while searching container.                                                                       | 'test-'                      |

### Returns

Result is a list of object with following properties

| Property | Type     | Description                                             | Example                                                    |
|----------|----------|---------------------------------------------------------|------------------------------------------------------------|
| Name     | `string` | Blob Name. With Directories, this is empty.             | 'file.txt'                                                 |
| Uri      | `string` | Blob Uri.                                               | 'https://storage.blob.core.windows.net/container/file.txt' |
| BlobType | `string` | Type of the blob. Either 'Block','Page' or 'Directory'. | 'Block'                                                    |
| ETag     | `string` | Value that is updated everytime blob is updated.        | '0x8FOLS20E0096123E'                                       |


## DownloadBlobAsync
Downloads a blob to a file.

### Task parameters

### Source

| Property             | Type                                | Description                                                                                    | Example                      |
|----------------------|-------------------------------------|------------------------------------------------------------------------------------------------|------------------------------|
| Connection method    | emun: ConnectionString, AccessToken | Method used for authentication.                                                                | ConnectionString             |
| Connection String    | `string`                            | Connection string to Azure storage. Used if connection method is connection string.            | 'UseDevelopmentStorage=true' |
| Container Name       | `string`                            | Name of the azure blob storage container from where the data will be downloaded.               | 'my-container'               |
| Application ID       | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.      | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID            | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                          | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret        | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token. | 'somesecretclientsecret123'  |
| Storage account name | `string`                            | Name of the storage account. Used if connection method is access token.                        | 'myteststorageaccount'       |
| Blob Name            | `string`                            | Name of the blob to be downloaded.                                                             | 'donwloadMe.xml'             |
| Blob Type            | enum: Append, Block or Page         | Azure blob type to download.                                                                   | Block                        |
| Encoding             | `string`                            | Encoding of the blob.                                                                          | 'utf-8'                      |

### Destination

| Property               | Type                           | Description                                                      | Example        |
|------------------------|--------------------------------|------------------------------------------------------------------|----------------|
| Directory              | `string`                       | Download destination directory.                                  | 'c:\downloads' |
| FileExistsOperation    | enum: Error, Rename, Overwrite | What should be done if destination file exists?                  | Error          |
| ParseIllegalCharacters | bool                           | If Blob name contains illegal characters, should they be parsed? | false          |

### Returns

Task returns an object with following properties

| Property         | Type     | Description                   | Example           |
|------------------|----------|-------------------------------|-------------------|
| FileName         | `string` | Downloaded file name.         | 'file.txt'        |
| Directory        | `string` | Download directory.           | 'c:\tmp'          |
| FullPath         | `string` | Full path to downloaded file. | 'c:\tmp\file.txt' |
| OriginalFileName | `string` | Original name of the Blob.    | `file.txt`        |

## ReadBlobContentAsync

Reads contents of a blob.

### Task parameters

### Source

| Property             | Type                                | Description                                                                                    | Example                      |
|----------------------|-------------------------------------|------------------------------------------------------------------------------------------------|------------------------------|
| Connection method    | emun: ConnectionString, AccessToken | Method used for authentication.                                                                | ConnectionString             |
| Connection String    | `string`                            | Connection string to Azure storage. Used if connection method is connection string.            | 'UseDevelopmentStorage=true' |
| Application ID       | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.      | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID            | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                          | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret        | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token. | 'somesecretclientsecret123'  |
| Storage account name | `string`                            | Name of the storage account. Used if connection method is access token.                        | 'myteststorageaccount'       |
| Container Name       | `string`                            | Name of the azure blob storage container from where blob data is located.                      | 'my-container'               |
| Blob Name            | `string`                            | Name of the blob which content is read.                                                        | 'donwloadMe.xml'             |
| Blob Type            | enum: Append, Block or Page         | Azure blob type to read.                                                                       | Block                        |
| Encoding             | `string`                            | Encoding name in which blob content is read.                                                   | 'UTF-8'                      |

### Returns

Task returns an object with following properties

| Property | Type     | Description   | Example                        |
|----------|----------|---------------|--------------------------------|
| Content  | `string` | Blob content. | 'This content is from a blob.' |

## DeleteBlobAsync

Deletes a blob from a target container. Operation result is seen as succesful even if the blob or the container doesn't exist.

### Task parameters

### Target

| Property                  | Type                                                | Description                                     | Example        |
|---------------------------|-----------------------------------------------------|-------------------------------------------------|----------------|
| Blob Name                 | `string`                                            | Name of the blob to delete.                     | 'deleteMe.xml' |
| Verify ETag when deleting | `string`                                            | Delete blob only if the ETag matches. Leave empty if verification is not needed. Used for concurrency. | 0x9FE13BAA3234312 |
| Snapshot delete option    | enum: None, IncludeSnapshots or DeleteSnapshotsOnly | Defines what should be done with blob snapshots | None           |

### Connection properties

| Property             | Type                                | Description                                                                                    | Example                      |
|----------------------|-------------------------------------|------------------------------------------------------------------------------------------------|------------------------------|
| Connection method    | emun: ConnectionString, AccessToken | Method used for authentication.                                                                | ConnectionString             |
| Connection String    | `string`                            | Connection string to Azure storage. Used if connection method is connection string.            | 'UseDevelopmentStorage=true' |
| Application ID       | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.      | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID            | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                          | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret        | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token. | 'somesecretclientsecret123'  |
| Storage account name | `string`                            | Name of the storage account. Used if connection method is access token.                        | 'myteststorageaccount'       |
| Container Name       | `string`                            | Name of the container where delete blob exists.                                                | 'my-container'               |

### Returns

Task returns an object with following properties

| Property | Type   | Description                                           | Example |
|----------|--------|-------------------------------------------------------|---------|
| Success  | `bool` | Indicates whether the operation was succesful or not. | true    |

## DeleteContainerAsync
Deletes a whole container from blob storage.

### Task parameters

### Target

| Property       | Type     | Description                      | Example        |
|----------------|----------|----------------------------------|----------------|
| Container Name | `string` | Name of the container to delete. | 'my-container' |

### Connection properties

| Property             | Type                                | Description                                                                                    | Example                      |
|----------------------|-------------------------------------|------------------------------------------------------------------------------------------------|------------------------------|
| Connection method    | emun: ConnectionString, AccessToken | Method used for authentication.                                                                | ConnectionString             |
| Connection String    | `string`                            | Connection string to Azure storage. Used if connection method is connection string.            | 'UseDevelopmentStorage=true' |
| Storage account name | `string`                            | Name of the storage account. Used if connection method is access token.                        | 'myteststorageaccount'       |
| Application ID       | `string`                            | Application (Client) ID for Azure application. Used if connection method is access token.      | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Tenant ID            | `string`                            | Tenant ID of Azure Tenant. Used if connection method is access token.                          | XXXXXX-XXXX-XXXX-XXXX-XXXXXX |
| Client Secret        | `string` (secret)                   | Client Secret for Azure application authentication. Used if connection method is access token. | 'somesecretclientsecret123'  |

### Returns: 

| Property | Type   | Description                                           | Example |
|----------|--------|-------------------------------------------------------|---------|
| Success  | `bool` | Indicates whether the operation was succesful or not. | true    |

# Building

Clone a copy of the repo.

`git clone https://github.com/CommunityHiQ/Frends.Community.Azure.Blob.git`

Build the project.

`dotnet build`

Run Tests.

`dotnet test`

Create a NuGet package.

`dotnet pack --configuration Release`

# Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

# Change Log

| Version | Changes                                                                                                                                           |
|---------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| 1.2.0   | Wrote documentation according to development quide lines. Added DownloadBlobAsync, ReadBlobContentAsync and ListBlobs tasks.                      |
| 1.3.0   | New parameters in multiple tasks. New return value in list task. Tasks now use System.ComponentModel.DataAnnotations.                             |
| 1.4.0   | Updated dependencies due potential security vulnerabilities.                                                                                      |
| 1.5.0   | File upload now uses stream. Added options to compress or read file as string with Contents Only. Added Content-Type and Content-Encoding fields. |
| 1.6.0   | Added encoding option to ReadBlobContentAsync task.                                                                                               |
| 2.0.0   | Added support for both .netStandard 2.0 and net471.                                                                                               |
| 3.0.0   | Added support for access token authentication to all tasks. Provider is client credentials provider.                                              |
| 3.0.1   | Added Azure.Core as dependency.                                                                                                                   |
| 3.0.2   | Added Microsoft.Identity.Client as dependency. Changed ConnectionString to secret to all tasks.                                                   |
| 3.1.0   | UploadFileAsync: New feature to add index tags to uploaded blobs.                                                                                 |
| 3.2.0   | DownloadBlobAsync: Added option to parse illegal characters when Blob is downloaded. Original Blob name added to result object.                   |
| 3.2.1   | DownloadBlobAsync: Fixed issue with empty encoding parameter.                                                                                     |
| 3.2.2   | UploadFileAsync: Fixed issue with tags in hierarchical storage accounts.                                                                          |
| 3.2.3   | UploadFileAsync: Another fix for tags in hierarchical storage accounts. Tags are passed as null if no Tags are provided in parameter editor.      |
