using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voxsay
{
    internal class ProductMap
    {
        public string Hostname { get; set; }

        public int? Portnumber { get; set; }

        public ProductMap(string host, int port)
        {
            Hostname = host;
            Portnumber = port;
        }
    }
}
