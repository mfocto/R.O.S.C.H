using Microsoft.AspNetCore.Mvc;
using R.O.S.C.H.adapter;
using R.O.S.C.H.API.DTO;
using R.O.S.C.H.API.Service.Interface;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.API.Endpoints;

public static class ControlEndpoints
{
    public static void MapControlEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/control");

        group.MapPost("conveyor", async (
            [FromBody] ControlRequest request,
            [FromServices] OpcUaAdapter opcUaAdapter,
            [FromServices] IControlService controlService,
            [FromServices] ILogger<Program> logger
            ) =>
        {
            try
            {
                string tag = string.Empty;
                string alias = string.Empty;
                switch (request.Name)
                {
                    case "load" :
                        tag = "TargetSpeedLoad";
                        alias = "LOAD";
                        break;
                    case "main" : 
                        tag = "TargetSpeedMain";
                        alias = "MAIN";
                        break;
                    case "sort" : 
                        tag = "TargetSpeedSort";
                        alias = "SORT";
                        break;
                }
                
                await opcUaAdapter.WriteStateAsync(
                    CancellationToken.None,
                    "STM",
                    "Stm_yolo",
                    tag,
                    request.Value);

                await controlService.ControlLogProcess(request, alias);
                
                return Results.Ok(new { success = true });
            }
            catch (Exception e)
            {
                logger.LogError("컨베이어 속도 변경 중 오류"+e.Message);
                return Results.Problem(detail:e.Message, statusCode:500);
            }
        });

        group.MapPost("process", async (
            [FromBody] ControlRequest request,
            [FromServices] OpcUaAdapter opcUaAdapter,
            [FromServices] IControlService controlService,
            [FromServices] ILogger<Program> logger
        ) =>
        {
            try
            {
                // 제어 명령은 두군데 다 보내야 함(stm, agv)
                await opcUaAdapter.WriteStateAsync(
                    CancellationToken.None,
                    "ModbusTCP",
                    "ESP32_01",
                    "Control",
                    request.Value);

                long state = request.Value switch
                {
                    "RUN" => 0,
                    "STOP" => 1,
                    "EMERGENCY STOP" => 2,
                    "CALL ADMIN" => 999,
                    _ => throw new ArgumentException()
                };

                await opcUaAdapter.WriteStateAsync(
                    CancellationToken.None,
                    "STM",
                    "Stm_yolo",
                    "TargetState",
                    state);
                
                await controlService.ControlLogProcess(request);
                
                return Results.Ok(new { success = true });
            }
            catch (Exception e)
            {
                logger.LogError("제어 명령 전송 중 오류" + e.Message);
                return Results.Problem(detail: e.Message, statusCode: 500);
            }
        });
    }
}