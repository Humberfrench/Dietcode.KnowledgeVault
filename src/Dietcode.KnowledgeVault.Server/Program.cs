using Dietcode.KnowledgeVault.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddVault(builder.Configuration);
var app = builder.Build();

app.Run();