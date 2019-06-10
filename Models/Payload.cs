using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Rave.Models
{    
    public class Payload {
        public string txref { get; set; }
        public string PBFPubKey { get; set; }
        public string customer_email { get; set; }
        public string amount { get; set; }
        public string country { get; set; }
        public string currency { get; set; }
        public string redirect_url { get; set; }
        public string customer_firstname { get; set; }
        public string customer_lastname { get; set; }
        public string customer_phone { get; set; }
        public string custom_title { get; set; }
        public string custom_description { get; set; }
        public string custom_logo { get; set; }
        public string integrity_hash { get; set; } 
    }
}
