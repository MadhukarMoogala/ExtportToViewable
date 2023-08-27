using Autodesk.Forge;
using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;
using Autodesk.Forge.DesignAutomation.Http;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Forge.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using static System.Formats.Asn1.AsnWriter;
using Engine = Autodesk.Forge.DesignAutomation.Model.Engine;

namespace DAClient
{


    internal class App
    {

        public DesignAutomationClient da;
        public ILogger Logger;
        public ForgeConfiguration forgeConfig;
        public App(DesignAutomationClient api, IOptions<ForgeConfiguration> config, ILogger log)
        {
            da = api;
            forgeConfig = config.Value;
            Logger = log;
            
        }

        private async Task<string?> GetTokenAsync(string scopes)
        {
           
          
            ApiResponse<Engine> resp = await da.EnginesApi.GetEngineAsync(Constants.TARGETENGINE,scopes);            
            string? bearerToken = resp.HttpResponse.RequestMessage?.Headers.Authorization?.ToString();            
            return bearerToken;
        }

        private async Task<(string input, string output)> GetOSSURLsAsync(string scopes)
        {
            var bearerToken = await GetTokenAsync(scopes);
            bearerToken = bearerToken?.Split(" ")[1];
            Logger.LogInformation("{Token}", bearerToken);
            var input = await OSSHandler.GetObjectIdAsync(bearerToken ?? String.Empty, "House3d.dwg", @"D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\DAClient\Files\House3d.dwg");
            Logger.LogInformation("{ObjectId}", input);
            var output = await OSSHandler.GetObjectIdAsync(bearerToken ?? String.Empty, "result.zip", CreateEmptyZip());
            Logger.LogInformation("{ObjectId}", output);
            return (input:input ?? "", output: output ?? "");
        }

        public async Task RunAsync()
        {
           
            try
            {
               var URLs = await GetOSSURLsAsync(Constants.SCOPES);

                //Step Check Nickname 
                if (!await SetupOwnerAsync())
                {
                    Logger.LogWarning("Exiting.");
                    return;
                }

                var myApp = await SetupAppBundleAsync();
                var myActivity = await SetupActivityAsync(myApp);

                await SubmitWorkItemAsync(myActivity,URLs);

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace);
            }
        }

        private static string CreateEmptyZip()
        {
            string zipFilePath = "result.zip";

            // Create an empty zip file
            using (FileStream zipStream = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                // No need to add any entries since the zip file is empty
            }
            return zipFilePath;
        }
        private async Task<bool> SetupOwnerAsync()
        {
            Console.WriteLine("Setting up owner...");
            var nickname = await da.GetNicknameAsync("me");
            if (nickname == forgeConfig.ClientId)
            {
                Console.WriteLine("\tNo nickname for this clientId yet. Attempting to create one...");
                HttpResponseMessage resp;
                resp = await da.ForgeAppsApi.CreateNicknameAsync("me", new NicknameRecord() { Nickname = Constants.OWNER }, throwOnError: false);
                if (resp.StatusCode == HttpStatusCode.Conflict)
                {
                    Console.WriteLine("\tThere are already resources associated with this clientId or nickname is in use. Please use a different clientId or nickname.");
                    return false;
                }
                await resp.EnsureSuccessStatusCodeAsync();
            }
            return true;
        }

