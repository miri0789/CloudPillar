using CloudPillar.Agent.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Entities.Twin;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeviceStateFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.Controller as AgentController;
        if (controller != null)
        {
            var deviceState = controller._stateMachineHandler.GetCurrentDeviceState();

            if (deviceState == DeviceStateType.Busy)
            {
                context.Result = new ObjectResult(StateMachineConstants.BUSY_MESSAGE) { StatusCode = 503 };
                return;
            }
            if (deviceState != DeviceStateType.Ready)
            {
                context.Result = new BadRequestObjectResult(StateMachineConstants.NOT_READY_MESSAGE);
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
