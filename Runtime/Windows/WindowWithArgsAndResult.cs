using SwiftFramework.Core;
using System;

namespace SwiftFramework.Core.Windows
{
    public abstract class WindowWithArgsAndResult<T, R> : WindowWithArgs<T>, IWindowWithArgsAndResult<T, R>
    {
        public IPromise<R> Result => resultPromise;

        private Promise<R> resultPromise;

        public bool Resolved { get; private set; }

        protected void Resolve(R result)
        {
            if(Resolved)
            {
                return;
            }
            resultPromise.Resolve(result);
            Resolved = true;
        }

        public void CreateNewResultPromise()
        {
            Resolved = false;
            resultPromise = Promise<R>.Create();
        }

        protected void Reject(Exception exception)
        {
            if (Resolved)
            {
                return;
            }
            resultPromise.Reject(exception);
            Resolved = true;
        }
    }
}
