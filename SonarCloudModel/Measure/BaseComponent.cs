using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Measure
{
    public class BaseComponent
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string qualifier { get; set; }
        public List<Measure> measures { get; set; }
    }
}
