using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools
{
    public class UserInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public bool IsAuthenticated { get; set; }
        public string SelectedOrganization { get; set; }

        public List<Organization> Organizations { get; set; }
    }
}
