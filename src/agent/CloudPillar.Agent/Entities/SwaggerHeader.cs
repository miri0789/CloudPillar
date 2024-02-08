using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class SwaggerHeader : IOperationFilter
{
    //This class was added for custom headers in swagger actions
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = Constants.X_DEVICE_ID,
            In = ParameterLocation.Header,
            Description = "Device id",
            Required = false
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = Constants.X_SECRET_KEY,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Device Secret value"
        });

    }
}
