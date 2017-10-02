# Frends.Community.Azure.Blob
Azure Blob related tasks

Task operations that use Azure DataMovement library for managing blobs.
https://github.com/Azure/azure-storage-net-data-movement

## Operations
- Upload operations
  - UploadFileAsync
- Delete operations
  - DeleteBlobAsync
  - DeleteContainerAsync

### UploadFileAsync
Uploads file to target container. If the container doesn't exist, it will be created before the upload operation.

Returns: bool indicating whether the operation was succesful or not

### DeleteBlobAsync
Deletes a blob from target container. Operation result is seen as succesful even if the blob or container doesn't exist.

Returns: bool indicating whether the operation was succesful or not

### DeleteContainerAsync
Deletes a whole container from blob storage.

Returns: bool indicating whether the operation was succesful or not
