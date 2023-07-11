using keyholder.Interfaces;
using keyholder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISigningService, SigningService>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var serviceProvider = builder.Services.BuildServiceProvider();

var signingService = serviceProvider.GetService<ISigningService>();
signingService.Init();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();

app.Run();
