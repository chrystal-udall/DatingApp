using System.Net;
using System.Text.Json;
using API.Errors;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace API.Middleware
{
  public class ExceptionMiddleware
    {
        private IHostEnvironment Environment { get; }
        private readonly ILogger<ExceptionMiddleware> _logger;
        private RequestDelegate Next { get; } //indicates next middleware we need to go onto

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
        {
            this.Next = next;
            this._logger = logger;
            this.Environment = environment;
            
        }

        public async Task InvokeAsync(HttpContext context) { // needs to be called invoke async
            try{
                await Next(context);
            }
            catch(Exception ex) { //catches exception not handled elsewhere in our program
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                var response = Environment.IsDevelopment()
                    ? new ApiException(context.Response.StatusCode, ex.Message, ex.StackTrace?.ToString()) 
                    : new ApiException(context.Response.StatusCode, ex.Message, "Internal Server Error");

                var options = new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}