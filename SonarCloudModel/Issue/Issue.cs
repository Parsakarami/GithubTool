using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Issue
{
    public class Issue
    {
        public string key { get; set; }
        public string rule { get; set; }
        public string severity { get; set; }
        public string component { get; set; }
        public string project { get; set; }
        public int line { get; set; }
        public string hash { get; set; }
        public TextRange textRange { get; set; }
        public List<Flow> flows { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public string effort { get; set; }
        public string debt { get; set; }
        public string author { get; set; }
        public List<object> tags { get; set; }
        public DateTime creationDate { get; set; }
        public DateTime updateDate { get; set; }
        public string type { get; set; }
        public string organization { get; set; }
    }

    public class TextRange
    {
        public int startLine { get; set; }
        public int endLine { get; set; }
        public int startOffset { get; set; }
        public int endOffset { get; set; }
    }

    public class Flow
    {
        public List<Location> locations { get; set; }
    }

    public class Location
    {
        public string component { get; set; }
        public TextRange textRange { get; set; }
        public string msg { get; set; }
    }
}
