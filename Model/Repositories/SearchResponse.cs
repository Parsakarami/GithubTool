using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools.Model.Repository
{
    public class SearchResponse
    {
        public int total_count { get; set; }
        public bool incomplete_results { get; set; }
        public List<Repository> items { get; set; }
    }
}
