using UnityEngine;

namespace SwiftFramework.Core.SharedData.Inventory
{
    [CreateAssetMenu(menuName = "SwiftFramework/Inventory/ItemReward")]
    public class InventoryItemReward : ScriptableObject, IReward
    {
        public string descKey;
        public InventoryItemLink item;
        public BigNumber amount = 1;

        public SpriteLink Icon => item.Value.Icon;

        public IPromise AddReward()
        {
            Promise promise = Promise.Create();
           
            item.Load().Then(i => 
            {
                i.Add(amount);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public BigNumber GetAmount() => amount;

        public string GetDescription()
        {
            return descKey;
        }

        public bool IsAvailable()
        {
            return true;
        }
    }
}
