using ServicoPix.Worker;
using NexusBus.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNexusBus(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
