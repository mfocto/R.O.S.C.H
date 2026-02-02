using Microsoft.AspNetCore.Mvc;
using R.O.S.C.H.adapter;
using R.O.S.C.H.API.DTO;

namespace R.O.S.C.H.API.Endpoints;

public static class ControlEndpoints
{
    public static void MapControlEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/control");

        group.MapPost("conveyor", async (
            [FromBody] ControlRequest request,
            [FromServices] OpcUaAdapter opcUaAdapter,
            [FromServices] ILogger<Program> logger
            ) =>
        {
            try
            {
                string tag = request.Name switch
                {
                    "load" => "TargetSpeedLoad",
                    "main" => "TargetSpeedMain",
                    "sort" => "TargetSpeedSort",
                    _ => throw new ArgumentException("잘못된 컨베이어 ID")
                };

                await opcUaAdapter.WriteStateAsync(
                    CancellationToken.None,
                    "STM",
                    "Stm_yolo",
                    tag,
                    request.Value);

                return Results.Ok(new { success = true });
            }
            catch (Exception e)
            {
                logger.LogError("컨베이어 속도 변경 중 오류"+e.Message);
                return Results.Problem(detail:e.Message, statusCode:500);
            }
        });
        
        group.MapPost("process", async (
            [FromBody]ControlRequest request,
            [FromServices] OpcUaAdapter opcUaAdapter,
            [FromServices] ILogger<Program> logger
            ) =>
        {
            try
            {
                
            }
            catch (Exception e)
            {
                logger.LogError("제어 명령 전송 중 오류" + e.Message);
                return Results.Problem(detail:e.Message, statusCode:500);
            }
        })
    }
}