using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScantistSCA.models
{
    class ComponentModel
    {
        public string name { get; set; }
        public string version { get; set; }
        public string license { get; set; }
        public int vulnerabilityCount { get; set; }

        public ComponentModel(String name, String version, String license, int vulnerabilityCount)
        {
            this.name = name;
            this.version = version;
            this.license = license;
            this.vulnerabilityCount = vulnerabilityCount;
        }

    }
}
