#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

namespace PLAYERTWO.ARPGProject.Tests
{
    public class ItemAttributesComparisonTests
    {
        class ItemWithoutRarity : ItemInstance
        {
            public ItemWithoutRarity(Item data, ItemAttributes attributes)
                : base(data, attributes) { }

            public override ItemRarity GetRarity() => null;
        }

        static readonly Color Favorable = Color.green;
        static readonly Color Unfavorable = Color.red;

        [Test]
        public void InspectComparisonShowsSignedMatchedAndGainedDifferences()
        {
            var candidate = new ItemAttributes();
            candidate[ItemAttributes.AttributeType.Strength] = 15;
            candidate[ItemAttributes.AttributeType.Vitality] = 8;
            var reference = new ItemAttributes();
            reference[ItemAttributes.AttributeType.Strength] = 10;

            var text = candidate.InspectComparison(
                reference,
                null,
                null,
                Favorable,
                Unfavorable
            );

            StringAssert.Contains("(+5", text);
            StringAssert.Contains("(+8", text);
        }

        [Test]
        public void InspectComparisonKeepsMissingReferencePropertiesInLossSection()
        {
            var candidate = new ItemAttributes();
            var reference = new ItemAttributes();
            reference[ItemAttributes.AttributeType.Health] = 40;

            var text = candidate.InspectComparison(
                reference,
                null,
                null,
                Favorable,
                Unfavorable
            );

            StringAssert.Contains("Properties lost when equipped:", text);
            StringAssert.Contains("- +40 of Additional Health", text);
        }

        [Test]
        public void InspectComparisonSuppressesUnchangedAndSocketOnlyDifferences()
        {
            var candidate = new ItemAttributes();
            candidate[ItemAttributes.AttributeType.Dexterity] = 12;
            candidate[ItemAttributes.AttributeType.Energy] = 5;
            var reference = new ItemAttributes();
            reference[ItemAttributes.AttributeType.Dexterity] = 12;
            var sockets = new ItemAttributes();
            sockets[ItemAttributes.AttributeType.Energy] = 5;

            var text = candidate.InspectComparison(
                reference,
                sockets,
                null,
                Favorable,
                Unfavorable
            );

            StringAssert.DoesNotContain("(+0", text);
            StringAssert.DoesNotContain("(-0", text);
            StringAssert.DoesNotContain("Energy", text);
        }

        [Test]
        public void InspectComparisonCanCompareResolvedGemBonusesSeparately()
        {
            var candidateGems = new ItemAttributes();
            candidateGems[ItemAttributes.AttributeType.FireDamage] = 18;
            var equippedGems = new ItemAttributes();
            equippedGems[ItemAttributes.AttributeType.FireDamage] = 10;
            equippedGems[ItemAttributes.AttributeType.Health] = 25;

            var text = candidateGems.InspectComparison(
                equippedGems,
                null,
                null,
                Favorable,
                Unfavorable
            );

            StringAssert.Contains("(+8", text);
            StringAssert.Contains("Properties lost when equipped:", text);
            StringAssert.Contains("Additional Health", text);
        }

        [Test]
        public void ItemPowerAndArmorIncludeResolvedAttributesAndSocketCapacity()
        {
            var armorData = ScriptableObject.CreateInstance<ItemArmor>();
            armorData.requiredLevel = 2;
            armorData.defense = 100;
            var attributes = new ItemAttributes();
            attributes[ItemAttributes.AttributeType.Defense] = 20;
            attributes[ItemAttributes.AttributeType.DefensePercent] = 10;
            var item = new ItemWithoutRarity(armorData, attributes)
            {
                sockets = new ItemInstance[2],
            };

            Assert.AreEqual(132, item.GetArmorValue());
            Assert.AreEqual(162, item.GetItemPower());

            Object.DestroyImmediate(armorData);
        }

        [Test]
        public void InspectPowerShowsSignedPowerAndArmorDifferences()
        {
            var candidateData = ScriptableObject.CreateInstance<ItemArmor>();
            candidateData.defense = 120;
            var referenceData = ScriptableObject.CreateInstance<ItemArmor>();
            referenceData.defense = 100;
            var candidate = new ItemWithoutRarity(candidateData, new ItemAttributes());
            var reference = new ItemWithoutRarity(referenceData, new ItemAttributes());

            var text = candidate.InspectPower(reference, Favorable, Unfavorable);

            StringAssert.Contains("Item Power 121", text);
            StringAssert.Contains("+120 Armor", text);
            StringAssert.Contains("+20", text);

            Object.DestroyImmediate(candidateData);
            Object.DestroyImmediate(referenceData);
        }

        [Test]
        public void TooltipTitleExcludesSocketAndNumericDetails()
        {
            var armorData = ScriptableObject.CreateInstance<ItemArmor>();
            armorData.name = "Iron Helm";
            var item = new ItemWithoutRarity(armorData, new ItemAttributes())
            {
                sockets = new ItemInstance[2],
            };

            Assert.AreEqual("Iron Helm", item.GetTooltipTitle());
            StringAssert.DoesNotContain("socket", item.GetTooltipTitle());
            StringAssert.DoesNotContain("Armor", item.GetTooltipTitle());
            StringAssert.DoesNotContain("Item Power", item.GetTooltipTitle());

            Object.DestroyImmediate(armorData);
        }
    }
}
#endif
