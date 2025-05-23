﻿using System.Buffers;
using System.Text;

namespace Bedrock.Irc;

public class IrcMessage
{
    public string From { get; private set; }
    public string User { get; private set; }
    public string Host { get; private set; }

    public string Command { get; private set; }
    public string[] Parameters { get; private set; }

    public string Trailing => Parameters is not null ? Parameters[^1] : string.Empty;
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
        if (!reader.UnreadSpan.Contains(Constants.Space))
        {
            Command = Encoding.UTF8.GetString(reader.UnreadSpan.TrimEnd(IrcProtocol.CrLf));
            return;
        }

        reader.TryReadTo(out ReadOnlySpan<byte> commandSpan, Constants.Space);
        Command = Encoding.UTF8.GetString(commandSpan);

        var parameters = new List<string>();

        while (reader.TryReadTo(out ReadOnlySequence<byte> parameterSequence, Constants.Space))
        {
            if (parameterSequence.FirstSpan.StartsWith([Constants.Colon]))
            {
                reader.Rewind(parameterSequence.Length);
                parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan.TrimEnd(IrcProtocol.CrLf)));
                reader.AdvanceToEnd();
                break;
            }

            if (parameterSequence.Length > 0)
            {
                parameters.Add(Encoding.UTF8.GetString(parameterSequence));
            }
        }

        // Check if there's only CrLf left
        if (reader.TryPeek(out var value) && value != IrcProtocol.CR)
        {
            if (reader.UnreadSpan.StartsWith([Constants.Colon]))
            {
                parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan.Slice(1).TrimEnd(IrcProtocol.CrLf)));
            }
            else if (reader.Remaining > 0)
            {
                parameters.Add(Encoding.UTF8.GetString(reader.UnreadSpan.TrimEnd(IrcProtocol.CrLf)));
            }
        }

        Parameters = [.. parameters];
    }

    private void ParsePrefix(ref SequenceReader<byte> reader)
    {
        // Check if message has prefix, if not, return
        if (!reader.TryPeek(out var value) || value != Constants.Colon)
        {
            return;
        }

        reader.Advance(1);
        reader.TryReadTo(out ReadOnlySequence<byte> prefixData, Constants.Space);

        var prefixSequence = new SequenceReader<byte>(prefixData);

        if (prefixSequence.TryReadTo(out ReadOnlySequence<byte> userHost, Constants.AtSign))
        {
            if (userHost.PositionOf(Constants.ExclamationMark) is SequencePosition userPosition)
            {
                From = Encoding.UTF8.GetString(userHost.Slice(0, userPosition));
                User = Encoding.UTF8.GetString(userHost.Slice(userPosition).FirstSpan.TrimStart(Constants.ExclamationMark));
            }
            else
            {
                From = Encoding.UTF8.GetString(userHost);
            }

            Host = Encoding.UTF8.GetString(prefixSequence.UnreadSpan);
        }
        else
        {
            if (prefixSequence.TryReadTo(out ReadOnlySequence<byte> from, Constants.ExclamationMark))
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
