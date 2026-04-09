using AssetHub.Service;
using AssetHub.Shared;
using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using AssetHub.Shared.Models;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<AssetHubSubscriberService>();

builder.Services.AddAssetHubClient(builder.Configuration);

// Load service bus configuration from appsettings and validate it at startup
builder.Services
    .AddOptions<ServiceBusSettings>()
    .Bind(builder.Configuration.GetSection(ServiceBusSettings.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.FullyQualifiedNamespace) &&
             !string.IsNullOrWhiteSpace(o.TopicName) &&
             !string.IsNullOrWhiteSpace(o.SubscriptionName),
        "ServiceBus configuration is incomplete.")
    .ValidateOnStart();
if(builder.Environment.IsDevelopment()) {
    // In development, use the ServiceBusClient constructor that doesn't require credentials, which allows for easier local development with tools like Azurite or the Azure Service Bus emulator.
     builder.Services.AddSingleton(sp =>
    {
        var serviceBusSettings = sp.GetRequiredService<IOptions<ServiceBusSettings>>().Value;
        Console.WriteLine($"Service bus conn string {serviceBusSettings.ConnectionString}");
        return new ServiceBusClient(serviceBusSettings.ConnectionString);
    });
} else {
    // In production, use DefaultAzureCredential to authenticate with Azure Service Bus, which supports various authentication methods including managed identities.
     builder.Services.AddSingleton(sp =>
    {
        var serviceBusSettings = sp.GetRequiredService<IOptions<ServiceBusSettings>>().Value;
        var fqns = serviceBusSettings.FullyQualifiedNamespace!;
        return new ServiceBusClient(fqns, new DefaultAzureCredential());
    });
}

builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapPost("/admin/dlq/replay", async (
    IDlqReplayService replayService,
    CancellationToken ct) => {
        var result = await replayService.ReplayAsync(ct);
        return Results.Ok(result);
    });
app.MapHealthChecks("/healthz");

app.MapGet("/admin/status", (
    SyncStatusStore status,
    IAssetHubCircuitState circuit) => {
        return Results.Ok(new {
            lastSyncUtc = status.LastSyncUtc,
            lastDlqReplayUtc = status.LastDlqReplayUtc,
            successfulToday = status.SuccessfulToday,
            failedToday = status.FailedToday,
            replayedToday = status.ReplayedToday,
            circuitState = circuit.State.ToString(),
            circuitLastOpenedAtUtc = circuit.LastOpenedAtUtc
        });
    });

app.MapGet("/", () => "Hello World!");

if (builder.Environment.IsDevelopment()) {
    AssetHubMockApi.StartApi();
}

app.Run();
