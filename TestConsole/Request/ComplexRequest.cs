using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Request
{
    public class ComplexRequest : IRequest<int>
    {
        public int Value { get; set; }
    }
}
