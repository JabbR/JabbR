using System;

namespace JabbR.Infrastructure
{
    public class NullBackplaneChannel : IBackplaneChannel
    {
        public void Subscribe<T>(T instance) where T : IDisposable
        {
        }

        public void Subscribe<T>(T instance, string channelName) where T : IDisposable
        {
        }

        public void Unsubscribe<T>(T instance) where T : IDisposable
        {
        }

        public void Unsubscribe(string channelName)
        {
        }

        public void Invoke<T>(string methodName, object[] arguments)
        {
        }

        public void Invoke(string channelName, string methodName, object[] arguments)
        {
        }
    }
}