using CloudPillar.Agent.Entities;
using FluentValidation;
namespace CloudPillar.Agent.Validators
{
    public class UpdateReportedPropValidator : AbstractValidator<TwinReportedCustomProp>
    {
        public UpdateReportedPropValidator()
        {
            RuleFor(x => x.Name).NotEmpty().NotNull();
            RuleFor(x => x.Value).NotNull();
        }
    }
}