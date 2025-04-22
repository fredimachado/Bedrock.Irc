using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bedrock.Irc.Tests;

public class IrcProtocolTests
{
    [Fact]
    public async Task ReadMessagesWorks()
    {
        var options = new PipeOptions(useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        await using var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);
        var raw = ":Fredi!FrediMachado@172.17.0.1 PRIVMSG Fredi :test test";
        await connection.Application.Output.WriteAsync(Encoding.UTF8.GetBytes($"{raw}\r\n"));
        connection.Application.Output.Complete();

        var protocol = new IrcProtocol();
        var reader = connection.CreateReader();

        var result = await reader.ReadAsync(protocol);

        Assert.Equal("Fredi", result.Message.From);
        Assert.Equal("FrediMachado", result.Message.User);
        Assert.Equal("172.17.0.1", result.Message.Host);
        Assert.Equal("PRIVMSG", result.Message.Command);
        Assert.Equal("Fredi", result.Message.Parameters[0]);
        Assert.Equal("test test", result.Message.Trailing);
        Assert.Equal(raw, result.Message.Raw);

        reader.Advance();

        result = await reader.ReadAsync(protocol);

        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task ReadMultipleMessagesWorks()
    {
        var options = new PipeOptions(useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        await using var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);
        var raw1 = ":Fredi!FrediMachado@172.17.0.1 PRIVMSG Fredi :test test";
        var raw2 = ":Fredi!FrediMachado@172.17.0.1 PRIVMSG Test :Testing :)";
        await connection.Application.Output.WriteAsync(Encoding.UTF8.GetBytes($"{raw1}\r\n{raw2}\r\n"));
        connection.Application.Output.Complete();

        var protocol = new IrcProtocol();
        var reader = connection.CreateReader();

        var result = await reader.ReadAsync(protocol);

        Assert.Equal("Fredi", result.Message.From);
        Assert.Equal("FrediMachado", result.Message.User);
        Assert.Equal("172.17.0.1", result.Message.Host);
        Assert.Equal("PRIVMSG", result.Message.Command);
        Assert.Equal("Fredi", result.Message.Parameters[0]);
        Assert.Equal("test test", result.Message.Trailing);
        Assert.Equal(raw1, result.Message.Raw);

        reader.Advance();

        result = await reader.ReadAsync(protocol);

        Assert.Equal("Fredi", result.Message.From);
        Assert.Equal("FrediMachado", result.Message.User);
        Assert.Equal("172.17.0.1", result.Message.Host);
        Assert.Equal("PRIVMSG", result.Message.Command);
        Assert.Equal("Test", result.Message.Parameters[0]);
        Assert.Equal("Testing :)", result.Message.Trailing);
        Assert.Equal(raw2, result.Message.Raw);

        reader.Advance();

        result = await reader.ReadAsync(protocol);

        Assert.True(result.IsCompleted);
    }
}

// From: https://github.com/davidfowl/BedrockFramework/blob/main/src/Bedrock.Framework/Infrastructure/DuplexPipe.cs
internal class DuplexPipe : IDuplexPipe
{
    public DuplexPipe(PipeReader reader, PipeWriter writer)
    {
        Input = reader;
        Output = writer;
    }

    public PipeReader Input { get; }

    public PipeWriter Output { get; }

    public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

        return new DuplexPipePair(applicationToTransport, transportToApplication);
    }

    // This class exists to work around issues with value tuple on .NET Framework
    public readonly struct DuplexPipePair
    {
        public IDuplexPipe Transport { get; }
        public IDuplexPipe Application { get; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }
}
