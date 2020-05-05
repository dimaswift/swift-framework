using System;

namespace SwiftFramework.Core.Supervisors
{
    [Serializable]
    public class SubordinateState : IDeepCopy<SubordinateState>
    {
        public SupervisorLink assignedSupervisor;

        public SubordinateState DeepCopy()
        {
            return new SubordinateState()
            {
                assignedSupervisor = assignedSupervisor
            };
        }
    }
}
