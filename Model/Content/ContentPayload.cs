using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools.Model.Content
{
    public class ContentPayload
    {
        public string message { get; set; }
        public string sha { get; set; }
        //Base64 String
        public string content { get; set; }
    }
}
