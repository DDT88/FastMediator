using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Utils
{
    public class AsyncPingRequest : IAsyncRequest<string>
    {
        public string Message { get; }

        public AsyncPingRequest(string message)
        {
            Message = message;
        }
    }
}
