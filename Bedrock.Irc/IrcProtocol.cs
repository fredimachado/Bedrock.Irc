using Bedrock.Framework.Protocols;
using System.Buffers;
using System.Text;

namespace Bedrock.Irc;

public class IrcProtocol : IMessageReader<IrcMessage>, IMessageWriter<string>
{
    private readonly static byte[] IrcMessageDelimiter;

    static IrcProtocol()
    {
        IrcMessageDelimiter = new byte[] { 0xD, 0xA }; // CR LF
    }

    public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out IrcMessage message)
    {
        var reader = new SequenceReader<byte>(input);

        if (!reader.TryReadTo(out ReadOnlySequence<byte> payload, IrcMessageDelimiter, advancePastDelimiter: true))
        {
            message = null;
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
        output.Write(IrcMessageDelimiter);
    }
}
