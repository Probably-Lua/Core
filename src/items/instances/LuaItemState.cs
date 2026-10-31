using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaItemState
    {
        internal static bool IsLocked(GameItem item) => item.contentWindow != null &&
            (item.IsTag("LOCKED_CONTAINER_TAG")
                ? item.GetTagReadonly("LOCK_STATUS").GetBool()
                : item.contentWindow.isLocked);

        internal static bool HasChildren(GameItem item) => item.contentWindow != null || item.examineWindow != null;

        internal static void RestoreContainerLock(GameItem item)
        {
            // Other native windows also use isLocked; LockHelper is only for
            // actual lockable containers, not every locked machine/slot window.
            if (!LuaItemView.IsDestroyed(item) && item.IsTag("LOCKED_CONTAINER_TAG") && IsLocked(item))
                LockHelper.LockUpContainer(item);
        }

        internal static void SetAmount(GameItem item, int amount)
        {
            LuaValues.Range(amount, 1, 1000000, "Item amount");
            if (HasChildren(item) && amount != 1)
                throw new ScriptRuntimeException($"Item '{item.identifier}' has a container or examination window and cannot be a stack.");

            if (item.unitCount == amount)
                return;

            item.SetAmount(amount);
            AmountChanged(item);
        }

        internal static void AmountChanged(GameItem item)
        {
            item.Validate();
            item.onChangeAmountItemFunc?.Invoke(item);
            GameInventory? inventory = item.parentInventory;
            if (inventory != null)
            {
                SlotMarker slot = inventory.GetIncompleteItemInventorySlot(item);
                item.onSlotChangeAmountItemFunc?.Invoke(item, inventory, slot);
                inventory.onSlotChangeAmountItemFunc?.Invoke(item, inventory, slot);
            }
            OverlayHandler.current?.UpdateInventoryStateUI();
        }

        internal static GameItem Split(GameItem source, int amount)
        {
            LuaValues.Range(amount, 1, source.unitCount, "Split amount");
            if (HasChildren(source))
                throw new ScriptRuntimeException($"Item '{source.identifier}' has child windows and cannot be split.");

            GameItem result;
            using (LuaItemLifecycle.Suppress())
                result = DirectoryMaster.Item(source.identifier);

            try
            {
                CopyState(source, result);
                result.unitCount = amount;
            }
            catch
            {
                LuaItemView.DestroyTree(result);
                throw;
            }
            source.unitCount -= amount;
            AmountChanged(source);
            AmountChanged(result);
            return result;
        }

        private static void CopyState(GameItem source, GameItem target)
        {
            // TagSystem.Clone copies its dictionary but shares mutable TagState
            // values. Split stacks need independent tags, not that shallow clone.
            target.state = CopyTags(source.state);
            target.modifiedState = CopyTags(source.modifiedState);
            target.itemTypes = new List<string>(source.GetGameItemType());
            target.name = source.name;
            target.shortDescription = source.shortDescription;
            target.longDescription = source.longDescription;
            target.flavorText = source.flavorText;
            target.customText = source.customText;
            target.unitValue = source.unitValue;
            target.unitBaseValue = source.unitBaseValue;
            target.lateUnitValue = source.lateUnitValue;
            target.backupUnitValue = source.backupUnitValue;
            target.useSpriteFromMod = source.useSpriteFromMod;
            target.SetSprite(source.spriteAtlasPath, source.spritePath);
            target.shape = source.shape.Clone();
            target.modifiedShape = source.modifiedShape.Clone();
            // Rebuild the native displays/pricing that these facade mutations
            // maintain in addition to their saved tags.
            if (source.IsTag("WEIGHT_TAG"))
                GeneralHelper.SetItemWeight(target, GeneralHelper.GetWeight(source));

            if (source.IsTag("DURABILITY_TAG"))
            {
                int maximum = DurabilityHelper.GetMaxDurability(source);
                DurabilityHelper.InitItemWithDurability(target, maximum);
                DurabilityHelper.SetDurability(target,
                    DurabilityHelper.GetRealDurability(source), DurabilityHelper.GetFakeDurability(source),
                    maximum, DurabilityHelper.GetDefDurability(source));
            }
        }

        private static TagSystem CopyTags(TagSystem source)
        {
            var result = new TagSystem();
            foreach (var pair in source.dict)
                result.dict.Add(pair.Key, pair.Value.Clone());

            return result;
        }

        internal static bool CanStack(GameItem left, GameItem right)
        {
            if (ReferenceEquals(left, right) || left.identifier != right.identifier ||
                HasChildren(left) || HasChildren(right) || left.name != right.name ||
                left.unitValue != right.unitValue || left.shortDescription != right.shortDescription ||
                left.longDescription != right.longDescription || left.flavorText != right.flavorText ||
                left.customText != right.customText || left.spritePath != right.spritePath ||
                left.spriteAtlasPath != right.spriteAtlasPath)
                return false;

            if (!new HashSet<string>(left.GetGameItemType()).SetEquals(right.GetGameItemType()))
                return false;

            return SameTags(left.state, right.state);
        }

        private static bool SameTags(TagSystem left, TagSystem right)
        {
            // This tag describes the enclosing crate, not the stack's identity.
            const string placementTag = "ITEM_BEHIND_LOCK_TAG";
            int compared = 0;
            foreach (var pair in left.dict)
            {
                if (pair.Key == placementTag)
                    continue;

                compared++;
                if (!right.dict.TryGetValue(pair.Key, out TagState other))
                    return false;

                TagState tag = pair.Value;
                if (tag.valueEnabled != other.valueEnabled || tag.valueString != other.valueString ||
                    tag.valueBool != other.valueBool || tag.valueInt != other.valueInt ||
                    tag.valueLong != other.valueLong || tag.valueFloat != other.valueFloat ||
                    tag.valueDouble != other.valueDouble)
                    return false;
            }
            return compared == right.dict.Count - (right.dict.ContainsKey(placementTag) ? 1 : 0);
        }
    }
}
