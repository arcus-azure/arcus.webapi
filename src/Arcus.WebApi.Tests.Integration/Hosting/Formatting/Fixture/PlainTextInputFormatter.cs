using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Arcus.WebApi.Tests.Integration.Hosting.Formatting.Fixture
{
    /// <summary>
    /// Represents an <see cref="IInputFormatter"/> that can parse the incoming HTTP request's body as plain text.
    /// </summary>
    public class PlainTextInputFormatter : InputFormatter
    {
        private const string PlainTextContentType = "text/plain";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PlainTextInputFormatter" /> class.
        /// </summary>
        public PlainTextInputFormatter()
        {
            SupportedMediaTypes.Add(PlainTextContentType);
        }

        /// <inheritdoc />
        public override bool CanRead(InputFormatterContext context)
        {
            string contentType = context.HttpContext.Request.ContentType;
            return contentType?.StartsWith(PlainTextContentType) == true;
        }

        /// <summary>
        /// Reads an object from the request body.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext" />.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that on completion deserializes the request body.</returns>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            HttpRequest request = context.HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                string content = await reader.ReadToEndAsync();
                return InputFormatterResult.Success(content);
            }
        }
    }
}