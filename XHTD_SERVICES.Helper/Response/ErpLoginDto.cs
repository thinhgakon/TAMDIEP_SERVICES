using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Response
{
    public class ERPLoginRequestDto
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public bool? RememberMe { get; set; }
    }

    public class ERPLoginResultDto
    {
        public string Token { get; set; }

        public string UserName { get; set; }

        public string User_Id { get; set; }
    }
}