        private async Task SubmitWorkItemAsync(string myActivity, ValueTuple<string,string> objectIds)
        {
            Console.WriteLine("Submitting up work item...\n");
            var workItem = new Autodesk.Forge.DesignAutomation.Model.WorkItem()
            {
                ActivityId = myActivity,
                Arguments = new Dictionary<string, IArgument>() {
                    {
                      "input",
                      new XrefTreeArgument() {
                      Url = objectIds.Item1,
                      Headers = new Dictionary<string, string>()
                      {
                        { "Authorization", await GetTokenAsync(Constants.SCOPES) ?? ""} 
                      },
                        Verb = Verb.Get
                      }
                    },
                    {
                      "output",
                      new XrefTreeArgument() {
                      Url = objectIds.Item2,
                      Headers = new Dictionary<string, string>()
                      {
                        { "Authorization", await GetTokenAsync(Constants.SCOPES) ?? ""}
                      },
                      Verb = Verb.Put
                      }
                    }
                  }
            };
            var bearerToken = await GetTokenAsync(Constants.SCOPES);     
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(Constants.WEBSOCKET_URL), CancellationToken.None);
            var msg = "{\"action\":\"post-workitem\", \"data\":" + JsonConvert.SerializeObject(workItem) + ", \"headers\": {\"Authorization\":\"" + bearerToken + "\"}}";
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result= await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                try
                {
                    var wsRes = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var json = JObject.Parse(wsRes);
                    switch (json?["action"]?.ToString())
                    {
                        case "progress":
                            Console.WriteLine($"Progress data:{json["data"]}");
                            break;
                        case "status":
                            var workItemStatus = JsonConvert.DeserializeObject<WorkItemStatus>(json?["data"]?.ToString() ?? string.Empty);
                            Console.WriteLine($"Status: {workItemStatus?.Status}.");
                            if (workItemStatus?.Status != Status.Pending && workItemStatus?.Status != Status.Inprogress)
                            {
                              
                                //The minimum size of a .ZIP file is 22 bytes. Such an empty zip file contains only an End of Central Directory Record (EOCD):
                                bool isOk = workItemStatus?.Status == Status.Success && workItemStatus.Stats.BytesUploaded > 22;

                                Console.WriteLine($"\n{workItemStatus?.Status}.");

                                var log = isOk ? $"Ok_{workItemStatus?.Id}.txt" : $"Err_{workItemStatus?.Id}.txt";


                                if (workItemStatus?.Status == Status.Success)
                                {
                                                                       
                                    var objectsApi = new ObjectsApi();
                                    bearerToken = bearerToken?.Split(" ")[1];
                                    objectsApi.Configuration.AccessToken = bearerToken;
                                    dynamic objectsResp = await objectsApi.getS3DownloadURLAsync(Constants.BUCKET_KEY,
                                        "result.zip",
                                        new Dictionary<string, object>
                                        {
                                            { "minutesExpiration", 15.0 },
                                            { "useCdn", true }
                                        });

                                    await DownloadToDocsAsync(objectsResp.url,
                                   "output.zip");
                                    await DownloadToDocsAsync(workItemStatus.ReportUrl, log);                                   
                                }
                                return; // we have reached some conclusion
                            }
                            break;
                        case "error":
                            Console.WriteLine(json["data"]);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Buffer: {Encoding.UTF8.GetString(buffer)}");
                    Console.WriteLine(e);
                }
            }
        }

        private async Task<string> SetupActivityAsync(string myApp)
        {
            Console.WriteLine("Setting up activity...");
            var myActivity = $"{Constants.OWNER}.{Constants.ACTIVITY}+{Constants.LABEL}";
            var actResponse = await da.ActivitiesApi.GetActivityAsync(myActivity, throwOnError: false);
            var activity = new Activity()
            {
                Appbundles = new List<string>()
                    {
                        myApp
                    },
                CommandLine = new List<string>()
                    {
                         $"$(engine.path)\\accoreconsole.exe /i \"$(args[input].path)\" /al \"$(appbundles[{Constants.BUNDLENAME}].path)\" /s \"$(settings[script].path)\""
                    },
                Engine = "Autodesk.AutoCAD+24_3",
                Settings = new Dictionary<string, ISetting>()
                    {
                        { "script", new StringSetting() { Value = "EXTRACTDATA\n" } }
                    },
                Parameters = new Dictionary<string, Parameter>()
                    {
                        {
                        "input",
                            new Parameter()
                            {
                                Verb= Verb.Get,
                                LocalName ="$(input)",
                                Required = true
                            }
                        },
                        {
                        "output", 
                            new Parameter()
                            {
                                Verb = Verb.Put,
                                LocalName = "output",
                                Zip = true,
                                Required = true
                               
                            }
                        }
                    },
                Id = Constants.ACTIVITY
            };
            if (actResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Creating activity {myActivity}...");
                await da.CreateActivityAsync(activity, Constants.LABEL);
                return myActivity;
            }
            await actResponse.HttpResponse.EnsureSuccessStatusCodeAsync();
            Console.WriteLine("\tFound existing activity...");
            if (!Equals(activity, actResponse.Content))
            {
                Console.WriteLine($"\tUpdating activity {myActivity}...");
                await da.UpdateActivityAsync(activity, Constants.LABEL);
            }
            return myActivity;
        }

        private bool Equals(Autodesk.Forge.DesignAutomation.Model.Activity a, Autodesk.Forge.DesignAutomation.Model.Activity b)
        {
            Console.Write("\tComparing activities...");
            //ignore id and version
            b.Id = a.Id;
            b.Version = a.Version;
            var res = a.ToString() == b.ToString();
            Console.WriteLine(res ? "Same." : "Different");
            return res;
        }

        private async Task<string> SetupAppBundleAsync()
        {
            Console.WriteLine("Setting up appbundle...");
            var myApp = $"{Constants.OWNER}.{Constants.BUNDLENAME}+{Constants.LABEL}";
            var appResponse = await da.AppBundlesApi.GetAppBundleAsync(myApp, throwOnError: false);
            var app = new AppBundle()
            {
                Engine = Constants.TARGETENGINE,
                Id = Constants.BUNDLENAME
            };
            var package = GetZip();
            if (appResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"\tCreating appbundle {myApp}...");
                await da.CreateAppBundleAsync(app, Constants.LABEL, package);
                return myApp;
            }
            await appResponse.HttpResponse.EnsureSuccessStatusCodeAsync();
            Console.WriteLine("\tFound existing appbundle...");
            if (!await EqualsAsync(package, appResponse.Content.Package))
            {
                Console.WriteLine($"\tUpdating appbundle {myApp}...");
                await da.UpdateAppBundleAsync(app, Constants.LABEL, package);
            }
            return myApp;
        }
        private async Task<bool> EqualsAsync(string a, string b)
        {
            Console.Write("\tComparing bundles...");
            using var aStream = File.OpenRead(a);
            var bLocal = await DownloadToDocsAsync(b, "das-appbundle.zip");
            using var bStream = File.OpenRead(bLocal);
            using var hasher = SHA256.Create();
            var res = hasher.ComputeHash(aStream).SequenceEqual(hasher.ComputeHash(bStream));
            Console.WriteLine(res ? "Same." : "Different");
            return res;
        }

        private static async Task<string> DownloadToDocsAsync(string url, string localFile)
        {
            
            if (File.Exists(localFile))
                File.Delete(localFile);
            using var client = new HttpClient();
            if (url.StartsWith("urn"))
            {
               

            }
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using (var fs = new FileStream(localFile, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
            Console.WriteLine($"Downloading {localFile}");
            return localFile;
        }
       

        static string GetZip()
        {
            Console.WriteLine("\nGetting autoloader zip...");
            // get directory
            var dir = VisualStudioProvider.TryGetSolutionDirectoryInfo();
            // if directory found
            if (dir != null)
            {
                var zip = Path.Combine(dir.FullName, "Bundles", "LMVExporter.bundle.zip");
                return zip;
            }
           
            return string.Empty;
        }

    }
}