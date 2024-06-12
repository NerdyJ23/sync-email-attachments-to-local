using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Serilog;
using SyncEmailAttachments.Application.Configuration.Email;
using SyncEmailAttachments.Application.Services.Emails;
using SyncEmailAttachments.Infrastructure.Data.EmailContext;

var builder = Host.CreateDefaultBuilder(args)

.ConfigureServices((context, services) =>
{
	services.BuildOptions(context.Configuration);
	services.AddServices();
	services.AddDatabases(context.Configuration);

}).UseSerilog((host, loggerConfig) =>
{
	loggerConfig.ReadFrom.Configuration(host.Configuration);
});

var host = builder.Build();

var dbFactory = host.Services.GetRequiredService<IDbContextFactory<EmailContext>>();
using var context = dbFactory.CreateDbContext();
context.Database.EnsureCreated();
context.Dispose();

//START
var handler = host.Services.GetRequiredService<EmailHandler>();
await handler.Sync();

host.Run();
