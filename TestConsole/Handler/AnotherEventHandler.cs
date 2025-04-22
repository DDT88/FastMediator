using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;

namespace TestConsole.Handler
{
    public class AnotherEventHandler : INotificationHandler<AnotherEvent>
    {
        public void Handle(AnotherEvent notification)
        {
            // Intenzionalmente vuoto per il benchmark
        }
    }
}
