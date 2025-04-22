using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Bedrock.Irc;
using Microsoft.Extensions.Logging;
using System.Net;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
});

var client = new ClientBuilder()
    .UseSockets()
    .UseConnectionLogging(loggerFactory: loggerFactory)
    .Build();

await using var connection = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 6667));
Console.WriteLine($"Connected to {connection.LocalEndPoint}");

var protocol = new IrcProtocol();
var reader = connection.CreateReader();
var writer = connection.CreateWriter();

await writer.WriteAsync(protocol, "NICK Fredi");
await writer.WriteAsync(protocol, "USER Fredi 0 - Fredi");

while (true)
{
    var result = await reader.ReadAsync(protocol);

    if (result.IsCompleted)
    {
        Console.WriteLine("Breaking...");
        break;
    }

    var ircMessage = result.Message;

    if (ircMessage.Command.Equals("PING", StringComparison.OrdinalIgnoreCase))
    {
        var pong = ircMessage.Trailing;
        await writer.WriteAsync(protocol, $"PONG :{pong}");
    }

    Console.WriteLine(ircMessage);

    reader.Advance();
}
