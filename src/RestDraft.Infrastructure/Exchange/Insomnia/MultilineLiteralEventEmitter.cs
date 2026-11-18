using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Writes text with line breaks as a block ("|"), the way Insomnia does, so a body stays readable in the file.
internal sealed class MultilineLiteralEventEmitter : ChainedEventEmitter
{
    public MultilineLiteralEventEmitter(IEventEmitter nextEmitter)
        : base(nextEmitter)
    {
    }

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (eventInfo.Source.Value is string text && text.Contains('\n'))
        {
            eventInfo.Style = ScalarStyle.Literal;
        }

        base.Emit(eventInfo, emitter);
    }
}
