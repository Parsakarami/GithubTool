using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Measure
{
    public class MeasureResponse
    {
        public Paging paging { get; set; }
        public BaseComponent baseComponent { get; set; }
        public List<Component> components { get; set; }
    }
}
