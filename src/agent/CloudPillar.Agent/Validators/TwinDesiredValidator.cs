using CloudPillar.Agent.Entities;
using FluentValidation;
using Shared.Entities.Twin;
namespace CloudPillar.Agent.Validators
{
    public class TwinDesiredValidator : AbstractValidator<TwinDesired>
    {
        public TwinDesiredValidator()
        {
        }
    }
}