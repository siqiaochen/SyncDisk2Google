using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Util.Store;
using System.Threading;
using Google.Apis.Services;
using System.Net;
using System.Xml.Serialization;

namespace SyncDisk2Google.GoogleDrive.API
{
    class GoogleDriveMgr
    {
        private string clientJsonPath = "./client_secret.json";
        public string ClientJsonPath
        {
            get { return clientJsonPath; }
            set { clientJsonPath = value; }
        }
        private DriveService driverservice;
        bool serviceStarted = false;
        public bool InitService()
        {
            driverservice = createService();
            serviceStarted = true;
            return true;
        }
        public bool ReleaseSerivce()
        {
            serviceStarted = false;
            driverservice.Dispose();
            driverservice = null;
            return false;
        }
        private DriveService createService()
        {// Create the service.
            UserCredential credential;
            using (var filestream = new FileStream(clientJsonPath,
                FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(filestream).Secrets,
                    new[] { DriveService.Scope.Drive },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("DriveCommandLineSample")).Result;
            };
            DriveService service = new DriveService(new BaseClientService.Initializer()
                 {
                     HttpClientInitializer = credential,
                     ApplicationName = "SymcDisk2Google",
                 });
            return service;
        }
        public Google.Apis.Drive.v2.Data.FileList ListFiles()
        {
            if (!serviceStarted || driverservice == null)
            {
                return null;
            }
            var listrequest = driverservice.Files.List();
            Google.Apis.Drive.v2.Data.FileList list = listrequest.Execute();
            return list;
        }
        public Google.Apis.Drive.v2.Data.File UploadFile(String filepath)
        {
            if (!serviceStarted)
                return null;
            Google.Apis.Drive.v2.Data.File body = new Google.Apis.Drive.v2.Data.File();
            body.Title = Path.GetFileName(filepath);
            body.Description = "File Sync";
            FileInfo finfo = new FileInfo(filepath);
            body.ModifiedDate = finfo.LastWriteTime;
            body.CreatedDate = finfo.CreationTime;
            using (FileStream fstream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                FilesResource.InsertMediaUpload request = driverservice.Files.Insert(body, fstream, "text/plain");
                request.Upload();
                Google.Apis.Drive.v2.Data.File file = request.ResponseBody;
                return file;
            }
        }

        public Google.Apis.Drive.v2.Data.File UploadFolder(Google.Apis.Drive.v2.Data.File folder)
        {
            if (!serviceStarted)
                return null;

                FilesResource.InsertRequest request = driverservice.Files.Insert(folder);
                Google.Apis.Drive.v2.Data.File file = request.Execute();
                return file;
            
        }       


        public Boolean DownloadFile(Google.Apis.Drive.v2.Data.File file, string destPath)
        {
            if (!serviceStarted || driverservice == null)
                return false;
            if (!String.IsNullOrEmpty(file.DownloadUrl))
            {
                try
                {
                    var dl = driverservice.HttpClient.GetStreamAsync(file.DownloadUrl);
                    Stream mStream = dl.Result;
                    using (FileStream fstream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        fstream.SetLength(0);
                        byte[] arr = new byte[4096];
                        int bytesRead = 0;
                        int bytesReceived = 0;
                        do
                        {
                            bytesRead = mStream.Read(arr, 0, arr.Length);
                            bytesReceived += bytesRead;
                            fstream.Write(arr, 0, bytesRead);
                        }
                        while (bytesRead > 0);
                        Console.WriteLine("File {0} received, size: {1}", Path.GetFileName(destPath), bytesReceived);
                    }
                    if (file.CreatedDate.HasValue)
                        File.SetCreationTime(destPath, file.CreatedDate.Value);
                    if (file.ModifiedDate.HasValue)
                        File.SetLastWriteTime(destPath, file.ModifiedDate.Value);
                    return true;

                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return false;
                }
            }
            return false;

        }
    }


    
}
