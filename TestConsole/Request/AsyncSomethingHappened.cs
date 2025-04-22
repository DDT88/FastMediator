using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Request
{
    public record AsyncSomethingHappened : IAsyncNotification
    {
        public string Message { get; set; } = "";
    }
}
