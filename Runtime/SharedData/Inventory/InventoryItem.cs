using SwiftFramework.Core;
using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Inventory
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Inventory/Item")]
    public class InventoryItem : ScriptableController<InventoryItemState, InventoryItemLink>, IInventoryItem
    {
        public SpriteLink Icon => icon;

        public SpriteLink icon;

        public BigNumber initialAmount;

        public IStatefulEvent<BigNumber> Amount => amount;

        private readonly StatefulEvent<BigNumber> amount = new StatefulEvent<BigNumber>();

        protected override InventoryItemState GetDefaultState()
        {
            return new InventoryItemState() { amount = initialAmount, link = Link as Link };
        }

        protected override void OnLoaded(InventoryItemState state)
        {
            amount.SetValue(state.amount);
        }

        protected override void OnSave()
        {
            state.amount = amount.Value;
        }

        public void Add(BigNumber amountToAdd)
        {
            amount.SetValue(amount.Value + amountToAdd);
        }

        public bool Take(BigNumber amountToTake)
        {
            if(amountToTake > amount.Value)
            {
                return false;
            }
            amount.SetValue(amount.Value - amountToTake);
            return true;
        }
    }

    [Serializable]
    public class InventoryItemState
    {
        public Link link;
        public BigNumber amount;
    }

    [Serializable]
    public class InventoryItemLink : LinkToScriptable<IInventoryItem>
    {

    }
}
