using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImapHelper.Entity
{
    public class Body
    {
        public Body()
        {
            ContentType = "text/plain";
            Charset = "utf-8";
        }

        public string ContentType { get; set; }
        public string Charset { get; set; }
        public string Content { get; set; }
        public bool HasPart { get; set; }
        public bool IsHtml
        {
            get
            {
                if (ContentType == "text/plain")
                    return false;
                return true;
            }
        }
    }
}
