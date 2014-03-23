using System;

namespace JabbR.Infrastructure
{
    public interface IBackplaneMethodResolver
    {
        bool TryGetMethod(BackplaneChannelRegistration channelRegistration, string methodName, int argumentCount, out BackplaneMethodRegistration methodInvoker);
    }
}
