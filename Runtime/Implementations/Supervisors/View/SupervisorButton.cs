using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    public class SupervisorButton : MonoBehaviour
    {
        [SerializeField] private AbilityTimer abilityTimer = null;
        [SerializeField] private Transform viewPosition = null;

        protected ISubordinate subordinate;
        private ISupervisorView view;
        protected SupervisorLink currentSupervisor;

        public virtual void Init(ISubordinate subordinate)
        {
            this.subordinate = subordinate;

            subordinate.Supervisors.IsAssigned.OnValueChanged += Manager_OnAssignmentChanged;

            subordinate.Supervisors.OnAbilitiesChanged += CheckAbility;


            if (subordinate.Supervisors.Current != null)
            {
                PlaceSupervisor(subordinate.Supervisors.Current);
            }
            else
            {
                RemoveCurrentSupervisor();
            }
        }

        protected virtual void OnAbilityClick()
        {
            if (currentSupervisor == null)
            {
                return;
            }

            if (subordinate.Supervisors.Current != currentSupervisor)
            {
                return;
            }

            subordinate.Supervisors.ActivateCurrentAbility().Done(success =>
            {
                if (success)
                {
                    view?.OnAbilityActivated();
                }
            });
        }

        private void Manager_OnAssignmentChanged(bool assigned)
        {
            if (assigned)
            {
                PlaceSupervisor(subordinate.Supervisors.Current);
            }
            else
            {
                RemoveCurrentSupervisor();
            }
        }

        public void Dispose()
        {
            currentSupervisor = null;
            view = null;
            subordinate = null;
        }

        protected virtual void OnSupervisorPlaced(SupervisorLink supervisor, IView view)
        {
            abilityTimer.gameObject.SetActive(true);
            subordinate.Supervisors.OnAbilitiesChanged -= Supervisors_OnAbilitiesStateChanged;
            subordinate.Supervisors.OnAbilitiesChanged += Supervisors_OnAbilitiesStateChanged;
            SetUp();
        }

        private void SetUp()
        {
            if (currentSupervisor == null)
            {
                abilityTimer.Clear();
                return;
            }
            if (subordinate.Supervisors.TryGetAbility(currentSupervisor, out AbilityState abilityState))
            {
                abilityTimer.Init(abilityState, OnAbilityClick);
            }
            else
            {
                abilityTimer.Clear();
            }
        }

        private void Supervisors_OnAbilitiesStateChanged()
        {
            SetUp();
        }

        protected virtual void OnSupervisorRemoved(SupervisorLink supervisor, IView view)
        {
            abilityTimer.gameObject.SetActive(false);
        }

        private void RemoveCurrentSupervisor()
        {
            abilityTimer.gameObject.SetActive(false);

            if (currentSupervisor != null)
            {
                OnSupervisorRemoved(currentSupervisor, view);
                currentSupervisor = null;
            }

            if (view != null)
            {

                view.ReturnToPool();
                view = null;
            }
        }

        private void CheckAbility()
        {
            if (subordinate.Supervisors.TryGetAbility(currentSupervisor, out AbilityState state))
            {
                if (state.active)
                {
                    view?.OnAbilityActivated();
                }
                else
                {
                    view?.OnAbilityDeactivated();
                }
            }
        }

        private void PlaceSupervisor(SupervisorLink link)
        {
            link.Load(supervisor =>
            {
                supervisor.skin.Load(skin =>
                {
                    App.Core.Views.CreateAsync<ISupervisorView>(skin.view).Done(view =>
                    {
                        this.view = view;
                        this.view.GetRoot().transform.position = viewPosition.position;
                        this.view.Init(supervisor);
                        abilityTimer.gameObject.SetActive(true);
                        currentSupervisor = link;
                        CheckAbility();
                        OnSupervisorPlaced(link, view);
                    });
                });
            });
        }
    }

}
