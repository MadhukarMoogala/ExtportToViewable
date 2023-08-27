using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAClient
{
    internal class Constants
    {
        public const string ACTIVITY = "exportxdata";
        public const string OWNER = "acadlmv";
        public const string LABEL = "dev";
        public const string BUNDLENAME = "lmvexporter";
        public const string TARGETENGINE = "Autodesk.AutoCAD+24_3";
        public const string SCOPES = "data:write data:read bucket:read bucket:update bucket:create bucket:delete code:all";
        public const string BUCKET_KEY = "svfoutputs25082023";
        public const string WEBSOCKET_URL = "wss://websockets.forgedesignautomation.io";
    }
    public static class VisualStudioProvider
    {
        public static DirectoryInfo? TryGetSolutionDirectoryInfo(string? currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }
    }
}
