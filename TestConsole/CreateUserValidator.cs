using FastMediator;
using FastMediator.Interfaces;
using FastMediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class CreateUserValidator : AbstractValidator<Ping>
    {
        protected override void ValidateInternal(Ping request, ValidationResult result)
        {
            if (string.IsNullOrEmpty(request.Message))
                result.AddError(nameof(request.Message), "Message obbligatorio");
        }
    }
}
