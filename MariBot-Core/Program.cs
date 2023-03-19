using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MariBot.Core.Services;

var builder = WebApplication.CreateBuilder(args);
var clientConfig = new DiscordSocketConfig()
{
    GatewayIntents = GatewayIntents.All
};
var discordClient = new DiscordSocketClient(clientConfig);

// Add services to the container.

builder.Services.AddControllers();
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

builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<PricechartingService>();
builder.Services.AddSingleton<StaticTextResponseService>();

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
