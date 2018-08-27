using System.Web.Http;
using WebActivatorEx;
using Kudu.Services.Web;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using System.Web.Http.Description;
using System.Collections.Generic;
using System.Linq;
using System.Net;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace Kudu.Services.Web
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.Schemes(new[] { "https" });
                        c.SingleApiVersion("v1", "Kudu.Services.Web");
                        c.PrettyPrint();
                        c.OperationFilter<AddFileParamTypes>();
                        c.OperationFilter<NoReservedParam>();
                        c.OperationFilter<AcceptedResponseFilter>();
                        c.OperationFilter<QueryParamCantBeObject>();
                    })
                .EnableSwaggerUi(c =>
                    {
                    });
        }
    }
}

public class AcceptedResponseFilter : IOperationFilter
{
    public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
        if (operation.operationId == "Deployment_GetResult" || operation.operationId.EndsWith("PushDeploy"))  // controller and action name
        {
            var response = operation.responses.First().Value;
            operation.responses.Add(((int)HttpStatusCode.Accepted).ToString(), new Response() {
                schema = response.schema,
                description = HttpStatusCode.Accepted.ToString()
            });
            if (operation.operationId.EndsWith("PushDeploy"))
            {
                operation.responses.Remove(((int)HttpStatusCode.NoContent).ToString());
                operation.responses.Add(((int)HttpStatusCode.OK).ToString(), new Response()
                {
                    schema = response.schema,
                    description = HttpStatusCode.OK.ToString()
                });
            }
        } else if (operation.operationId == "Function_Delete")  // controller and action name
        {
            var response = operation.responses.First().Value;
            operation.responses.Add(((int)HttpStatusCode.NoContent).ToString(), new Response()
            {
                schema = response.schema,
                description = HttpStatusCode.NoContent.ToString()
            });
        } else if (operation.operationId.EndsWith("_GetItem"))  // controller and action name
        {
            var response = operation.responses.First().Value;
            operation.responses.Add(((int)HttpStatusCode.OK).ToString(), new Response()
            {
                schema = response.schema,
                description = HttpStatusCode.OK.ToString()
            });
            operation.responses.Remove(((int)HttpStatusCode.NoContent).ToString());
        } else if (operation.operationId.EndsWith("_PutItem"))
        {
            var response = operation.responses.First().Value;
            operation.responses.Add(((int)HttpStatusCode.Created).ToString(), new Response()
            {
                schema = response.schema,
                description = HttpStatusCode.Created.ToString()
            });
            operation.responses.Add(((int)HttpStatusCode.NoContent).ToString(), new Response()
            {
                schema = response.schema,
                description = HttpStatusCode.NoContent.ToString()
            });
        }
    }
}

public class AddFileParamTypes : IOperationFilter
{
    public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
        var fileParam = new Parameter
        {
            name = "file",
            @in = "body",
            required = true,
            schema = new Schema
            {
                type = "object",
                format = "file"
            }
        };
        var consumes = "application/octet-stream";
        if (operation.operationId.EndsWith("PushDeploy") || operation.operationId.EndsWith("_PutItem"))
        {
            operation.consumes.Add(consumes);
            operation.parameters.Insert(0, fileParam);
        }
    }
}

public class NoReservedParam : IOperationFilter
{
    public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
        if (operation.parameters != null)
        {
            // Remove any parameters named 'arguments' because it's a reserved word in JavaScript and won't work anyways
            operation.parameters = operation.parameters.Where(p => p.name != "arguments").ToArray();
        }
    }
}

public class QueryParamCantBeObject : IOperationFilter
{
    public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
        if (operation.parameters != null)
        {
            foreach (var p in operation.parameters)
            {
                if (p.@in == "query" && p.type == "object")
                {
                    p.type = "string";
                }
            }
        }
    }
}