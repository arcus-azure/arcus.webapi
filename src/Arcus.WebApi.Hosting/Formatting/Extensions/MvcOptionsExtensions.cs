using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Extensions on reusable JSON formatting configurations.
    /// </summary>
    public static class MvcOptionsExtensions
    {
        /// <summary>
        /// Restrict the MVC formatting to only allow JSON formatting during receiving and sending.
        /// </summary>
        /// <param name="options">The MVC options where the input and output formatting will be restricted to JSON formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MvcOptions OnlyAllowJsonFormatting(this MvcOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires MVC options to restrict the formatting to only JSON formatting");
            }
            
            IInputFormatter[] allButJsonInputFormatters = 
                options.InputFormatters.Where(formatter => !(formatter is SystemTextJsonInputFormatter))
                       .ToArray();

            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }

            // Removing for text/plain, see https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-3.0#special-case-formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();

            return options;
        }

        /// <summary>
        /// Configure the MVC JSON formatters for both receiving and sending.
        /// </summary>
        /// <param name="options">The MVC options where the JSON formatters will be configured.</param>
        /// <param name="configureOptions">The function to configure the input and output JSON formatters in the MVC <paramref name="options"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="configureOptions"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(MvcCoreMvcBuilderExtensions) + "." + nameof(MvcCoreMvcBuilderExtensions.AddJsonOptions) + " instead to configure the JSON formatters")]
        public static MvcOptions ConfigureJsonFormatting(this MvcOptions options, Action<JsonSerializerOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires MVC options to configure the JSON formatters");
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions), "Requires a function to configure the JSON formatters in the MVC options");
            }

            SystemTextJsonInputFormatter[] onlyJsonInputFormatters = 
                options.InputFormatters.OfType<SystemTextJsonInputFormatter>()
                       .ToArray();
            
            foreach (SystemTextJsonInputFormatter inputFormatter in onlyJsonInputFormatters)
            {
                configureOptions(inputFormatter.SerializerOptions);
            }

            SystemTextJsonOutputFormatter[] onlyJsonOutputFormatters = 
                options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>()
                       .ToArray();
            
            foreach (SystemTextJsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                configureOptions(outputFormatter.SerializerOptions);
            }

            return options;
        }
    }
}
