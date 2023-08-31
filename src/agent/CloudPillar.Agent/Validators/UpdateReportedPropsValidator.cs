using CloudPillar.Agent.Entities;
using FluentValidation;
namespace CloudPillar.Agent.Validators
{
    public class UpdateReportedPropsValidator : AbstractValidator<UpdateReportedProps>
    {
        public UpdateReportedPropsValidator()
        {
            RuleForEach(x => x.Properties).SetValidator(new UpdateReportedPropValidator());
        }
    }
}