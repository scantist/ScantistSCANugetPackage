using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScantistSCA.models
{
    class ScanResult
    {
        public List<ScanResultVulnerability> vulnerabilities;

        public List<ScanResultLicense> licenses;

        public List<ScanResultComponent> components;
    }
}
