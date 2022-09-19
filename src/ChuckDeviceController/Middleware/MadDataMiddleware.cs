﻿namespace ChuckDeviceController.Middleware
{
    using ChuckDeviceController.Extensions;

    public sealed class MadDataMiddleware
    {
        private const string RawDataEndpoint = "/raw";
        private const string DefaultMadUsername = "PogoDroid";

        private readonly RequestDelegate _next;

        public MadDataMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var isMad = context.IsMadDeviceRequest(RawDataEndpoint);
                if (isMad)
                {
                    await context.ConvertPayloadDataAsync(DefaultMadUsername);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error - MadDataMiddleware: {ex.Message}");
            }

            await _next(context);
        }
    }
}