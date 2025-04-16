using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Utils
{
    public class AsyncSomethingHappened : IAsyncNotification
    {
        public string Message { get; set; } = "";
    }
}
