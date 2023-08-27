using Autodesk.Forge;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Forge.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAClient
{
    internal class OSSHandler
    {
               
        static void OnUploadProgress(float increment, TimeSpan elapsed, List<UploadItemDesc> objects)
        {
            var str = String.Format("Uploading: {0:P2} Elapsed: {1:c}", increment,elapsed);
            Console.Write($"\r{str}");
        }

        public static async Task<string?> GetObjectIdAsync(string token, string objectKey, string fileSavePath)
        {
            try
            {
                if(!File.Exists(fileSavePath))
                {
                    Console.Error.WriteLine($"File Not Found {fileSavePath}"); 
                    return null;
                }
                BucketsApi buckets = new BucketsApi();
                buckets.Configuration.AccessToken = token;
                try
                {
                    PostBucketsPayload bucketPayload = new PostBucketsPayload(Constants.BUCKET_KEY, null, PostBucketsPayload.PolicyKeyEnum.Transient);
                    await buckets.CreateBucketAsync(bucketPayload, "US");
                }
                catch { }; // in case bucket already exists
                ObjectsApi objectsApi = new ObjectsApi();
                objectsApi.Configuration.AccessToken = token;               
                List<UploadItemDesc> uploadRes = await objectsApi.uploadResources(Constants.BUCKET_KEY,
                    new List<UploadItemDesc> {
                        new UploadItemDesc(objectKey, await System.IO.File.ReadAllBytesAsync(fileSavePath))
                    },
                    null,
                    OnUploadProgress,
                    null);                
                DynamicDictionary objValues = uploadRes[0].completed;
                objValues.Dictionary.TryGetValue("objectId", out var id);
                Console.WriteLine("\r\nDone!");
                return id?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when preparing input url:{ex.Message}");
                throw;
            }

        }
    }
}
