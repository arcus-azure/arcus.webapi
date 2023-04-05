using System.Threading;
using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Provides an implementation of <see cref="IFunctionContextAccessor" /> based on the current execution context.
    /// </summary>
    /// <remarks>
    ///     Inspired by <a href="https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http/src/HttpContextAccessor.cs" />.
    /// </remarks>
    public class DefaultFunctionContextAccessor : IFunctionContextAccessor
    {
        private static readonly AsyncLocal<FunctionContextHolder> FunctionContextCurrent = new AsyncLocal<FunctionContextHolder>();

        /// <summary>
        /// Gets or sets the current <see cref="Microsoft.Azure.Functions.Worker.FunctionContext"/>.
        /// Returns <see langword="null" /> if there is no active <see cref="Microsoft.Azure.Functions.Worker.FunctionContext" />.
        /// </summary>
        public FunctionContext FunctionContext
        {
            get => FunctionContextCurrent.Value?.Context;
            set
            {
                FunctionContextHolder holder = FunctionContextCurrent.Value;
                if (holder != null)
                {
                    holder.Context = null;
                }

                if (value != null)
                {
                    FunctionContextCurrent.Value = new FunctionContextHolder { Context = value };
                }
            }
        }

        private sealed class FunctionContextHolder
        {
            public FunctionContext Context;
        }
    }
}