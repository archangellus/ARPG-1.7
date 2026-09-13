using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [System.Serializable]
    public partial class ItemInstance
    {
        /// <summary>
        /// Invoked when the durability changed.
        /// </summary>
        public System.Action onChanged;

        /// <summary>
        /// Invoked when the stack size changed.
        /// </summary>
        public System.Action onStackChanged;

        /// <summary>
        /// Invoked when the durability points reach zero.
        /// </summary>
        public System.Action onBreak;

        /// <summary>
        /// The Item data that represents this Item Instance.
        /// </summary>
        public Item data;

        /// <summary>
        /// The additional attributes of this Item Instance.
        /// </summary>
        public ItemAttributes attributes;

        /// <summary>
        /// The index of the rarity tier in the Game Database's item rarities list.
        /// A value of -1 means no rarity (plain item with no affixes).
        /// </summary>
        public int rarityId = -1;

        /// <summary>
        /// Indices of selected prefix affixes from the item's affix scope.
        /// </summary>
        public List<int> prefixIndices;

        /// <summary>
        /// Indices of selected suffix affixes from the item's affix scope.
        /// </summary>
        public List<int> suffixIndices;

        /// <summary>
        /// The Socketable Item Instances attached to this item's sockets. The array length is
        /// the number of socket slots rolled from this item's rarity; null entries represent
        /// empty slots. Null when this item has no socket slots.
        /// </summary>
        /// <remarks>
        /// Marked <see cref="System.NonSerializedAttribute"/> because this field's type is
        /// self-referential (an Item Instance holding an array of Item Instances), which makes
        /// Unity's engine-level serializer treat it as an unbounded cycle. Save/load doesn't rely
        /// on Unity's serializer for this — it goes through <see cref="ItemSerializer"/> instead.
        /// </remarks>
        [System.NonSerialized]
        public ItemInstance[] sockets;

        protected int m_stack;

        /// <summary>
        /// The current durability points of this Item Instance.
        /// </summary>
        public int durability { get; protected set; }

        /// <summary>
        /// The size of the item stack.
        /// </summary>
        public int stack
        {
            get { return m_stack; }
            set
            {
                if (!IsStackable())
                    return;

                m_stack = Mathf.Clamp(value, 0, data.stackCapacity);
                onStackChanged?.Invoke();
            }
        }

        /// <summary>
        /// The amount of rows this Item Instance takes on the Inventory.
        /// </summary>
        public int rows => data.rows;

        /// <summary>
        /// The amount of columns this Item Instance takes on the Inventory.
        /// </summary>
        public int columns => data.columns;

        /// <summary>
        /// Creates an Item Instance with no affixes.
        /// </summary>
        public ItemInstance(Item data)
        {
            SetDefaultData(data);
        }

        /// <summary>
        /// Creates an Item Instance and rolls affixes based on a rarity level from the Game Database.
        /// </summary>
        public ItemInstance(Item data, int rarityId)
        {
            SetDefaultData(data);
            GenerateAttributesFromRarity(rarityId);
        }

        /// <summary>
        /// Creates an Item Instance and rolls affixes based on a direct ItemRarity reference.
        /// </summary>
        public ItemInstance(Item data, ItemRarity rarity)
        {
            SetDefaultData(data);
            var id = GameDatabase.instance.itemRarities.IndexOf(rarity);
            if (id >= 0)
                GenerateAttributesFromRarity(id);
        }

        /// <summary>
        /// Creates an Item Instance with a rarity level (affixes rolled normally from that rarity)
        /// but an explicit number of socket slots instead of rolling them.
        /// </summary>
        public ItemInstance(Item data, int rarityId, int socketCount)
        {
            SetDefaultData(data);
            GenerateAttributesFromRarity(rarityId, socketCount);
        }

        /// <summary>
        /// Creates an Item Instance with pre-existing attributes and default durability.
        /// </summary>
        public ItemInstance(Item data, ItemAttributes attributes)
        {
            this.data = data;
            this.attributes = attributes;

            if (IsEquippable())
                durability = GetEquippable().maxDurability;

            if (IsStackable())
                stack = 1;
        }

        /// <summary>
        /// Creates an Item Instance with pre-existing attributes and explicit durability and stack.
        /// </summary>
        public ItemInstance(Item data, ItemAttributes attributes, int durability, int stack)
        {
            this.data = data;
            this.attributes = attributes;
            this.durability = durability;
            this.stack = stack;
        }

        /// <summary>
        /// Creates an Item Instance from fully explicit save data.
        /// </summary>
        public ItemInstance(
            Item data,
            ItemAttributes attributes,
            int durability,
            int stack,
            int rarityId,
            List<int> prefixIndices,
            List<int> suffixIndices,
            ItemInstance[] sockets
        )
        {
            this.data = data;
            this.attributes = attributes;
            this.durability = durability;
            this.stack = stack;
            this.rarityId = rarityId;
            this.prefixIndices = prefixIndices;
            this.suffixIndices = suffixIndices;
            this.sockets = sockets;
        }

        /// <summary>
        /// Tries to stack another item on the stack.
        /// </summary>
        /// <param name="other">The Item Instance you want to try stack.</param>
        /// <returns>Returns true if it was able to stack the item.</returns>
        public virtual bool TryStack(ItemInstance other)
        {
            if (!CanStack(other))
                return false;

            stack += other.stack;
            return true;
        }

        /// <summary>
        /// Returns the required minimum level to equip this Item.
        /// </summary>
        public virtual int GetRequiredLevel()
        {
            if (IsEquippable())
                return GetEquippable().requiredLevel;
            if (IsSkill())
                return GetSkill().requiredLevel;

            return 0;
        }

        /// <summary>
        /// Returns the required minimum strength to equip this Item.
        /// </summary>
        public virtual int GetRequiredStrength()
        {
            if (IsEquippable())
                return GetEquippable().requiredStrength;
            if (IsSkill())
                return GetSkill().requiredStrength;

            return 0;
        }

        /// <summary>
        /// Returns the required minimum dexterity to equip this Item.
        /// </summary>
        public virtual int GetRequiredDexterity()
        {
            if (IsEquippable())
                return GetEquippable().requiredDexterity;

            return 0;
        }

        /// <summary>
        /// Returns the required minimum energy to equip this Item.
        /// </summary>
        public virtual int GetRequiredEnergy()
        {
            if (IsSkill())
                return GetSkill().requiredEnergy;

            return 0;
        }

        /// <summary>
        /// Returns true if this Item Instance can stack another given Item Instance.
        /// </summary>
        /// <param name="other">The Item Instance you want to check.</param>
        public virtual bool CanStack(ItemInstance other) =>
            IsStackable() && other.data == data && stack + other.stack <= data.stackCapacity;

        /// <summary>
        /// Returns true if the durability points of this Item Instance is zero.
        /// </summary>
        public virtual bool IsBroken() => durability == 0;

        /// <summary>
        /// Returns true if the durability of this Item Instance is at half.
        /// </summary>
        public virtual bool IsAboutToBreak()
        {
            if (!IsEquippable())
                return false;

            return durability <= GetEffectiveMaxDurability() / 2f;
        }

        /// <summary>
        /// Returns true if this Item Instance has additional attributes.
        /// </summary>
        public virtual bool ContainAttributes() => IsEquippable() && attributes != null;

        /// <summary>
        /// Returns true if it's allowed to read the additional attributes from this Item Instance.
        /// </summary>
        public virtual bool UseAttributes() => ContainAttributes() && !IsBroken();

        /// <summary>
        /// Returns the value of the given attribute type, or 0 if attributes are not active.
        /// </summary>
        public virtual int GetAttribute(ItemAttributes.AttributeType type) =>
            UseAttributes() ? attributes[type] : 0;

        /// <summary>
        /// Reduces the durability of this Item Instance by a given amount.
        /// </summary>
        /// <param name="amount">The amount of points to decrease from the durability.</param>
        public virtual void ApplyDamage(int amount)
        {
            if (!IsEquippable())
                return;

            var maxDurability = GetEffectiveMaxDurability();
            durability = Mathf.Clamp(durability - amount, 0, maxDurability);

            if (durability <= 0)
                onBreak?.Invoke();

            onChanged?.Invoke();
        }

        /// <summary>
        /// Returns the minimum and maximum damage of this Item Instance. If the Item is broken or if its
        /// not a Weapon, the damage will always be zero. If it's about to break, the damage is reduced by half.
        /// </summary>
        public virtual MinMax GetDamage()
        {
            if (!IsWeapon() || IsBroken())
                return MinMax.Zero;

            var rarity = GetRarity();
            var damageBonus = rarity != null ? rarity.bonusDamage : 0;
            var minDamage = GetWeapon().minDamage + damageBonus;
            var maxDamage = GetWeapon().maxDamage + damageBonus;

            if (IsAboutToBreak())
                return new((int)(minDamage / 2f), (int)(maxDamage / 2f));

            return new(minDamage, maxDamage);
        }

        /// <summary>
        /// Returns the defense points of this Item Instance. If it's broken, the defense is zero.
        /// If the Item Instance is about to break, the defense is reduced by half.
        /// </summary>
        public virtual int GetDefense()
        {
            if (IsBroken())
                return 0;

            var rarity = GetRarity();
            var defenseBonus = rarity != null ? rarity.bonusDefense : 0;
            var defense = 0;

            if (IsArmor())
                defense = GetArmor().defense + defenseBonus;
            else if (IsShield())
                defense = GetShield().defense + defenseBonus;

            return IsAboutToBreak() ? (int)(defense / 2f) : defense;
        }

        /// <summary>
        /// Sets this Item Instance durability to its maximum points.
        /// </summary>
        public virtual void Repair()
        {
            if (!IsEquippable())
                return;

            durability = GetEffectiveMaxDurability();
            onChanged?.Invoke();
        }

        /// <summary>
        /// Returns the current durability in a rate of zero to one.
        /// </summary>
        public virtual float GetDurabilityRate()
        {
            if (!IsEquippable())
                return 1;

            return durability / (float)GetEffectiveMaxDurability();
        }

        /// <summary>
        /// Returns the display name of this Item Instance, incorporating affix naming rules.
        /// In <see cref="ItemRarity.AffixesMode.Paired"/> mode, a prefix appears before the item name and a
        /// suffix appears after as "of [name]". In <see cref="ItemRarity.AffixesMode.Layered"/> mode, the
        /// rarity display name is prepended instead (e.g. "Rare Iron Sword").
        /// </summary>
        public virtual string GetDisplayName()
        {
            var db = GameDatabase.instance;

            if (rarityId < 0 || !db.itemRarities.IsIndexValid(rarityId))
                return WithSocketSuffix(data.name);

            var rarity = db.itemRarities[rarityId];

            if (rarity.affixesMode == ItemRarity.AffixesMode.Layered)
                return WithSocketSuffix($"{rarity.displayName} {data.name}");

            var affixes = rarity.affixes;

            if (affixes == null)
                return WithSocketSuffix(data.name);

            var prefix =
                prefixIndices?.Count > 0 && affixes.prefixes.IsIndexValid(prefixIndices[0])
                    ? $"{affixes.prefixes[prefixIndices[0]].name} "
                    : "";
            var suffix =
                suffixIndices?.Count > 0 && affixes.suffixes.IsIndexValid(suffixIndices[0])
                    ? $" {affixes.suffixes[suffixIndices[0]].name}"
                    : "";

            return WithSocketSuffix($"{prefix}{data.name}{suffix}".Trim());
        }

        /// <summary>
        /// Appends a "(N sockets)" suffix to the given name when this item has socket slots,
        /// using the singular "socket" when there's exactly one.
        /// </summary>
        protected virtual string WithSocketSuffix(string name)
        {
            if (sockets == null || sockets.Length == 0)
                return name;

            var word = sockets.Length == 1 ? "socket" : "sockets";
            return $"{name} ({sockets.Length} {word})";
        }

        /// <summary>
        /// Returns the rarity color for this Item Instance, or the fallback color if no rarity is set.
        /// </summary>
        /// <param name="fallback">The color to use when no rarity is assigned.</param>
        public virtual Color GetRarityColor(Color fallback)
        {
            var db = GameDatabase.instance;

            if (rarityId >= 0 && db.itemRarities.IsIndexValid(rarityId))
                return db.itemRarities[rarityId].color;

            return fallback;
        }

        /// <summary>
        /// Returns the Item Rarity for this Item Instance, or null if no rarity is assigned.
        /// </summary>
        public virtual ItemRarity GetRarity()
        {
            var db = GameDatabase.instance;

            if (rarityId >= 0 && db.itemRarities.IsIndexValid(rarityId))
                return db.itemRarities[rarityId];

            return null;
        }

        /// <summary>
        /// Returns the effective maximum durability, including any flat bonus from the item's rarity.
        /// </summary>
        public virtual int GetEffectiveMaxDurability()
        {
            if (!IsEquippable())
                return 0;

            var rarity = GetRarity();
            return GetEquippable().maxDurability + (rarity != null ? rarity.bonusMaxDurability : 0);
        }

        /// <summary>
        /// Returns the effective attack speed of this weapon, including any flat bonus from the item's rarity.
        /// </summary>
        public virtual int GetEffectiveAttackSpeed()
        {
            if (!IsWeapon())
                return 0;

            var rarity = GetRarity();
            return GetWeapon().attackSpeed + (rarity != null ? rarity.bonusAttackSpeed : 0);
        }

        /// <summary>
        /// Returns the effective chance to block as a 0–1 value, including any flat bonus from the item's rarity.
        /// </summary>
        public virtual float GetEffectiveChanceToBlock()
        {
            if (!IsShield())
                return 0;

            var rarity = GetRarity();
            return (GetShield().chanceToBlock + (rarity != null ? rarity.bonusChanceToBlock : 0))
                / 100f;
        }

        /// <summary>
        /// Returns the selling price of this Item Instance.
        /// </summary>
        public virtual int GetSellPrice() => (int)(GetPrice() / 2f);

        /// <summary>
        /// Returns the price of this Item Instance. If it's a stack, the price is multiplied
        /// by the stack size. The durability rate of the Item Instance is multiplied by its
        /// final price. The price of every socketed Socketable is added on top, unaffected by
        /// this item's own durability.
        /// </summary>
        public virtual int GetPrice()
        {
            var price = data.price;

            if (IsStackable())
                price *= stack;

            if (IsEquippable())
            {
                if (attributes != null)
                {
                    var totalAttr = attributes.GetAttributesCount();
                    price += totalAttr * Game.instance.pricePerAttribute;
                }

                price = (int)(price * GetDurabilityRate());
            }

            price += GetSocketsPrice();

            return price;
        }

        /// <summary>
        /// Returns the combined price of every Socketable currently attached to this item's
        /// sockets.
        /// </summary>
        public virtual int GetSocketsPrice()
        {
            if (sockets == null)
                return 0;

            var price = 0;

            foreach (var socket in sockets)
            {
                if (socket != null)
                    price += socket.GetPrice();
            }

            return price;
        }

        /// <summary>
        /// Returns a new ItemAttributes containing only the attribute bonuses granted by this
        /// item's filled sockets, scoped to this item's type. Useful for excluding the sockets'
        /// contribution from the merged <see cref="attributes"/> totals when displaying them
        /// separately (see <see cref="InspectSockets"/>).
        /// </summary>
        public virtual ItemAttributes GetSocketsAttributes()
        {
            var result = new ItemAttributes();

            if (sockets == null)
                return result;

            var itemScope = GetItemScope();

            foreach (var socket in sockets)
            {
                if (socket != null)
                    result.ApplySocket(socket.GetSocketable(), itemScope);
            }

            return result;
        }

        protected string InspectRequired(
            string name,
            int required,
            int current,
            Color error,
            bool breakLine
        )
        {
            var lineBreak = breakLine ? "\n" : "";
            var attr = $"Required {name}: {required}";

            if (current < required)
                return lineBreak + attr.WithColor(error);

            return lineBreak + attr;
        }

        /// <summary>
        /// Returns a string with the Item's general attributes.
        /// </summary>
        /// <param name="stats">The Entity Stats to compare against.</param>
        /// <param name="warning">The color of warning texts.</param>
        /// <param name="error">The color of the error texts.</param>
        /// <param name="special">The color used for values boosted by rarity.</param>
        public virtual string Inspect(
            EntityStatsManager stats,
            Color warning,
            Color error,
            Color special
        )
        {
            var text = "";
            var rarity = GetRarity();

            if (IsArmor())
            {
                var defense = GetArmor().defense + (rarity != null ? rarity.bonusDefense : 0);
                var defenseStr =
                    rarity != null && rarity.bonusDefense > 0
                        ? $"{defense}".WithColor(special)
                        : $"{defense}";
                text += $"Defense: {defenseStr}";
            }
            else if (IsShield())
            {
                var defense = GetShield().defense + (rarity != null ? rarity.bonusDefense : 0);
                var chanceToBlock =
                    GetShield().chanceToBlock + (rarity != null ? rarity.bonusChanceToBlock : 0);
                var defenseStr =
                    rarity != null && rarity.bonusDefense > 0
                        ? $"{defense}".WithColor(special)
                        : $"{defense}";
                var chanceToBlockStr =
                    rarity != null && rarity.bonusChanceToBlock > 0
                        ? $"{chanceToBlock}%".WithColor(special)
                        : $"{chanceToBlock}%";
                text += $"Defense: {defenseStr}";
                text += $"\nChance To Block: {chanceToBlockStr}";
            }
            else if (IsWeapon())
            {
                var damageBonus = rarity != null ? rarity.bonusDamage : 0;
                var minDamage = GetWeapon().minDamage + damageBonus;
                var maxDamage = GetWeapon().maxDamage + damageBonus;
                var attackSpeed =
                    GetWeapon().attackSpeed + (rarity != null ? rarity.bonusAttackSpeed : 0);
                var damageStr =
                    damageBonus > 0
                        ? $"{minDamage} ~ {maxDamage}".WithColor(special)
                        : $"{minDamage} ~ {maxDamage}";
                var attackSpeedStr =
                    rarity != null && rarity.bonusAttackSpeed > 0
                        ? $"{attackSpeed}".WithColor(special)
                        : $"{attackSpeed}";
                text += $"Damage: {damageStr}";
                text += $"\nAttack Speed: {attackSpeedStr}";
            }

            if (IsEquippable())
            {
                var lineBreak = text.Length > 0 ? "\n" : "";
                var maxDurability = GetEffectiveMaxDurability();
                var durabilityValues = $"{durability} of {maxDurability}";
                var hasSpecialDurability = rarity != null && rarity.bonusMaxDurability > 0;

                if (IsAboutToBreak())
                    text += lineBreak + $"Durability: {durabilityValues}".WithColor(warning);
                else if (IsBroken())
                    text += lineBreak + $"Durability: {durabilityValues}".WithColor(error);
                else if (hasSpecialDurability)
                    text += lineBreak + $"Durability: {durabilityValues.WithColor(special)}";
                else
                    text += lineBreak + $"Durability: {durabilityValues}";
            }

            if (GetRequiredLevel() > 1)
                text += InspectRequired(
                    "Level",
                    GetRequiredLevel(),
                    stats.level,
                    error,
                    text.Length > 0
                );

            if (GetRequiredStrength() > 0)
                text += InspectRequired(
                    "Strength",
                    GetRequiredStrength(),
                    stats.strength,
                    error,
                    text.Length > 0
                );

            if (GetRequiredDexterity() > 0)
                text += InspectRequired(
                    "Dexterity",
                    GetRequiredDexterity(),
                    stats.dexterity,
                    error,
                    text.Length > 0
                );

            if (GetRequiredEnergy() > 0)
                text += InspectRequired(
                    "Energy",
                    GetRequiredEnergy(),
                    stats.energy,
                    error,
                    text.Length > 0
                );

            return text;
        }

        /// <summary>
        /// Returns a formatted description of this item's socket slots: the attribute bonuses
        /// granted by every filled socket, merged into a single flat list (formatted the same
        /// way as <see cref="ItemAttributes.Inspect"/>, without naming which Socketable grants
        /// each one), and "Empty Socket" for each empty slot.
        /// </summary>
        /// <param name="emptySocketColor">The color used for the "Empty Socket" text.</param>
        public virtual string InspectSockets(Color emptySocketColor)
        {
            if (sockets == null || sockets.Length == 0)
                return "";

            var text = "";
            var itemScope = GetItemScope();
            var emptySocketText = "Empty Socket Slot".WithColor(emptySocketColor);

            foreach (var socket in sockets)
            {
                if (text.Length > 0)
                    text += "\n";

                var bonusText =
                    socket != null
                        ? ItemAttributes.InspectSocket(socket.GetSocketable(), itemScope)
                        : "";
                text += !string.IsNullOrEmpty(bonusText) ? bonusText : emptySocketText;
            }

            return text;
        }

        /// <summary>
        /// Returns a formatted description of this Socketable's own attribute bonuses, grouped
        /// by the item scope each applies to (formatted the same way as
        /// <see cref="ItemAttributes.Inspect"/>). Returns an empty string if this Item Instance
        /// isn't a Socketable.
        /// </summary>
        public virtual string InspectSocketableModifiers()
        {
            if (!IsSocketable())
                return "";

            return ItemAttributes.InspectSocketable(GetSocketable());
        }

        protected virtual void SetDefaultData(Item data)
        {
            this.data = data;

            if (IsEquippable())
                durability = GetEquippable().maxDurability;

            if (IsStackable())
                stack = 1;
        }

        /// <summary>
        /// Returns the <see cref="ItemScope"/> flag that corresponds to this item's type.
        /// For armor, the specific slot (Helm, Chest, Pants, Gloves, Boots) is returned.
        /// Returns <see cref="ItemScope.None"/> when the item type has no scope.
        /// </summary>
        protected virtual ItemScope GetItemScope()
        {
            if (IsBlade())
                return ItemScope.Blade;
            if (IsBow())
                return ItemScope.Bow;
            if (IsArmor())
                return GetArmor().slot switch
                {
                    ItemSlots.Helm => ItemScope.Helm,
                    ItemSlots.Chest => ItemScope.Chest,
                    ItemSlots.Pants => ItemScope.Pants,
                    ItemSlots.Gloves => ItemScope.Gloves,
                    ItemSlots.Boots => ItemScope.Boots,
                    _ => ItemScope.None,
                };
            if (IsShield())
                return ItemScope.Shield;
            if (IsRing())
                return ItemScope.Ring;
            if (IsAmulet())
                return ItemScope.Amulet;

            return ItemScope.None;
        }

        /// <summary>
        /// Returns true if this Item Instance has at least one empty socket slot.
        /// </summary>
        public virtual bool HasEmptySocket() =>
            sockets != null && System.Array.Exists(sockets, socket => socket == null);

        /// <summary>
        /// Returns the Item Instances currently attached to this item's socket slots, excluding
        /// empty slots.
        /// </summary>
        public virtual List<ItemInstance> GetOccupiedSockets()
        {
            var result = new List<ItemInstance>();

            if (sockets == null)
                return result;

            foreach (var socket in sockets)
            {
                if (socket != null)
                    result.Add(socket);
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given Item Instance can be attached to one of this item's empty
        /// socket slots: this item must have an empty socket, and the given instance must be a
        /// Socketable.
        /// </summary>
        /// <param name="socketable">The Item Instance you want to attach.</param>
        public virtual bool CanAddSocket(ItemInstance socketable) =>
            socketable != null && socketable.IsSocketable() && HasEmptySocket();

        /// <summary>
        /// Attaches one unit of the given Socketable Item Instance to this item's first empty
        /// socket slot, baking its scoped attribute bonuses directly into this item's
        /// attributes. Individual sockets cannot be removed or replaced in place — use
        /// <see cref="ClearSockets"/> to remove all of them at once.
        /// </summary>
        /// <param name="socketable">The Socketable Item Instance to attach.</param>
        /// <returns>Returns true if the socketable was successfully attached.</returns>
        public virtual bool TryAddSocket(ItemInstance socketable)
        {
            if (!CanAddSocket(socketable))
                return false;

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null)
                    continue;

                sockets[i] = new ItemInstance(socketable.data);
                attributes ??= new ItemAttributes();
                attributes.ApplySocket(socketable.GetSocketable(), GetItemScope());
                onChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes every Socketable currently attached to this item's sockets, reverting the
        /// attribute bonuses they granted and leaving all socket slots empty again.
        /// </summary>
        public virtual void ClearSockets()
        {
            if (sockets == null)
                return;

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] == null)
                    continue;

                attributes?.RemoveSocket(sockets[i].GetSocketable(), GetItemScope());
                sockets[i] = null;
            }

            onChanged?.Invoke();
        }

        /// <summary>
        /// Randomly generates additional attributes and socket slots for this Item Instance
        /// based on a rarity level. The affix selection strategy is determined by the rarity's
        /// <see cref="ItemRarity.AffixesMode"/>, and the socket count by
        /// <see cref="ItemRarity.RollSocketCount"/>. Logs a warning and skips generation if the
        /// rarity level is out of bounds.
        /// </summary>
        protected virtual void GenerateAttributesFromRarity(int level)
        {
            if (!IsEquippable())
                return;

            var db = GameDatabase.instance;

            if (!db.itemRarities.IsIndexValid(level))
            {
                Debug.LogWarning(
                    $"ItemInstance: rarityId {level} is out of bounds in the Game Database. "
                        + "No attributes will be generated."
                );
                return;
            }

            var rarity = db.itemRarities[level];
            var itemScope = GetItemScope();

            var rolledPrefixIndices = new List<int>();
            var rolledSuffixIndices = new List<int>();

            if (rarity.affixes != null)
                rarity.RollAffixIndices(
                    itemScope,
                    out rolledPrefixIndices,
                    out rolledSuffixIndices
                );

            var socketCount = rarity.RollSocketCount(itemScope);

            if (
                rolledPrefixIndices.Count == 0
                && rolledSuffixIndices.Count == 0
                && socketCount == 0
            )
                return;

            rarityId = level;
            durability = GetEffectiveMaxDurability();
            attributes = new ItemAttributes();
            prefixIndices = rolledPrefixIndices;
            suffixIndices = rolledSuffixIndices;

            foreach (var i in prefixIndices)
                attributes.Apply(rarity.affixes.prefixes[i], rarity.valueWeight);

            foreach (var i in suffixIndices)
                attributes.Apply(rarity.affixes.suffixes[i], rarity.valueWeight);

            if (socketCount > 0)
                sockets = new ItemInstance[socketCount];
        }

        /// <summary>
        /// Randomly generates additional attributes for this Item Instance based on a rarity
        /// level, using the same affix selection as <see cref="GenerateAttributesFromRarity(int)"/>,
        /// but with an explicit number of socket slots instead of rolling them from the rarity.
        /// </summary>
        protected virtual void GenerateAttributesFromRarity(int level, int socketCount)
        {
            if (!IsEquippable())
                return;

            var db = GameDatabase.instance;

            if (!db.itemRarities.IsIndexValid(level))
            {
                Debug.LogWarning(
                    $"ItemInstance: rarityId {level} is out of bounds in the Game Database. "
                        + "No attributes will be generated."
                );
                return;
            }

            var rarity = db.itemRarities[level];
            var itemScope = GetItemScope();

            var rolledPrefixIndices = new List<int>();
            var rolledSuffixIndices = new List<int>();

            if (rarity.affixes != null)
                rarity.RollAffixIndices(
                    itemScope,
                    out rolledPrefixIndices,
                    out rolledSuffixIndices
                );

            rarityId = level;
            durability = GetEffectiveMaxDurability();
            attributes = new ItemAttributes();
            prefixIndices = rolledPrefixIndices;
            suffixIndices = rolledSuffixIndices;

            foreach (var i in prefixIndices)
                attributes.Apply(rarity.affixes.prefixes[i], rarity.valueWeight);

            foreach (var i in suffixIndices)
                attributes.Apply(rarity.affixes.suffixes[i], rarity.valueWeight);

            if (socketCount > 0)
                sockets = new ItemInstance[socketCount];
        }

        /// <summary>
        /// Returns a new Item Instance from the Item Serializer.
        /// </summary>
        /// <param name="serializer">The Item Serializer to create the Item Instance from.</param>
        public static ItemInstance CreateFromSerializer(ItemSerializer serializer)
        {
            if (serializer == null || serializer.itemId < 0)
                return null;

            var item = GameDatabase.instance.FindElementById<Item>(serializer.itemId);
            var attributes = ItemAttributes.CreateFromSerializer(serializer.attributes);
            var prefixIndices =
                serializer.prefixIndices != null ? new List<int>(serializer.prefixIndices) : null;
            var suffixIndices =
                serializer.suffixIndices != null ? new List<int>(serializer.suffixIndices) : null;
            var sockets =
                serializer.socketItemIds != null
                    ? System.Array.ConvertAll(
                        serializer.socketItemIds,
                        socketItemId =>
                            socketItemId >= 0
                                ? new ItemInstance(
                                    GameDatabase.instance.FindElementById<Item>(socketItemId)
                                )
                                : null
                    )
                    : null;

            return new ItemInstance(
                item,
                attributes,
                serializer.durability,
                serializer.stack,
                serializer.rarityId,
                prefixIndices,
                suffixIndices,
                sockets
            );
        }
    }
}
