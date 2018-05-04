using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DropDownloadCore
{
    // Let us mock out drop http calls.
    public interface IDropApi
    {
         Task<IEnumerable<VstsFile>> GetVstsManifest(Uri manifestUri, string blobapiversion, string relativeroot);
    }
}
