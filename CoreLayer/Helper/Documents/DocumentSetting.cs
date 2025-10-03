using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CoreLayer.Helper.Documents
{
    public static class DocumentSetting
    {
        public static string Upload(IFormFile file, string folderName)
        {
            string folderpath = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot\\files\\{folderName}");

            // Create directory if it doesn't exist
            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }

            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(folderpath, fileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            file.CopyTo(fileStream);

            return fileName;
        }

        public static void Delete(string fileName, string folderName)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/files/{folderName}", fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - this prevents the 500 error
                Console.WriteLine($"Could not delete file {fileName}: {ex.Message}");
            }
        }

        public static string GetFileUrl(string fileName, string folderName, string baseUrl)
        {
            return $"{baseUrl}/files/{folderName}/{fileName}";
        }
    }
}
