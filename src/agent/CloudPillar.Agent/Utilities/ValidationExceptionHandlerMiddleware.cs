using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Logger;

namespace CloudPillar.Agent.Utilities
{
    public class ValidationExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerHandler _logger;

        public ValidationExceptionHandlerMiddleware(RequestDelegate next, ILoggerHandler logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                
                _logger.Error($"Validation error occurred : {ex.Message}");


                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(ex.Message);
            }
        }
    }
}