using System;
using System.Collections.Generic;
using System.Text;

namespace EtsGitTools.Model
{
    public class License
    {
        public string key { get; set; }
        public string name { get; set; }
        public string spdx_id { get; set; }
        public object url { get; set; }
        public string node_id { get; set; }
    }
}
