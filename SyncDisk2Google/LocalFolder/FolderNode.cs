using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SyncDisk2Google.LocalFolder
{
    class FolderNode : IBaseFileNode
    {
        public FolderNode(string path)
        {
            // TODO: Complete member initialization            
            this.FullPath = path;
            this.Title = Path.GetFileName(path);
            this.Parent = null;
            IsRoot = false;
        }

        #region IBaseFileNode Members

        public string Title { get; set; }
        public string FullPath { get; set; }

        #endregion

        public FolderNode Parent { get; set; }
        private List<IBaseFileNode> childs = new List<IBaseFileNode>();
        public List<IBaseFileNode> Childs { get { return childs; } set { childs = value; } }
        public Boolean IsRoot { get; set; }



    }
}
