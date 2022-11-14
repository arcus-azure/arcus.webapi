using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class XunitSerilogSink : ILogEventSink
    {
        private readonly ITestOutputHelper _ouputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitSerilogSink" /> class.
        /// </summary>
        public XunitSerilogSink(ITestOutputHelper ouputWriter)
        {
            _ouputWriter = ouputWriter;
        }
        public void Emit(LogEvent logEvent)
        {
            _ouputWriter.WriteLine(logEvent.RenderMessage(formatProvider: null));
        }
    }
}
