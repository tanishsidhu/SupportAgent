using SupportAgent;
using SupportAgent.Agent;
using SupportAgent.Llm;
using SupportAgent.Tools;
using SupportAgent.Ui;

var dataRoot = Path.Combine(Directory.GetCurrentDirectory(), "data");

if (args is ["list-data"])
{
    ListData.Run();
    return 0;
}

if (args is ["review-queue"])
{
    var tools = new SupportTools(dataRoot);
    ReviewQueueView.Print(tools);
    return 0;
}

if (args.Length >= 3 && args[0] == "review")
{
    var tools = new SupportTools(dataRoot);
    var review = new ReviewService(tools);
    var ticketId = args[2];

    try
    {
        switch (args[1])
        {
            case "approve":
                review.Approve(ticketId);
                Console.WriteLine($"Approved {ticketId} and appended to resolved corpus.");
                break;
            case "amend" when args.Length >= 4:
                review.Amend(ticketId, args[3]);
                Console.WriteLine($"Amended and approved {ticketId}. Amendment logged.");
                break;
            case "reject":
                review.Reject(ticketId);
                Console.WriteLine($"Rejected {ticketId} → escalated.");
                break;
            default:
                Console.Error.WriteLine("Usage: review approve|reject <ticket-id>");
                Console.Error.WriteLine("       review amend <ticket-id> \"new answer text\"");
                return 1;
        }

        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

if (args is ["ui", ..])
{
    var port = 5050;

    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedPort))
        {
            port = parsedPort;
            i++;
        }
    }

    await ReviewUiHost.RunAsync(dataRoot, port);
    return 0;
}

if (args.Length >= 1 && (args[0] == "agent" || args[0] == "inbox" || args[0] == "schedule"))
{
    try
    {
        var settings = DeepSeekSettings.FromEnvironment();
        var apiCalls = new ApiCallTracker();
        var client = new DeepSeekClient(settings, apiCalls);
        var tools = new SupportTools(dataRoot);
        var maxTurns = 10;
        var demoForbiddenPost = false;
        var maxTickets = SchedulerOptions.DefaultMaxTicketsPerRun;
        var maxApiCalls = SchedulerOptions.DefaultMaxApiCalls;

        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--max-turns" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedTurns))
            {
                maxTurns = parsedTurns;
                i++;
            }
            else if (args[i] == "--max-tickets" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedTickets))
            {
                maxTickets = parsedTickets;
                i++;
            }
            else if (args[i] == "--max-api-calls" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedCalls))
            {
                maxApiCalls = parsedCalls;
                i++;
            }
            else if (args[i] == "--demo-forbidden-post")
            {
                demoForbiddenPost = true;
            }
        }

        var loop = new AgentLoop(client, tools, maxTurns, demoForbiddenPost);

        if (args[0] == "schedule")
        {
            var scheduler = new InboxScheduler(
                loop,
                tools,
                client,
                Path.Combine(dataRoot, "LOOP_STATE.json"),
                maxTickets,
                maxApiCalls);

            await scheduler.RunAsync();
            return 0;
        }

        if (args[0] == "inbox")
        {
            await InboxRunner.RunAsync(loop, tools);
            return 0;
        }

        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: dotnet run -- agent <ticket-id>");
            return 1;
        }

        var ticketId = args[1];
        await loop.RunAsync(ticketId);
        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine();
        Console.Error.WriteLine("  export DEEPSEEK_API_KEY=\"your-key-here\"");
        Console.Error.WriteLine("  dotnet run -- schedule");
        return 1;
    }
}

try
{
    var settings = DeepSeekSettings.FromEnvironment();
    ILlmClient client = new DeepSeekClient(settings);

    var reply = await client.CompleteChatAsync("Hello");
    Console.WriteLine(reply);
    return 0;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("  export DEEPSEEK_API_KEY=\"your-key-here\"");
    Console.Error.WriteLine("  dotnet run");
    return 1;
}
