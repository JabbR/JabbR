using System;

namespace JabbR.Infrastructure
{
    public interface IBackplaneChannel
    {
        void Subscribe<T>(T instance) where T : IDisposable;

        void Subscribe<T>(T instance, string channelName) where T : IDisposable;

        void Unsubscribe<T>(T instance) where T : IDisposable;

        void Unsubscribe(string channelName);

        void Invoke<T>(string methodName, object[] arguments);

        void Invoke(string channelName, string methodName, object[] arguments);
    }
}
