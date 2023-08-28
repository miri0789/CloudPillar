using CloudPillar.Agent.API.Entities;
using FluentValidation;
namespace CloudPillar.Agent.API.Validators
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