using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Company.TestProject.Shared;
using Company.TestProject.Shared.BrokenRules;
using Company.TestProject.WebApiClient.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.TestProject.WebApi
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IResourceProvider resourceProvider;
        private readonly ILogger<ExceptionMiddleware> logger;

        public ExceptionMiddleware(RequestDelegate next, IResourceProvider resourceProvider, ILogger<ExceptionMiddleware> logger)
        {
            this.next = next;
            this.resourceProvider = resourceProvider;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await this.next(httpContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await this.HandleExceptionAsync(httpContext, ex).ConfigureAwait(false);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (exception is BrokenRuleException brokenRuleException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = new List<ErrorDetail>();
                foreach (var brokenRule in brokenRuleException.BrokenRules)
                {
                    string errorMessage;

                    if (brokenRule.Prms != null && brokenRule.Prms.Any())
                    {
                        errorMessage = string.Format(this.resourceProvider.GetTextResourceById(brokenRule.ErrorCode), brokenRule.Prms);
                    }
                    else
                    {
                        errorMessage = this.resourceProvider.GetTextResourceById(brokenRule.ErrorCode);
                    }

                    errors.Add(new ErrorDetail
                    {
                        ErrorCode = brokenRule.ErrorCode,
                        ErrorType = brokenRule.GetType().Name,
                        ErrorMessage = errorMessage,
                        Severity = brokenRule.Severity.ToString()
                    });
                }

                await context.Response.WriteAsync(JsonConvert.SerializeObject(errors, Formatting.Indented)).ConfigureAwait(false);
            }
            else if (exception is ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        ErrorCode = "Validation_Error",
                        ErrorType = "Validation Error",
                        ErrorMessage = validationException.Message,
                        Severity = "Warning"
                    }
                };

                await context.Response.WriteAsync(JsonConvert.SerializeObject(errors, Formatting.Indented)).ConfigureAwait(false);
            }
            else
            {
                var exceptionDetail = new ExceptionDetail
                {
                    ErrorMessage = this.resourceProvider.GetTextResourceById("UnhandledException"),
                    Exception = exception
                };

                await context.Response.WriteAsync(JsonConvert.SerializeObject(exceptionDetail, Formatting.Indented)).ConfigureAwait(false);
            }
        }
    }
}
