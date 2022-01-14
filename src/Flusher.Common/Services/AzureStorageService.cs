using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CommonHelpers.Common;
using Flusher.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flusher.Common.Services
{
    public class AzureStorageService : BindableBase
    {
        private readonly BlobServiceClient service;
        private BlobContainerClient container;
        private bool isInitialized;

        /// <summary>
        /// Creates an instance of the Azure Storage helper class.
        /// </summary>
        public AzureStorageService()
        {
            if (string.IsNullOrEmpty(Secrets.BlobConnectionString))
            {
                throw new Exception("MISSING SECRETS - the Flusher.Common/Helpers/Secrets.cs class is missing values for your services");
            }

            service = new BlobServiceClient(Secrets.BlobConnectionString);
        }

        /// <summary>
        /// Informs consumers and internal methods if the service is ready for use.
        /// </summary>
        public bool IsInitialized
        {
            get => isInitialized;
            set => SetProperty(ref isInitialized, value);
        }

        /// <summary>
        /// The time-frame to keep images in the blob container (default value is 30 days).
        /// </summary>
        public TimeSpan ImageFileLifeSpan { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Initializes the service and ensures blob container exists. Must be called before using any API endpoints.
        /// </summary>
        /// <returns>Task</returns>
        public async Task InitializeAsync()
        {
            try
            {
                container = service.GetBlobContainerClient(Secrets.BlobContainerName);
                await container.CreateIfNotExistsAsync();

                IsInitialized = true;
            }
            catch(Exception ex)
            {
                IsInitialized = false;
                Trace.WriteLine($"AzureStorageService InitializeAsync: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Uploads a photo to Azure Storage Blob container.
        /// </summary>
        /// <param name="fileNameWithExtension">The name of the file, with file extension specified (e.g. 'photo.jpg').</param>
        /// <param name="filePath">The path to the file on the local file system.</param>
        /// <returns>Primary uri to the file.</returns>
        public async Task<Uri> UploadPhotoAsync(string fileNameWithExtension, string filePath)
        {
            if (!IsInitialized)
                return null;

            var blob = container.GetBlobClient(fileNameWithExtension);

            await blob.UploadAsync(filePath);

            await CleanContainerAsync().ConfigureAwait(false);

            return blob.Uri;
        }

        /// <summary>
        /// Uploads a photo to Azure Storage Blob container.
        /// </summary>
        /// <param name="fileNameWithExtension">The name of the file, with file extension specified (e.g. 'photo.jpg').</param>
        /// <param name="imgBytes">The byte array of the image after it has been encoded.</param>
        /// <returns>Primary uri to the file.</returns>
        public async Task<Uri> UploadPhotoAsync(string fileNameWithExtension, byte[] imgBytes)
        {
            if (!IsInitialized)
                return null;

            using (var memStream = new MemoryStream(imgBytes))
            {
                var blob = container.GetBlobClient(fileNameWithExtension);

                await blob.UploadAsync(memStream);

                await CleanContainerAsync().ConfigureAwait(false);

                return blob.Uri;
            }
        }

        /// <summary>
        /// Uploads a photo to Azure Storage Blob container.
        /// </summary>
        /// <param name="fileNameWithExtension">The name of the file, with file extension specified (e.g. 'photo.jpg').</param>
        /// <param name="imageStream">The stream of the image after it has been encoded.</param>
        /// <returns>Primary uri to the file.</returns>
        public async Task<Uri> UploadPhotoAsync(string fileNameWithExtension, Stream imageStream)
        {
            if (!IsInitialized)
                return null;

            var blob = container.GetBlobClient(fileNameWithExtension);

            await blob.UploadAsync(imageStream);

            await CleanContainerAsync().ConfigureAwait(false);

            return blob.Uri;
        }

        /// <summary>
        /// Deletes blobs (image files) from the blob container (directory) that are older than a specified time span (The default is 30 days).
        /// </summary>
        public async Task CleanContainerAsync()
        {
            if (!IsInitialized)
                return;

            try
            {
                var blobsToDelete = new List<BlobItem>();

                await foreach (BlobItem blob in container.GetBlobsAsync())
                {
                    if (DateTime.Now - blob.Properties.LastModified > ImageFileLifeSpan)
                    {
                        blobsToDelete.Add(blob);
                    }
                }

                if (blobsToDelete.Any())
                {
                    foreach (var outdatedBlob in blobsToDelete)
                    {
                        await container.DeleteBlobIfExistsAsync(outdatedBlob.Name, DeleteSnapshotsOption.IncludeSnapshots);
                    }

                    Console.WriteLine($"AzureStorageService - Deleted {blobsToDelete.Count} expired blobs.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                Trace.WriteLine($"AzureStorageService CleanContainerAsync: {ex}");
            }
        }
    }
}
