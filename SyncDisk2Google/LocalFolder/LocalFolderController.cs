using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SyncDisk2Google.LocalFolder
{
    class LocalFolderController
    {
        public FolderNode RootFolder { get; set; }
        public List<FolderNode> Folders { get; set; }
        public List<FileNode> Files { get; set; }
        public LocalFolderController(String path)
        {
            path = Path.GetFullPath(path);
            ConstructFolderTree(path);

        }
        public void ConstructFolderTree(string rootPath)
        {            
            Folders = new List<FolderNode>();
            Files = new List<FileNode>();
            if (Directory.Exists(rootPath))
            {
                RootFolder = new FolderNode(rootPath);
                RootFolder.IsRoot = true;
                ParsePath(RootFolder);
            }
        }

        private bool ParsePath(FolderNode folder)
        {

            string[] subdirs = Directory.GetDirectories(folder.FullPath);
            foreach (var subdir in subdirs)
            {
                // parse folder node
                FolderNode fnode = new FolderNode(subdir);
                Folders.Add(fnode);
                fnode.Parent = folder;
                folder.Childs.Add(fnode);
                ParsePath(fnode);

            }
            string[] files = Directory.GetFiles(folder.FullPath);
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    FileNode fnode = new FileNode(file);
                    Files.Add(fnode);
                    folder.Childs.Add(fnode);
                    fnode.Parent = folder;
                }
            }
            return true;
        }
    }
}
