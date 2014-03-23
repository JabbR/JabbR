using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.AspNet.SignalR.Hubs;

namespace JabbR.Infrastructure
{
    public class BackplaneReflectionMethodResolver : IBackplaneMethodResolver
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, BackplaneMethodRegistration[]>> _methods;

        private readonly ConcurrentDictionary<string, BackplaneMethodRegistration> _executableMethods;

        public BackplaneReflectionMethodResolver()
        {
            _methods = new ConcurrentDictionary<string, IDictionary<string, BackplaneMethodRegistration[]>>();

            _executableMethods = new ConcurrentDictionary<string, BackplaneMethodRegistration>();
        }

        public bool TryGetMethod(BackplaneChannelRegistration channelRegistration, string methodName, int argumentCount, out BackplaneMethodRegistration methodRegistration)
        {
            string cacheKey = string.Format(
                "{0}::{1}({2})",
                channelRegistration.Name,
                methodName,
                argumentCount);

            if (!_executableMethods.TryGetValue(cacheKey, out methodRegistration))
            {
                BackplaneMethodRegistration[] matches;
                var methodCache = _methods.GetOrAdd(channelRegistration.Name, key => BuildMethodCacheFor(channelRegistration));
                if (methodCache.TryGetValue(methodName, out matches))
                {
                    methodRegistration = matches[0];
                }
            }

            return methodRegistration != null;
        }

        private IDictionary<string, BackplaneMethodRegistration[]> BuildMethodCacheFor(BackplaneChannelRegistration channelRegistration)
        {
            return channelRegistration.Type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key,
                              group => group.Select(methodInfo => new BackplaneMethodRegistration
                              {
                                  MethodName = methodInfo.Name,
                                  ArgumentTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                                  ReturnType = methodInfo.ReturnType,
                                  Invoker = (channel, arguments) => methodInfo.Invoke(channel.Instance, arguments)
                              }).ToArray(),
                              StringComparer.OrdinalIgnoreCase);
        }
    }
}