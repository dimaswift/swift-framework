using SwiftFramework.Core.Boosters;
using SwiftFramework.Core.Supervisors;
using System;

namespace SwiftFramework.Core
{
    public interface ISubordinate
    {
        SupervisorTemplateLink GetSupervisorTemplate();
        string SupervisorsTitle { get; }
        ISupervisorsManager Supervisors { get; }
    }

    [Serializable]
    public class SubordinateLink : LinkToScriptable<ISubordinate>
    {

    }
}
