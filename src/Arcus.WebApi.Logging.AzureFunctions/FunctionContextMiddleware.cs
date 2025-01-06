using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Represents a middleware component that assigns the current <see cref="FunctionContext"/> to the <see cref="IFunctionContextAccessor"/>.
    /// </summary>
    public class FunctionContextMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IFunctionContextAccessor _contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionContextMiddleware" /> class.
        /// </summary>
        /// <param name="contextAccessor">The instance to manage the <see cref="FunctionContext"/> in this request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contextAccessor"/> is <c>null</c>.</exception>
        public FunctionContextMiddleware(IFunctionContextAccessor contextAccessor)
        {   
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor), "Requires a function context accessor to assign the current function context instance");
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.Azure.Functions.Worker.FunctionContext" /> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous invocation.</returns>
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context), "Requires a function context instance to assign the context to the function context accessor");
            }
    
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next), "Requires a 'next' function to chain this middleware to the next action in the HTTP request pipeline");
            }

            _contextAccessor.FunctionContext = context;
            await next(context);
        }
    }
}
