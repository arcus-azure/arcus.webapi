using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Provides access to the current <see cref="Microsoft.Azure.Functions.Worker.FunctionContext"/>, if one is available.
    /// </summary>
    /// <remarks>
    /// This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
    /// It also creates a dependency on "ambient state" which can make testing more difficult.
    /// </remarks>
    public interface IFunctionContextAccessor
    {
        /// <summary>
        /// Gets or sets the current <see cref="Microsoft.Azure.Functions.Worker.FunctionContext"/>.
        /// Returns <see langword="null" /> if there is no active <see cref="Microsoft.Azure.Functions.Worker.FunctionContext" />.
        /// </summary>
        FunctionContext FunctionContext { get; set; }
    }
}
