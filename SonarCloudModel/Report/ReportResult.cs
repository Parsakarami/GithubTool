using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Report
{

    public class ReportResult
    {
        public ReportResult()
        {
            MetircsValues = new Dictionary<string, string>();
        }
        public string ProjectName { get; set; }
        public Dictionary<string, string> MetircsValues { get; set; }
    }
}
