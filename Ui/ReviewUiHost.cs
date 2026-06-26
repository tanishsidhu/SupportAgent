using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SupportAgent.Agent;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Ui;

public static class ReviewUiHost
{
    private static readonly SemaphoreSlim RunBatchLock = new(1, 1);

    public static async Task RunAsync(string dataRoot, int port = 5050)
    {
        var tools = new SupportTools(dataRoot);
        var review = new ReviewService(tools);
        var loopStatePath = Path.Combine(dataRoot, "LOOP_STATE.json");
        var runState = new AgentRunState();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");

        var app = builder.Build();

        app.MapGet("/", () => Results.Content(MonitorPage.Html, "text/html"));
        app.MapGet("/review", () => Results.Content(ReviewPage.Html, "text/html"));

        app.MapGet("/api/agent/status", () =>
        {
            var snapshot = AgentDashboard.Build(tools, loopStatePath, runState);
            return Results.Json(snapshot);
        });

        app.MapGet("/api/queue", () =>
        {
            var items = ReviewQueueBuilder.GetPending(tools)
                .Select(item => new
                {
                    ticketId = item.TicketId,
                    subject = item.Subject,
                    question = item.Question,
                    draft = item.Draft,
                    pipeline = item.Pipeline,
                });

            return Results.Json(items);
        });

        app.MapPost("/api/review/{ticketId}/approve", (string ticketId) =>
        {
            if (!TicketId.IsValid(ticketId))
            {
                return Results.BadRequest("Invalid ticket id.");
            }

            try
            {
                var updated = review.Approve(ticketId);
                return Results.Json(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        app.MapPost("/api/review/{ticketId}/amend", async (string ticketId, HttpRequest request) =>
        {
            if (!TicketId.IsValid(ticketId))
            {
                return Results.BadRequest("Invalid ticket id.");
            }

            try
            {
                var body = await JsonSerializer.DeserializeAsync<AmendRequest>(
                    request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (string.IsNullOrWhiteSpace(body?.Answer))
                {
                    return Results.BadRequest("Answer is required.");
                }

                var updated = review.Amend(ticketId, body.Answer);
                return Results.Json(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        app.MapPost("/api/review/{ticketId}/reject", (string ticketId) =>
        {
            if (!TicketId.IsValid(ticketId))
            {
                return Results.BadRequest("Invalid ticket id.");
            }

            try
            {
                var updated = review.Reject(ticketId);
                return Results.Json(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = RunBatchAsync(tools, loopStatePath, runState);
        });

        Console.WriteLine($"Monitor UI at http://localhost:{port}");
        Console.WriteLine($"Review queue at http://localhost:{port}/review");
        Console.WriteLine("Agent batch starts automatically on launch.");

        await app.RunAsync();
    }

    private static async Task RunBatchAsync(
        SupportTools tools,
        string loopStatePath,
        AgentRunState runState)
    {
        if (!await RunBatchLock.WaitAsync(0))
        {
            return;
        }

        runState.IsRunning = true;
        runState.Error = null;
        runState.Log = "Starting agent batch…\n";

        try
        {
            var settings = DeepSeekSettings.FromEnvironment();
            var apiCalls = new ApiCallTracker();
            var client = new DeepSeekClient(settings, apiCalls);
            var loop = new AgentLoop(client, tools);
            var log = new StringWriter();
            var scheduler = new InboxScheduler(
                loop,
                tools,
                client,
                loopStatePath,
                log: log);

            var result = await scheduler.RunAsync();
            runState.Log = log.ToString();

            if (result.Processed == 0 && result.Skipped > 0)
            {
                runState.Log += "\nNo pending tickets — inbox already processed.";
            }
        }
        catch (InvalidOperationException ex)
        {
            runState.Error = ex.Message;
            runState.Log += $"\n{ex.Message}";
            Console.Error.WriteLine(ex.Message);
        }
        finally
        {
            runState.IsRunning = false;
            RunBatchLock.Release();
        }
    }

    private sealed record AmendRequest(string Answer);
}
