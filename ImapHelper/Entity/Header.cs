using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImapHelper.Entity
{
    public class Header
    {
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public string To { get; set; }
        public string MessageID { get; set; }
        public string Subject { get; set; }
        public DateTime Date { get; set; }
    }
}
