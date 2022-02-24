using System.Buffers;
using System.Text;
using Xunit;

namespace Bedrock.Irc.Tests;

// Test cases from: https://github.com/ircdocs/parser-tests/blob/master/tests/msg-split.yaml
public class IrcMessageSplitTests
{
    [Fact]
    public void TestMessage_Simple()
    {
        var message = "foo bar baz asdf";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("asdf", ircMessage.Parameters[2]);
    }

    [Fact]
    public void TestMessage_WithPrefix()
    {
        var message = ":coolguy foo bar baz asdf";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("asdf", ircMessage.Parameters[2]);
    }

    [Fact]
    public void TestMessage_WithTrailingParam()
    {
        var message = "foo bar baz :asdf quux";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("asdf quux", ircMessage.Parameters[2]);
        Assert.Equal("asdf quux", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithTrailingParamEmpty()
    {
        var message = "foo bar baz :";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("", ircMessage.Parameters[2]);
        Assert.Equal("", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithTrailingParamStartingWithColon()
    {
        var message = "foo bar baz ::asdf";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal(":asdf", ircMessage.Parameters[2]);
        Assert.Equal(":asdf", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithSourceAndTrailingParam()
    {
        var message = ":coolguy foo bar baz :asdf quux";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("asdf quux", ircMessage.Parameters[2]);
        Assert.Equal("asdf quux", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithSourceAndTrailingParamWithSpaces()
    {
        var message = ":coolguy foo bar baz :  asdf quux ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("  asdf quux ", ircMessage.Parameters[2]);
        Assert.Equal("  asdf quux ", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_CommandWithSourceAndTrailingParam()
    {
        var message = ":coolguy PRIVMSG bar :lol :) ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("PRIVMSG", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("lol :) ", ircMessage.Parameters[1]);
        Assert.Equal("lol :) ", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithSourceAndTrailingParamEmpty()
    {
        var message = ":coolguy foo bar baz :";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("", ircMessage.Parameters[2]);
        Assert.Equal("", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithSourceAndTrailingParamWithWhitespace()
    {
        var message = ":coolguy foo bar baz :  ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("  ", ircMessage.Parameters[2]);
        Assert.Equal("  ", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_CommandWithSimpleLastParam()
    {
        var message = ":src JOIN #chan";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("src", ircMessage.From);
        Assert.Equal("JOIN", ircMessage.Command);
        Assert.Equal("#chan", ircMessage.Parameters[0]);
        Assert.Equal("#chan", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_CommandWithColonSeparatedLastParam()
    {
        var message = ":src JOIN :#chan";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("src", ircMessage.From);
        Assert.Equal("JOIN", ircMessage.Command);
        Assert.Equal("#chan", ircMessage.Parameters[0]);
        Assert.Equal("#chan", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_CommandWithoutLastParam()
    {
        var message = ":src AWAY";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("src", ircMessage.From);
        Assert.Equal("AWAY", ircMessage.Command);
    }

    [Fact]
    public void TestMessage_CommandWithoutLastParamWithWhitespace()
    {
        var message = ":src AWAY ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("src", ircMessage.From);
        Assert.Equal("AWAY", ircMessage.Command);
    }

    [Fact]
    public void TestMessage_TabIsNotConsideredWhitespace()
    {
        var message = ":cool\tguy foo bar baz";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("cool\tguy", ircMessage.From);
        Assert.Equal("foo", ircMessage.Command);
        Assert.Equal("bar", ircMessage.Parameters[0]);
        Assert.Equal("baz", ircMessage.Parameters[1]);
        Assert.Equal("baz", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithWeirdControlCodesInPrefix()
    {
        var message = ":coolguy!ag@net\x035w\x03ork.admin PRIVMSG foo :bar baz";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("ag", ircMessage.User);
        Assert.Equal("net\x035w\x03ork.admin", ircMessage.Host);
        Assert.Equal("PRIVMSG", ircMessage.Command);
        Assert.Equal("foo", ircMessage.Parameters[0]);
        Assert.Equal("bar baz", ircMessage.Parameters[1]);
        Assert.Equal("bar baz", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_WithWeirdControlCodesInPrefix2()
    {
        var message = ":coolguy!~ag@n\x02et\x0305w\x0fork.admin PRIVMSG foo :bar baz";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("coolguy", ircMessage.From);
        Assert.Equal("~ag", ircMessage.User);
        Assert.Equal("n\x02et\x0305w\x0fork.admin", ircMessage.Host);
        Assert.Equal("PRIVMSG", ircMessage.Command);
        Assert.Equal("foo", ircMessage.Parameters[0]);
        Assert.Equal("bar baz", ircMessage.Parameters[1]);
        Assert.Equal("bar baz", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_JustCommand()
    {
        var message = "COMMAND";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("COMMAND", ircMessage.Command);
    }

    [Fact]
    public void TestMessage_BrokenMessagesFromUnreal()
    {
        var message = ":gravel.mozilla.org 432  #momo :Erroneous Nickname: Illegal characters";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("gravel.mozilla.org", ircMessage.From);
        Assert.Equal("432", ircMessage.Command);
        Assert.Equal("#momo", ircMessage.Parameters[0]);
        Assert.Equal("Erroneous Nickname: Illegal characters", ircMessage.Parameters[1]);
        Assert.Equal("Erroneous Nickname: Illegal characters", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_Mode()
    {
        var message = ":gravel.mozilla.org MODE #tckk +n ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("gravel.mozilla.org", ircMessage.From);
        Assert.Equal("MODE", ircMessage.Command);
        Assert.Equal("#tckk", ircMessage.Parameters[0]);
        Assert.Equal("+n", ircMessage.Parameters[1]);
        Assert.Equal("+n", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_Mode2()
    {
        var message = ":services.esper.net MODE #foo-bar +o foobar  ";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("services.esper.net", ircMessage.From);
        Assert.Equal("MODE", ircMessage.Command);
        Assert.Equal("#foo-bar", ircMessage.Parameters[0]);
        Assert.Equal("+o", ircMessage.Parameters[1]);
        Assert.Equal("foobar", ircMessage.Parameters[2]);
        Assert.Equal("foobar", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_Mode3()
    {
        var message = ":SomeOp MODE #channel :+i";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("SomeOp", ircMessage.From);
        Assert.Equal("MODE", ircMessage.Command);
        Assert.Equal("#channel", ircMessage.Parameters[0]);
        Assert.Equal("+i", ircMessage.Parameters[1]);
        Assert.Equal("+i", ircMessage.Trailing);
    }

    [Fact]
    public void TestMessage_Mode4()
    {
        var message = ":SomeOp MODE #channel +oo SomeUser :AnotherUser";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(message));

        var ircMessage = new IrcMessage(sequence);

        Assert.Equal("SomeOp", ircMessage.From);
        Assert.Equal("MODE", ircMessage.Command);
        Assert.Equal("#channel", ircMessage.Parameters[0]);
        Assert.Equal("+oo", ircMessage.Parameters[1]);
        Assert.Equal("SomeUser", ircMessage.Parameters[2]);
        Assert.Equal("AnotherUser", ircMessage.Parameters[3]);
        Assert.Equal("AnotherUser", ircMessage.Trailing);
    }
}
