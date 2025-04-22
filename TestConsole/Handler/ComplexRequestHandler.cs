using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;

namespace TestConsole.Handler
{
    public class ComplexRequestHandler : IRequestHandler<ComplexRequest, int>
    {
        public int Handle(ComplexRequest request) => request.Value * 2;
    }
}
