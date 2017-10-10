using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Kudu.Contracts.Tracing;
using Kudu.Core;
using Kudu.Core.Commands;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;
using System.Web.Http.Description;

namespace Kudu.Services.Commands
{
    public class CommandController : ApiController
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly ITracer _tracer;

        public CommandController(ICommandExecutor commandExecutor, ITracer tracer)
        {
            _commandExecutor = commandExecutor;
            _tracer = tracer;
        }

        /// <summary>
        /// Executes an arbitrary command line and return its output
        /// </summary>
        /// <param name="input">The command line to execute</param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(CommandResult))]
        public HttpResponseMessage ExecuteCommand(CommandInput input)
        {
            if (input == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            string command = input.command;
            string workingDirectory = input.dir;
            using (_tracer.Step("Executing " + command, new Dictionary<string, string> { { "CWD", workingDirectory } }))
            {
                try
                {
                    CommandResult result = _commandExecutor.ExecuteCommand(command, workingDirectory);
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                catch (CommandLineException ex)
                {
                    _tracer.TraceError(ex);
                    return Request.CreateResponse(HttpStatusCode.OK, new CommandResult { Error = ex.Error, ExitCode = ex.ExitCode });
                }
                catch (Exception ex)
                {
                    _tracer.TraceError(ex);
                    return Request.CreateResponse(HttpStatusCode.OK, new CommandResult { Error = ex.ToString(), ExitCode = -1 });
                }
            }
        }
    }
}

public class CommandInput
{
    public string command { get; set; }
    public string dir { get; set; }
}