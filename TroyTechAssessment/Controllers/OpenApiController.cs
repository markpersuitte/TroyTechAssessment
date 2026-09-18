using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TroyTechAssessment.Controllers;

[ApiController]
[Route("api/openapi.json")]
[AllowAnonymous]
public sealed class OpenApiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return new JsonResult(new
        {
            openapi = "3.0.3",
            info = new { title = "TroyTechAssessment list API", version = "1.0.0" },
            paths = new Dictionary<string, object>
            {
                ["/Index?handler=UnitsJson"] = new
                {
                    get = new
                    {
                        summary = "Paged and sorted visible units",
                        parameters = PagingParameters(),
                        responses = new { _200 = ResponseSchema("UnitPage") }
                    }
                },
                ["/Applications?handler=Json"] = new
                {
                    get = new
                    {
                        summary = "Paged and sorted applications visible to the current user",
                        parameters = PagingParameters(),
                        responses = new { _200 = ResponseSchema("ApplicationPage") }
                    }
                }
            },
            components = new
            {
                schemas = new
                {
                    UnitPage = new { type = "object", properties = new { items = new { type = "array" }, page = new { type = "integer" }, pageSize = new { type = "integer" }, totalItems = new { type = "integer" } } },
                    ApplicationPage = new { type = "object", properties = new { items = new { type = "array" }, page = new { type = "integer" }, pageSize = new { type = "integer" }, totalItems = new { type = "integer" } } }
                }
            }
        });
    }

    private static object[] PagingParameters() =>
    [
        new { name = "page", @in = "query", schema = new { type = "integer", minimum = 1 } },
        new { name = "pageSize", @in = "query", schema = new { type = "integer", @enum = new[] { 10, 25, 50 } } },
        new { name = "sort", @in = "query", schema = new { type = "string" } },
        new { name = "direction", @in = "query", schema = new { type = "string", @enum = new[] { "asc", "desc" } } }
    ];

    private static object ResponseSchema(string schemaName) =>
        new { description = "Successful response", content = new { application_json = new { schema = new { @ref = $"#/components/schemas/{schemaName}" } } } };
}
