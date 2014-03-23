using System;

namespace JabbR.Infrastructure
{
    public class BackplaneMethodRegistration
    {
        public Type ReturnType { get; set; }

        public Type[] ArgumentTypes { get; set; }

        public string MethodName { get; set; }

        public virtual Func<BackplaneChannelRegistration, object[], object> Invoker { get; set; } 
    }
}