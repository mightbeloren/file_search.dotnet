using indexer;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore;
using indexer.Models;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<indexer.Models.IndexerContext>(options=>options.UseSqlite(builder.Configuration.GetConnectionString("default")));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
