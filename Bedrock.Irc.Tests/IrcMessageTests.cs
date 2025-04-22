using System.Buffers;
using System.Text;
using Xunit;

namespace Bedrock.Irc.Tests;

public class IrcMessageTests
{
    [Fact]
    public void TestMessage_FullPrefix()
    {
        var message = ":Fredi!FrediMachado@172.17.0.1 PRIVMSG Fredi_ :test";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("Fredi", ircMessage.From);
        Assert.Equal("FrediMachado", ircMessage.User);
        Assert.Equal("172.17.0.1", ircMessage.Host);
    }

    [Fact]
    public void TestMessagePrefix_FromAndHost()
    {
        var message = ":Fredi@172.17.0.1 PRIVMSG Fredi_ :test";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("Fredi", ircMessage.From);
        Assert.Null(ircMessage.User);
        Assert.Equal("172.17.0.1", ircMessage.Host);
    }

    [Fact]
    public void TestMessagePrefix_FromAndUser()
    {
        var message = ":Fredi!FrediMachado PRIVMSG Fredi_ :test";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("Fredi", ircMessage.From);
        Assert.Equal("FrediMachado", ircMessage.User);
        Assert.Null(ircMessage.Host);
    }

    [Fact]
    public void TestMessageParameters()
    {
        var message = ":Fredi!FrediMachado PRIVMSG Fredi_ :test";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("PRIVMSG", ircMessage.Command);
        Assert.Equal("Fredi_", ircMessage.Parameters[0]);
        Assert.Equal("test", ircMessage.Parameters[1]);
        Assert.Equal("test", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessageParameters_TrailingWithSpaces()
    {
        var message = ":Fredi!FrediMachado PRIVMSG Fredi_ :test test";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("PRIVMSG", ircMessage.Command);
        Assert.Equal("Fredi_", ircMessage.Parameters[0]);
        Assert.Equal("test test", ircMessage.Parameters[1]);
        Assert.Equal("test test", ircMessage.Trailing);
    }

    [Fact]
    public void TestWelcome()
    {
        var message = ":df762e2aa5da.example.com 001 Fredi :Welcome to the Omega IRC Network Fredi!Fredi@172.19.0.1";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("df762e2aa5da.example.com", ircMessage.From);
        Assert.Equal("001", ircMessage.Command);
        Assert.Equal("Fredi", ircMessage.Parameters[0]);
        Assert.Equal("Welcome to the Omega IRC Network Fredi!Fredi@172.19.0.1", ircMessage.Trailing);
    }
}