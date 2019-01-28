using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetServerMirror
{
    public class NugetPackage
    {
        public NugetPackage(string id, string version)
        {
            ID = id;
            Version = version;
        }

        public string ID { get; }
        public string Version { get; }
    }
}
