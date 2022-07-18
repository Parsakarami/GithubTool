using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel
{
    public class ProjectListResponse
    {
        public Paging paging { get; set; }
        public List<Component> components { get; set; }
    }
}
