using Bedrock.Framework.Protocols;
using System.Buffers;
using System.Text;

namespace Bedrock.Irc;

public class IrcProtocol : IMessageReader<IrcMessage>, IMessageWriter<string>
{
    internal const byte CR = (byte)'\r';
    internal const byte LF = (byte)'\n';

    internal static ReadOnlySpan<byte> CrLf => [CR, LF];

    public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out IrcMessage message)
    {
        message = default;
        var reader = new SequenceReader<byte>(input);

        if (!reader.TryReadTo(out ReadOnlySequence<byte> payload, CrLf, advancePastDelimiter: true))
        {
            return false;
        }

        message = new IrcMessage(payload);

        consumed = reader.Position;
        examined = consumed;
        return true;
    }

    public void WriteMessage(string message, IBufferWriter<byte> output)
    {
        output.Write(Encoding.UTF8.GetBytes(message));
        output.Write(CrLf);
    }
}
