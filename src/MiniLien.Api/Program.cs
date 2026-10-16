using MiniLien.Api.Liens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MiniLienOptions>(builder.Configuration.GetSection("MiniLien"));
builder.Services.AddSingleton<LienStore>();

var app = builder.Build();

app.MapLiens();

app.Run();

// Rend la classe Program visible pour les tests d'intégration (WebApplicationFactory).
public partial class Program { }
