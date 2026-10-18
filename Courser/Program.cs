using Courser;
using Courser.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddCourserServices(builder.Configuration);
builder.Services.AddHostedService<ConsoleWorker>();

builder.Configuration.AddCommandLine(["--course=MA-TINF26IT"]);

var app = builder.Build();


app.Run();