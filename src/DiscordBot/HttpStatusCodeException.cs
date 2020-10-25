using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MariBot
{
    public class HttpStatusCodeException : Exception
    {
        private static readonly string DefaultMessage = "Status code does not indicate success.";
        public uint statusCode { get; set; }

        public HttpStatusCodeException() : base(DefaultMessage) 
        { 
        }
        public HttpStatusCodeException(HttpStatusCode statusCode) : base(DefaultMessage + " " + statusCode)
        {
            this.statusCode = (uint)statusCode;
        }
        public HttpStatusCodeException(HttpStatusCode statusCode, Exception innerException) : base(DefaultMessage + " " + statusCode, innerException)
        {
            this.statusCode = (uint)statusCode;
        }
    }
}
