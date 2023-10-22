using CloudPillar.Agent.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Entities.Twin;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeviceStateFilterAttribute : ActionFilterAttribute
{
    public override async void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.Controller as AgentController;
        if (controller != null)
        {
            var deviceState = await controller._stateMachine.GetState();

            if (deviceState != DeviceStateType.Ready)
            {
                context.Result = new BadRequestObjectResult("Device is not ready");
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
