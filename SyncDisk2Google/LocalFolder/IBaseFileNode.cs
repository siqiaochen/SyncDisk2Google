using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncDisk2Google.LocalFolder
{
    interface IBaseFileNode
    {
        String Title { get; set; }
        String FullPath { get; set; } 
    }
}
