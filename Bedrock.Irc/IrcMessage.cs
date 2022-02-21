using System.Buffers;
using System.Text;

namespace Bedrock.Irc;

public class IrcMessage
{
    public string From { get; private set; }
    public string User { get; private set; }
    public string Host { get; private set; }

    public string Command { get; private set; }
    public string[] Parameters { get; private set; }

    public string Trailing => Parameters is not null ? Parameters[Parameters.Length - 1] : string.Empty;
    public string Raw { get; }

    public IrcMessage(ReadOnlySequence<byte> payload)
    {
        var reader = new SequenceReader<byte>(payload);

        ParsePrefix(ref reader);
        Parse(ref reader);

        Raw = Encoding.UTF8.GetString(payload);
    }

    private void Parse(ref SequenceReader<byte> reader)
    {
        if (!reader.CurrentSpan.Contains((byte)' '))
        {
            Command = Encoding.UTF8.GetString(reader.CurrentSpan);
            return;
        }

        reader.TryReadTo(out ReadOnlySpan<byte> commandSpan, (byte)' ');
        Command = Encoding.UTF8.GetString(commandSpan);

        var parameters = new List<string>();

        while (reader.TryReadTo(out ReadOnlySequence<byte> parameterSequence, (byte)' '))
        {
            if (parameterSequence.FirstSpan.StartsWith(new[] { (byte)':' }))
            {
                reader.Rewind(parameterSequence.Length);
                parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan));
                reader.AdvanceToEnd();
                break;
            }

            parameters.Add(Encoding.UTF8.GetString(parameterSequence));
        }

        if (reader.UnreadSpan.StartsWith(new[] { (byte)':' }))
        {
            parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan.Slice(1)));
        }
        else if (reader.Remaining > 0)
        {
            parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan));
        }

        Parameters = parameters.ToArray();
    }

    private void ParsePrefix(ref SequenceReader<byte> reader)
    {
        // Check if message has prefix, if not, return
        if (!reader.TryPeek(out var value) || value != (byte)':')
        {
            return;
        }

        reader.Advance(1);
        reader.TryReadTo(out ReadOnlySequence<byte> prefixData, (byte)' ');

        var prefixSequence = new SequenceReader<byte>(prefixData);

        if (prefixSequence.TryReadTo(out ReadOnlySequence<byte> userHost, (byte)'@'))
        {
            if (userHost.PositionOf((byte)'!') is SequencePosition userPosition)
            {
                From = Encoding.UTF8.GetString(userHost.Slice(0, userPosition));
                User = Encoding.UTF8.GetString(userHost.Slice(userPosition.GetInteger()));
            }
            else
            {
                From = Encoding.UTF8.GetString(userHost);
            }

            Host = Encoding.UTF8.GetString(prefixSequence.UnreadSpan);
        }
        else
        {
            if (prefixSequence.TryReadTo(out ReadOnlySequence<byte> from, (byte)'!'))
            {
                From = Encoding.UTF8.GetString(from);
                User = Encoding.UTF8.GetString(prefixSequence.UnreadSpan);
            }
            else
            {
                From = Encoding.UTF8.GetString(prefixSequence.CurrentSpan);
            }
        }
    }

    public override string ToString() => Raw;
}
