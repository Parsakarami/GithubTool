using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel
{
    public class Component
    {
        public string organization { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string qualifier { get; set; }
        public string project { get; set; }
    }
}
