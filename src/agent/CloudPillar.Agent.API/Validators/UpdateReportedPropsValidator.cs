using CloudPillar.Agent.API.Entities;
using FluentValidation;
namespace CloudPillar.Agent.API.Validators
{
    public class UpdateReportedPropsValidator : AbstractValidator<UpdateReportedProps>
    {
        public UpdateReportedPropsValidator()
        {
            RuleForEach(x => x.updateReportedProps).SetValidator(new UpdateReportedPropValidator());
        }
    }
}