using MariBot.Common.Model.GpuWorker;
using MariBot.Worker;
using MariBot.Worker.CommandHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<JobHandler>();
builder.Services.AddSingleton<MagickImageHandler>();
builder.Services.AddSingleton<OpenCVHandler>();
builder.Services.AddSingleton<StableDiffusionTextVariantHandler>();
builder.Services.AddSingleton(x => new TraceExceptionLogger());

builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Logging.AddFile("maribot.log");
builder.Logging.AddEventLog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

WorkerGlobals.WorkerStatus = WorkerStatus.Ready;
app.Run();
