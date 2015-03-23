using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SyncDisk2Google.GoogleDrive.API;
using System.IO;
using SyncDisk2Google.LocalFolder;
using System.Security.Cryptography;

namespace SyncDisk2Google
{
    public partial class FormGoogleDoc : Form
    {
        GoogleDriveMgr googleMgr = new GoogleDriveMgr();
        String savePath = "";
        Setup setupfile;
        public FormGoogleDoc()
        {
            InitializeComponent();
            if (File.Exists("./Steup.xml"))
            {
                using (FileStream fstream = File.Open("./Setup.xml", FileMode.Open))
                {
                    setupfile = Setup.FromStream(fstream);
                }
            }
            else
            {
                setupfile = new Setup();
                using (FileStream fstream = File.Open("./Setup.xml", FileMode.OpenOrCreate))
                {
                    fstream.SetLength(0);
                    setupfile.ToStream(fstream);
                    fstream.Flush();
                }
            }
            savePath = setupfile.LocalPath;
            googleMgr.ClientJsonPath = setupfile.GoogleClientJson;
        }


        List<string> ParsePath(string path)
        {
            List<string> AllFiles = new List<string>();
            string[] SubDirs = Directory.GetDirectories(path);
            AllFiles.AddRange(SubDirs);
            AllFiles.AddRange(Directory.GetFiles(path));
            foreach (string subdir in SubDirs)
                ParsePath(subdir);
            return AllFiles;
        }

