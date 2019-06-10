using System.Net;

namespace Nop.Plugin.Payments.Rave.Models
{
    class RaveResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public string link { get; set; }
    }
  public class RaveHttpResponse
    {
        public HttpStatusCode code { get; set; }
       
        public string message { get; set; }
        public bool isSuccessCode { get; set; }
    }
}
