using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Issue
{
    public class IssueResponse
    {
        public int total { get; set; }
        public int p { get; set; }
        public int ps { get; set; }
        public Paging paging { get; set; }
        public int effortTotal { get; set; }
        public int debtTotal { get; set; }
        public List<Issue> issues { get; set; }
        public List<Component> components { get; set; }
        public List<SonarCloudOrganization> organizations { get; set; }
        public List<object> facets { get; set; }
    }
}
