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
builder.Services.AddSingleton<EasyOcrHandler>();
builder.Services.AddSingleton(x => new TraceExceptionLogger());

// Self-registration with Core. Bound once and shared so the registration loop
// and the job deadline agree on the same settings.
var workerSettings = builder.Configuration.GetSection(WorkerSettings.SectionName).Get<WorkerSettings>()
                     ?? new WorkerSettings();
builder.Services.AddSingleton(workerSettings);
builder.Services.AddHttpClient();
builder.Services.AddHostedService<WorkerRegistrationService>();

builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
// Same knob MariBot.Core uses, so the container image can point the file sink
// at a mounted volume instead of writing inside the image.
builder.Logging.AddFile(Environment.GetEnvironmentVariable("MariBot__LogPath") ?? "maribot.log");

// The Windows event log has no Linux counterpart, and its provider throws on
// first write rather than degrading, so it is only added where it exists.
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

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
