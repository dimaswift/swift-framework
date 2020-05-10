using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    public interface IPrice
    {
        SpriteLink Icon { get; }
        string GetPriceString(BigNumber amount, float discount = 0);
        IPromise<bool> Pay(BigNumber amount, float discount = 0);
        IPromise<bool> Refund(BigNumber amount, float penalty = 0f);
        event Action OnAvailabilityChanged;
        bool CanAfford(BigNumber amount, float discount = 0);
    }

    [FlatHierarchy]
    [Serializable]
    public class PriceLink : LinkToScriptable<IPrice>
    {
        public static PriceLink CreatePrice(PriceLink source, BigNumber amount)
        {
            var link = Create<PriceLink>(source.GetPath());
            link.amount = amount;
            return link;
        }

        public PriceLink CloneWithAmount(BigNumber amount)
        {
            return CreatePrice(this, amount);
        }

        public BigNumber amount;

        public SpriteLink Icon => Value.Icon;

        public bool CanAfford(float discount = 0)
        {
            return Value.CanAfford(amount, discount);
        }

        public string GetPriceString(float discount = 0)
        {
            return Value.GetPriceString(amount, discount);
        }

        public IPromise<bool> Refund(float penalty = 1)
        {
            return Value.Refund(amount, penalty);
        }

        public IPromise<bool> Pay(float discount = 0)
        {
            Promise<bool> promise = Promise<bool>.Create();

            Load().Then(p => { p.Pay(amount, discount).Channel(promise); }).Catch(e => 
            {
                Debug.Log($"Cannot load price {GetPath()}: {e.Message}"); 
                promise.Resolve(false);
            });

            return promise; 
            
        }
    }
}