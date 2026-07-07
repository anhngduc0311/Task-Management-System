using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _uploadFolder;

        public LocalFileStorageService(IConfiguration configuration)
        {
            var uploadPath = configuration["FileStorage:UploadPath"] ?? "Uploads";
            
            // If the configured path is not absolute, make it relative to the current directory
            if (!Path.IsPathRooted(uploadPath))
            {
                _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
            }
            else
            {
                _uploadFolder = uploadPath;
            }

            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_uploadFolder, uniqueFileName);

            using (var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            return uniqueFileName; // This will be stored as StorageKey in database
        }

        public Task<Stream> GetFileAsync(string storageKey)
        {
            var filePath = Path.Combine(_uploadFolder, storageKey);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found in local storage.", storageKey);
            }

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteFileAsync(string storageKey)
        {
            var filePath = Path.Combine(_uploadFolder, storageKey);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
