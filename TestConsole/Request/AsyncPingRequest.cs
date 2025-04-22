using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Request
{
    public record AsyncPingRequest : IAsyncRequest<string>
    {
        public string Message { get; set; }

        public AsyncPingRequest(string message)
        {
            Message = message;
        }
    }
}
