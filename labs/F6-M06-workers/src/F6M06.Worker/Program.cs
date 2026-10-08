using F6M06.Worker;
using F6M06.Worker.Saude;

// (PRONTO) Worker Service do OrderFlow: expiração de pedidos + processamento de notificações.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkersDoOrderFlow(builder.Configuration);

// Em container, o orquestrador manda SIGTERM e espera um prazo (Kubernetes: terminationGracePeriodSeconds,
// 30 s por padrão). O ShutdownTimeout precisa caber DENTRO desse prazo.
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(20));

using var host = builder.Build();
await host.RunAsync();

// Com BackgroundServiceExceptionBehavior.StopHost, um worker que morre PARA o host — mas o processo
// sai com código 0, como se tudo tivesse dado certo. Devolver ≠ 0 faz o orquestrador saber que falhou.
return host.Services.GetRequiredService<MonitorDeWorkers>().AlgumaFalhaFatal ? 1 : 0;
