using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace SyncDisk2Google.LocalFolder
{
    class FileNode : IBaseFileNode
    {
        public FileNode(string path)
        {
            // TODO: Complete member initialization
            FullPath = path;
            Title = Path.GetFileName(path);
            Parent = null;
            if (File.Exists(path))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(path))
                    {

                        Size = stream.Length;
                        Md5Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","").ToLower();
                    }
                }
                CreateTime = File.GetCreationTime(path);
                ModifiedTime = File.GetLastWriteTime(path);
            }
        }
        #region IBaseFileNode Members
        public string Title { get; set; }        
        public string FullPath { get; set;}
        #endregion
        public FolderNode Parent { get; set; }
        public String Md5Hash { get; set; }
        public long Size { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }
}