        private void PushDirToGoogle()
        {
            // make sure file path exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            LocalFolderController localFolderController = new LocalFolderController(savePath);
            var remotefilelist = googleMgr.ListFiles();
            
            // for each folder
            foreach (var subfolder in localFolderController.Folders)
            { 
                if(Directory.Exists(subfolder.FullPath))
                {
                    bool existOnServer = false;
                    foreach(var remotefile in remotefilelist.Items)
                    {
                        if(remotefile.MimeType == "application/vnd.google-apps.folder")
                        {
                            if(CompareFolder(remotefilelist,remotefile,subfolder))
                            {
                                existOnServer = true;
                                break;
                            }
                        }
                    }
                    if (!existOnServer)
                    { 
                        Google.Apis.Drive.v2.Data.File remotefolder = new Google.Apis.Drive.v2.Data.File();
                        remotefolder.Title = subfolder.Title;
                        // set folder parent, if it is not in rootfolder
                        if (!subfolder.Parent.IsRoot)
                        {
                            bool parentFound = false;
                            foreach (var remotefile in remotefilelist.Items)
                            {
                                if (remotefile.MimeType == "application/vnd.google-apps.folder")
                                {
                                    if (CompareFolder(remotefilelist, remotefile, subfolder.Parent))
                                    {
                                        Google.Apis.Drive.v2.Data.ParentReference parent = new Google.Apis.Drive.v2.Data.ParentReference();
                                        parent.Id = remotefile.Id;
                                        remotefolder.Parents.Add(parent);
                                        parentFound = true;
                                        googleMgr.UploadFolder(remotefolder);
                                        break;
                                    }
                                }
                            }
                            if (!parentFound)
                                Console.WriteLine("Can not create folder, can not find appropriate remote file parent for local folder: " + subfolder.FullPath);
                        }
                        else
                        {
                            googleMgr.UploadFolder(remotefolder);
                        }
                    }
                }
            }
            // for each file
            foreach (var localfile in localFolderController.Files)
            {
                if (File.Exists(localfile.FullPath))
                {
                    bool existOnServer = false;
                    foreach (var remotefile in remotefilelist.Items)
                    {
                        if (remotefile.Title.Equals(localfile.Title))
                        {
                            if (remotefile.Md5Checksum == localfile.Md5Hash)
                            {
                                existOnServer = true;
                                break;
                            }

                            else
                            {
                                Console.WriteLine("Remote file {0} is different than local file {1}", remotefile.Title, localfile.FullPath);

                            }
                        }
                    }
                    if (!existOnServer)
                    {
                        googleMgr.UploadFile(localfile.FullPath);
                    }
                }
            }
        }
        private bool CompareFolder(Google.Apis.Drive.v2.Data.FileList remotefilelist, Google.Apis.Drive.v2.Data.File remotefolder,FolderNode folderNode)
        {
            List<string> paths = new List<string>();
            if (folderNode.IsRoot)
                return false;

            if (remotefolder.Title == folderNode.Title)
            {
                if (remotefolder.Parents.Count < 1)
                {
                    if (folderNode.Parent.IsRoot == true)
                        return true;
                }
                else
                {
                    foreach (Google.Apis.Drive.v2.Data.ParentReference parent in remotefolder.Parents)
                    {
                        if (parent.IsRoot == true)
                        {
                            if (folderNode.Parent.IsRoot == true)
                                return true;
                        }
                        else
                        {
                            foreach (var entry in remotefilelist.Items)
                            {
                                if (entry.Id == parent.Id)
                                {
                                    if (CompareFolder(remotefilelist, entry, folderNode.Parent))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private  List<string> RetrievePath(Google.Apis.Drive.v2.Data.FileList remotefilelist, Google.Apis.Drive.v2.Data.File file)
        {
            List<string> paths = new List<string>();
            if (file.Parents.Count < 1)
            {
                paths.Add(Path.Combine(savePath, file.Title));
            }
            else
            {
                foreach (Google.Apis.Drive.v2.Data.ParentReference parent in file.Parents)
                {
                    if (parent == null || parent.IsRoot == true)
                    {
                        paths.Add(Path.Combine(savePath, file.Title));
                    }
                    else
                    {
                        foreach (var entry in remotefilelist.Items)
                        {
                            if (entry.Id == parent.Id)
                            {
                                List<string> subpaths = RetrievePath(remotefilelist, entry);
                                
                                foreach (var subpath in subpaths)
                                {
                                    if (!Directory.Exists(subpath))
                                        Directory.CreateDirectory(subpath);
                                    paths.Add(Path.Combine(subpath, file.Title));
                                }

                            }
                        }
                    }
                }
            }
            return paths;
        }

        private void PullDirFromGoogle()
        { 
        
            // make sure file path exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            var remotefilelist = googleMgr.ListFiles();
            List<string> remoteFileLocalPaths = new List<string>();
            foreach (var remotefile in remotefilelist.Items)
            {
                
                Console.WriteLine(remotefile.Title);
                if (remotefile.DownloadUrl != null) // handle remote file
                {
                    // check if file paths exist in directory, if so, remove them from list   
                    List<string> paths = RetrievePath(remotefilelist,remotefile);
                    for (int i = paths.Count - 1; i >= 0;i-- )
                    {
                        bool pathDuplicated = false;
                        foreach (var existedpath in remoteFileLocalPaths)
                        { 
                            if(String.Compare(existedpath,paths[i]) == 0)
                            {
                                Console.WriteLine("Path conflict: Already have " + existedpath);
                                paths.RemoveAt(i);
                                pathDuplicated = true;
                                break;
                            }

                        }
                        if (pathDuplicated)
                            continue;
                        else
                            remoteFileLocalPaths.Add(paths[i]);
                        if (File.Exists(paths[i]))
                        {
                            FileInfo lofileinfo = new FileInfo(paths[i]);
                            if (String.Compare(remotefile.Title, lofileinfo.Name) == 0 && remotefile.FileSize == lofileinfo.Length)
                            {
                                if (remotefile.FileSize == 0)
                                {
                                    paths.RemoveAt(i);
                                }
                                else
                                {
                                    string Md5Hash = "";
                                    using (var md5 = MD5.Create())
                                    {
                                        using (var stream = File.OpenRead(paths[i]))
                                        {
                                            Md5Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                                        }
                                    }
                                    if (Md5Hash == remotefile.Md5Checksum)
                                    {
                                        paths.RemoveAt(i);
                                    }
                                }
                            }
                        }
                    }
                    
                    if (paths.Count > 0)
                    {
                        if(googleMgr.DownloadFile(remotefile,paths[0]))
                        {
                            if(paths.Count > 1)
                            {
                                for(int i = 1;i< paths.Count;i++)
                                {
                                    if(File.Exists(paths[i]))
                                        File.Delete(paths[i]);
                                    File.Copy(paths[0], paths[i]);
                                }
                            }
                        }
                        else
                        {                            
                                Console.WriteLine("Can not receive file: " + remotefile.Title);
                        }                     
                        
                        
                    }
                }
                else if (remotefile.MimeType == "application/vnd.google-apps.folder") // handle remote folder
                {

                    List<string> paths = RetrievePath(remotefilelist, remotefile);
                    foreach (var path in paths)
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(Path.Combine(savePath, remotefile.Title));
                    }
                }
                else if(remotefile.ExportLinks.Count > 0)
                {
                // handle remote google doc
                
                }
            }
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            PushDirToGoogle();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            PullDirFromGoogle();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (googleMgr.InitService())
            {
                buttonDownload.Enabled = true;
                buttonUpload.Enabled = true;
                setupToolStripMenuItem.Enabled = false;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void setApplicationPremitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = false;
            if (fdlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                setupfile.GoogleClientJson = fdlg.FileName;
                using (FileStream fstream = File.Open("./Setup.xml", FileMode.OpenOrCreate))
                {
                    fstream.SetLength(0);
                    setupfile.ToStream(fstream);
                    fstream.Flush();
                }
                googleMgr.ClientJsonPath = setupfile.GoogleClientJson;
            }

        }

        private void setLocalFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                setupfile.LocalPath = folderDialog.SelectedPath;
                using (FileStream fstream = File.Open("./Setup.xml", FileMode.OpenOrCreate))
                {
                    fstream.SetLength(0);
                    setupfile.ToStream(fstream);
                    fstream.Flush();
                }
                savePath = folderDialog.SelectedPath;
            }
            
        }

        private void FormGoogleDoc_Load(object sender, EventArgs e)
        {

        }
        
    }
}
