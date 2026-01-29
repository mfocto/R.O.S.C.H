using System.Reflection;
using Npgsql;
using R.O.S.C.H.adapter;
using R.O.S.C.H.API.Endpoints;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository;
using R.O.S.C.H.Database.Repository.Interface;
using R.O.S.C.H.Worker;
using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.RTC;
using R.O.S.C.H.WS.RTC.Handler;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5178");


builder.Services.AddControllers();


builder.Services.AddSingleton<RTCConnectionManager>();
builder.Services.AddSingleton<OpcUaAdapter>();
builder.Services.AddHostedService<StatePollingWorker>();
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

#region Repository 자동등록
var repositories = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"));

foreach (var repo in repositories) {
    var inf = repo.GetInterfaces().FirstOrDefault(i => i.Name == $"I{repo.Name}");

    if (inf != null) {
        builder.Services.AddScoped(inf, repo);
    }
}
#endregion

#region service 등록
var services = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"));

foreach (var service in services)
{
    var inf = service.GetInterfaces().FirstOrDefault(i => i.Name == $"I{service.Name}");
    
    if  (inf != null) {
        builder.Services.AddScoped(inf, service);
    }
}
#endregion


var app = builder.Build();

// 테스트 사용자 초기화
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    
    var userRepo = serviceProvider.GetRequiredService<IUserRepository>();
    
    // 기존 사용자 확인
    var existingAdmin = await userRepo.GetUserByUserNameAsync(conn, "admin");
    
    if (existingAdmin == null)
    {
        // 테스트 사용자 생성
        using var tx = await conn.BeginTransactionAsync();
        var testAdmin = new User
        {
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword("admin"),  // 실제로는 해시화 필요
            Role = "ADMIN",
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        int result = await userRepo.CreateAsync(conn, tx, testAdmin);
        Console.WriteLine("admin 생성 : " +  result);
        await tx.CommitAsync();
    }
    
    var existingUser = await userRepo.GetUserByUserNameAsync(conn, "user");
    
    if (existingUser == null)
    {
        // 테스트 사용자 생성
        using var tx = await conn.BeginTransactionAsync();
        var testUser = new User
        {
            Username = "user",
            Password = BCrypt.Net.BCrypt.HashPassword("user"),  // 실제로는 해시화 필요
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        int result = await userRepo.CreateAsync(conn, tx, testUser);
        Console.WriteLine("user 생성 : " +  result);
        await tx.CommitAsync();
    }
}


app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(1) });

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapAuthEndpoints();



app.Map("/ws/rtc", appBuilder =>
{
    app.UseMiddleware<RTCSocketMiddleware>();
});
app.Run();
