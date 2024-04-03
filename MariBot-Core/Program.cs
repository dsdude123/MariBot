using System.Web.Http.ExceptionHandling;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MariBot.Core;
using MariBot.Core.Services;
using MariBot.Services;
using OpenAI.Extensions;

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
builder.Services.AddSingleton<GoogleService>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<PricechartingService>();
builder.Services.AddSingleton<StaticTextResponseService>();
builder.Services.AddSingleton<TalkHubService>();
builder.Services.AddSingleton<TwitterService>();
builder.Services.AddSingleton<UrbanDictionaryService>();
builder.Services.AddSingleton<WikipediaService>();
builder.Services.AddSingleton<WolframAlphaService>();
builder.Services.AddSingleton<WorkerManagerService>();
builder.Services.AddSingleton<YahooFantasyService>();
builder.Services.AddSingleton<MediawikiSharp_API.Mediawiki>();
builder.Services.AddOpenAIService(settings => {
    settings.ApiKey = builder.Configuration["DiscordSettings:OpenAiApiKey"];
    settings.Organization = builder.Configuration["DiscordSettings:OpenAiOrganization"];
});

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

app.Run();
