using Microsoft.AspNetCore.Builder;

namespace Alexa.NET.Security.Middleware
{
    /// <summary>
    /// Middleware builder
    /// </summary>
    public static class AlexaRequestValidationMiddlewareExtension
    {
        /// <summary>
        /// Add AlexaReqeustValidationMiddleware to the request pipeline
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAlexaRequestValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AlexaRequestValidationMiddleware>();
        }
    }
}