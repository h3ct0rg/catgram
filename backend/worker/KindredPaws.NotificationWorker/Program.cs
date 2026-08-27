using KindredPaws.NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IWorkerEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<NotificationConsumer>();
await builder.Build().RunAsync();
