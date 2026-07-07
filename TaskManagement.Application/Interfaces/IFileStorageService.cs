using System.IO;
using System.Threading.Tasks;

namespace TaskManagement.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName);
        Task<Stream> GetFileAsync(string storageKey);
        Task DeleteFileAsync(string storageKey);
    }
}
