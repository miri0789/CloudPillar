using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class SwaggerHeader : IOperationFilter
{
    //This class was added for custom headers in swagger actions
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = AuthorizationConstants.X_DEVICE_ID,
            In = ParameterLocation.Header,
            Description = "The device id",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "String"
            }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = AuthorizationConstants.X_SECRET_KEY,
            In = ParameterLocation.Header,
            Required = false,
            Description = "The OneMD value",
            Schema = new OpenApiSchema
            {
                Type = "String"
            }
        });

    }
}
