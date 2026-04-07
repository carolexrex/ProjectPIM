using Platform.Api.Infrastructure.ErrorHandling;
using Platform.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCatalogPersistence(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();

app.Run();
