using SwiftFramework.Core.SharedData.Inventory;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Funds/Source")]
    public class FundsSource : InventoryItem, IFundsSource
    {
        public IPromise<bool> Withdraw(BigNumber price)
        {
            Promise<bool> promise = Promise<bool>.Create();

            if (Take(price) == false)
            {
                promise.Resolve(false);
                return promise;
            }

            promise.Resolve(true);

            return promise;
        }
    }
}
