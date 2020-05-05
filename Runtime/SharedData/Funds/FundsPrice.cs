using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [CreateAssetMenu(menuName = "SwiftFramework/Funds/Price")]
    public class FundsPrice : BasePrice, IPrice
    {
        public event Action OnAvailabilityChanged = () => { };

        private void OnEnable()
        {
            App.WaitForState(AppState.ModulesInitialized, () =>
            {
                if (source.HasValue)
                {
                    source.Value.Amount.OnValueChanged += a => OnAvailabilityChanged();
                }
            });
        }

        public FundsSourceLink source;

        public bool CanAfford(BigNumber amount, float discount = 0)
        {
            return source.Value.Amount.Value >= GetFinalPrice(amount, discount);
        }

        public string GetPriceString(BigNumber amount, float discount = 0)
        {
            return GetFinalPrice(amount, discount).ToString();
        }

        private BigNumber GetFinalPrice(BigNumber amount, float discount)
        {
            return (amount - amount.Value.MultiplyByFloat(discount));
        }

        public IPromise<bool> Pay(BigNumber amount, float discount = 0)
        {
            Promise<bool> promise = Promise<bool>.Create();

            source.Load().Then(e =>
            {
                e.Withdraw(amount - amount.Value.MultiplyByFloat(discount)).Done(success => promise.Resolve(success));
            })
            .Catch(e =>
            {
                Debug.Log($"Cannot pay the price! Funds source config not found: {source}");
                promise.Resolve(false);
            });

            return promise;
        }

        public IPromise<bool> Refund(BigNumber amount, float penalty = 1f)
        {
            Promise<bool> promise = Promise<bool>.Create();

            source.Load().Then(e =>
            {
                e.Add(amount.Value.MultiplyByFloat(1f - penalty));
                promise.Resolve(true);
            })
            .Catch(e =>
            {
                Debug.Log($"Cannot refund! Expense config not found: {source}");
                promise.Resolve(false);
            });

            return promise;
        }
    }
    
}
