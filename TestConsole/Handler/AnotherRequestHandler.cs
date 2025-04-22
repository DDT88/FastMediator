using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;

namespace TestConsole.Handler
{
    public class AnotherRequestHandler : IRequestHandler<AnotherRequest, bool>
    {
        public bool Handle(AnotherRequest request) => !string.IsNullOrEmpty(request.Name);
    }
}
