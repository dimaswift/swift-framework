using SwiftFramework.Core;
using System;

namespace SwiftFramework.Core.Windows
{
    public abstract class WindowWithResult<R> : Window, IWindowWithResult<R>
    {
        public IPromise<R> Result => resultPromise;

        private Promise<R> resultPromise;

        public bool Resolved { get; private set; }

        protected void Resolve(R result)
        {
            if (Resolved)
            {
                return;
            }
            resultPromise.Resolve(result);
            Resolved = true;
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

        public void CreateNewResultPromise()
        {
            resultPromise = Promise<R>.Create();
            Resolved = false;
        }
    }
}
