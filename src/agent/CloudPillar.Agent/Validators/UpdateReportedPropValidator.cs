using CloudPillar.Agent.Entities;
using FluentValidation;
namespace CloudPillar.Agent.Validators
{
    public class UpdateReportedPropValidator : AbstractValidator<UpdateReportedProp>
    {
        public UpdateReportedPropValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}