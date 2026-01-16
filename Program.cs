using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.Ros;
using R.O.S.C.H.WS.RTC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(1) });


app.Map("/ws/ros", appBuilder =>
{
    app.UseMiddleware<ROSSocketMiddleware>();
});

app.Map("/ws/rtc", appBuilder =>
{
    app.UseMiddleware<RTCSocketMiddleware>();
});
app.Run();
