using MP3.Notificacoes.Worker;

// Esqueleto do Worker Service do MP3. Ele sobe, loga e não faz nada útil ainda:
// a missão (README) é transformar isto num consumidor confiável de PedidoCriado.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
