namespace SwiftFramework.Core.Supervisors
{
    public interface ISupervisorView : IView<Supervisor>
    {
        void OnAbilityActivated();
        void OnAbilityDeactivated();
    }
}
