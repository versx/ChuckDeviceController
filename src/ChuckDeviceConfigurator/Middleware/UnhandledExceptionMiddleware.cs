﻿namespace ChuckDeviceConfigurator.Middleware
{
    using System.Net;

    using ChuckDeviceController.Extensions.Json;

    public class UnhandledExceptionMiddleware
    {
        private static readonly ILogger<UnhandledExceptionMiddleware> _logger =
            new Logger<UnhandledExceptionMiddleware>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly RequestDelegate _next;

        public UnhandledExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogCritical($"Unhandled Error: {exception}");

            await context.Response.WriteAsync(new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from the custom middleware.",
            }.ToString());
        }
    }

    public class ErrorDetails
    {
        public int StatusCode { get; set; }

        public string? Message { get; set; }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}