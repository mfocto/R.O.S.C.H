using System.Reflection;
using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.RTC;
using R.O.S.C.H.WS.RTC.Handler;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5178);
});

builder.Services.AddControllers();


builder.Services.AddSingleton<RTCConnectionManager>();

#region 메시지 핸들러 자동등록

var handlerTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IMessageHandler).IsAssignableFrom(t) 
                && t.IsClass 
                && !t.IsAbstract);

foreach (var handlerType in handlerTypes)
{
    builder.Services.AddSingleton(typeof(IMessageHandler), handlerType);
}

#endregion

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(1) });



app.Map("/ws/ros", appBuilder =>
{
    //app.UseMiddleware<ROSSocketMiddleware>();
});

app.Map("/ws/rtc", appBuilder =>
{
    app.UseMiddleware<RTCSocketMiddleware>();
});
app.Run();
