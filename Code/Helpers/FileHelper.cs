using System;
using System.IO;
using System.Web;
using System.Configuration;

namespace CRMP.Helpers
{
    public static class FileHelper
    {
        private static readonly long _maxBytes =
            long.Parse(ConfigurationManager.AppSettings["MaxUploadSizeMB"] ?? "10") * 1024 * 1024;

        private static readonly string[] _allowed =
            (ConfigurationManager.AppSettings["AllowedExtensions"] ?? ".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg,.txt,.zip")
            .ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Saves a posted file; returns the relative path stored in DB.</summary>
        public static string SaveUploadedFile(HttpPostedFile file, int requestId)
        {
            ValidateFile(file);

            string folder = Path.Combine(GetUploadRoot(), "Requests", requestId.ToString());
            Directory.CreateDirectory(folder);

            string safeName = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + SecurityHelper.SanitizeFileName(Path.GetFileName(file.FileName));
            string fullPath = Path.Combine(folder, safeName);
            file.SaveAs(fullPath);

            // Return relative path (for DB storage)
            return "Requests/" + requestId.ToString() + "/" + safeName;
        }

        public static string GetPhysicalPath(string relativePath)
        {
            return Path.Combine(GetUploadRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static void ValidateFile(HttpPostedFile file)
        {
            if (file == null || file.ContentLength == 0)
                throw new InvalidOperationException("No file was provided.");

            if (file.ContentLength > _maxBytes)
                throw new InvalidOperationException("File exceeds maximum size of " + (_maxBytes / 1024 / 1024).ToString() + " MB.");

            string ext = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(_allowed, ext) < 0)
                throw new InvalidOperationException("File type '" + ext + "' is not permitted.");
        }

        private static string GetUploadRoot()
        {
            string root = HttpContext.Current != null
                ? HttpContext.Current.Server.MapPath(ConfigurationManager.AppSettings["UploadPath"] ?? "~/Uploads/")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
