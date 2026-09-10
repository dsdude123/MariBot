using System.Web.Http.ExceptionHandling;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MariBot.Core;
using MariBot.Core.Models.Config;
using MariBot.Core.Services;
using MariBot.Services;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Moderations;

var builder = WebApplication.CreateBuilder(args);
var clientConfig = new DiscordSocketConfig()
{
    GatewayIntents = GatewayIntents.All
};
var discordClient = new DiscordSocketClient(clientConfig);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMvc();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Services.AddSingleton(discordClient);
builder.Services.AddSingleton<DiscordBotService>();
builder.Services.AddHostedService<DiscordBotService>(provider => provider.GetService<DiscordBotService>());
builder.Services.AddSingleton<DynamicConfigService>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<CommandHandlingService>();
builder.Services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
builder.Services.AddSingleton(x => new TraceExceptionLogger());

builder.Services.AddSingleton<BooruService>();
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<ElectionService>();
builder.Services.AddSingleton<FluxService>();
builder.Services.AddSingleton<GoogleService>();
builder.Services.AddSingleton<GrokService>();
builder.Services.AddSingleton<IgdbService>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<PricechartingService>();
builder.Services.AddSingleton<SleeperPlayerCache>();
builder.Services.AddSingleton<SleeperService>();
builder.Services.AddSingleton<StaticTextResponseService>();
builder.Services.AddSingleton<TalkHubService>();
builder.Services.AddSingleton<TwitterService>();
builder.Services.AddSingleton<UrbanDictionaryService>();
builder.Services.AddSingleton<WikipediaService>();
builder.Services.AddSingleton<WolframAlphaService>();

// Worker pipeline. WorkerSettings is bound once and shared, so the registry,
// the breaker and the metrics pruner all agree on the same numbers.
var workerSettings = builder.Configuration.GetSection(WorkerSettings.SectionName).Get<WorkerSettings>()
                     ?? new WorkerSettings();
builder.Services.AddSingleton(workerSettings);
builder.Services.AddSingleton<WorkerAuthenticator>();
builder.Services.AddSingleton<WorkerRegistry>();
builder.Services.AddSingleton<JobQueue>();
builder.Services.AddSingleton<JobMetricsService>();
// Pooled handlers rather than a new HttpClient per dispatch, which used to
// leak a socket for every job.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<WorkerManagerService>();
builder.Services.AddSingleton<YouTubeDlService>();
builder.Services.AddSingleton<YahooFantasyService>();
builder.Services.AddSingleton<MediawikiSharp_API.Mediawiki>();

builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Logging.AddFile(Environment.GetEnvironmentVariable("MariBot__LogPath") ?? "maribot.log");
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

var app = builder.Build();

// Resolve eagerly so the transaction poller and the player-catalog refresh actually
// start at boot. Both attach their timers in their constructors, and a singleton is
// not constructed until something asks for it — which is why the equivalent Yahoo
// poller never ran until someone happened to invoke a fantasy command.
app.Services.GetRequiredService<SleeperPlayerCache>();
app.Services.GetRequiredService<SleeperService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
