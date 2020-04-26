using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StellarApp.Models
{
    public class AccountInformation
    {
        public class AssetInformation
        {
            public string Type { get; set; }
            public string Code { get; set; }
            public string Balance { get; set; }
        }

        public List<AssetInformation> Assets { get; set; }
    }
}
