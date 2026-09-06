namespace PLAYERTWO.ARPGProject
{
    public class LevelUp : ConsoleAction
    {
        public override void Execute(string[] args)
        {
            var nextLevelExperience = currentCharacter.entity.stats.nextLevelExp;
            var currentExperience = currentCharacter.entity.stats.experience;
            currentCharacter.entity.stats.AddExperience(nextLevelExperience - currentExperience);
            Console.LogSuccess($"Leveled up \"{currentCharacter.name}\".");
        }
    }

    public class AddExperience : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.AddExperience(amount);
            Console.LogSuccess($"Added \"{amount}\" experience to \"{currentCharacter.name}\".");
        }
    }

    public class ResetExperience : ConsoleAction
    {
        public override void Execute(string[] args)
        {
            currentCharacter.entity.stats.ResetExperience();
            Console.LogSuccess($"Reset experience for \"{currentCharacter.name}\".");
        }
    }

    public class AddHealth : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.health += amount;
            Console.LogSuccess($"Added \"{amount}\" health to \"{currentCharacter.name}\".");
        }
    }

    public class SubHealth : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.health -= amount;
            Console.LogSuccess($"Subtracted \"{amount}\" health from \"{currentCharacter.name}\".");
        }
    }

    public class AddMana : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.mana += amount;
            Console.LogSuccess($"Added \"{amount}\" mana to \"{currentCharacter.name}\".");
        }
    }

    public class SubMana : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.mana -= amount;
            Console.LogSuccess($"Subtracted \"{amount}\" mana from \"{currentCharacter.name}\".");
        }
    }

    public class AddStrength : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.strength += amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Added \"{amount}\" strength to \"{currentCharacter.name}\".");
        }
    }

    public class SubStrength : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.strength -= amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess(
                $"Subtracted \"{amount}\" strength from \"{currentCharacter.name}\"."
            );
        }
    }

    public class SetStrength : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.strength = amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Set strength to \"{amount}\" for \"{currentCharacter.name}\".");
        }
    }

    public class AddDexterity : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.dexterity += amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Added \"{amount}\" dexterity to \"{currentCharacter.name}\".");
        }
    }

    public class SubDexterity : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.dexterity -= amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess(
                $"Subtracted \"{amount}\" dexterity from \"{currentCharacter.name}\"."
            );
        }
    }

    public class SetDexterity : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.dexterity = amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Set dexterity to \"{amount}\" for \"{currentCharacter.name}\".");
        }
    }

    public class AddVitality : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.vitality += amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Added \"{amount}\" vitality to \"{currentCharacter.name}\".");
        }
    }

    public class SubVitality : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.vitality -= amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess(
                $"Subtracted \"{amount}\" vitality from \"{currentCharacter.name}\"."
            );
        }
    }

    public class SetVitality : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.vitality = amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Set vitality to \"{amount}\" for \"{currentCharacter.name}\".");
        }
    }

    public class AddEnergy : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.energy += amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Added \"{amount}\" energy to \"{currentCharacter.name}\".");
        }
    }

    public class SubEnergy : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.energy -= amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Subtracted \"{amount}\" energy from \"{currentCharacter.name}\".");
        }
    }

    public class SetEnergy : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.stats.energy = amount;
            currentCharacter.entity.stats.Recalculate();
            Console.LogSuccess($"Set energy to \"{amount}\" for \"{currentCharacter.name}\".");
        }
    }

    public class Revitalize : ConsoleAction
    {
        public override void Execute(string[] args)
        {
            currentCharacter.entity.stats.Revitalize();
            Console.LogSuccess($"Revitalized \"{currentCharacter.name}\".");
        }
    }

    public class SetInfinityHealth : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<true|false>";

        public override void Execute(string[] args)
        {
            var value = bool.Parse(args[1]);
            currentCharacter.entity.stats.infiniteHealth = value;
            Console.LogSuccess(
                $"Infinity health set to \"{value}\" for \"{currentCharacter.name}\"."
            );
        }
    }

    public class SetInfinityMana : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<true|false>";

        public override void Execute(string[] args)
        {
            var value = bool.Parse(args[1]);
            currentCharacter.entity.stats.infiniteMana = value;
            Console.LogSuccess(
                $"Infinity mana set to \"{value}\" for \"{currentCharacter.name}\"."
            );
        }
    }

    public class SetImmuneToStun : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<true|false>";

        public override void Execute(string[] args)
        {
            var value = bool.Parse(args[1]);
            currentCharacter.entity.stats.immuneToStun = value;
            Console.LogSuccess(
                $"Immune to stun set to \"{value}\" for \"{currentCharacter.name}\"."
            );
        }
    }

    public class PrintPosition : ConsoleAction
    {
        public override void Execute(string[] args)
        {
            var position = currentCharacter.entity.transform.position;
            Console.Log(
                $"Current position of \"{currentCharacter.name}\": {position.x}, {position.y}, {position.z}"
            );
        }
    }

    public class AddMoney : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.inventory.instance.money += amount;
            Console.LogSuccess($"Added \"{amount}\" money to \"{currentCharacter.name}\".");
        }
    }

    public class SubMoney : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.inventory.instance.money -= amount;
            Console.LogSuccess($"Subtracted \"{amount}\" money from \"{currentCharacter.name}\".");
        }
    }

    public class SetMoney : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<amount>";

        public override void Execute(string[] args)
        {
            var amount = int.Parse(args[1]);
            currentCharacter.entity.inventory.instance.money = amount;
            Console.LogSuccess($"Set money to \"{amount}\" for \"{currentCharacter.name}\".");
        }
    }

    public class SpawnItem : ConsoleAction
    {
        public override string Usage => $"{Command}\t\t<itemId|name> [rarityId] [socketCount]";

        public override void Execute(string[] args)
        {
            var item = ResolveItem(args[1]);

            if (item == null)
            {
                Console.LogError($"No item found matching \"{args[1]}\".");
                return;
            }

            var rarityId = args.Length > 2 ? int.Parse(args[2]) : -1;
            var socketCount = args.Length > 3 ? int.Parse(args[3]) : 0;

            var instance =
                rarityId >= 0
                    ? new ItemInstance(item, rarityId, socketCount)
                    : new ItemInstance(item);

            var position = currentCharacter.entity.transform.position;
            Level.instance.InstantiateItemDrop(instance, position);

            Console.LogSuccess($"Spawned \"{item.name}\" at \"{currentCharacter.name}\"'s position.");
        }

        /// <summary>
        /// Resolves an Item by its database id, or, when the identifier isn't a number,
        /// by a case-insensitive partial match against the item's name.
        /// </summary>
        protected virtual Item ResolveItem(string identifier)
        {
            if (int.TryParse(identifier, out var itemId))
                return GameDatabase.instance.FindElementById<Item>(itemId);

            return GameDatabase.instance.items.Find(item =>
                item != null
                && item.name.IndexOf(identifier, System.StringComparison.OrdinalIgnoreCase) >= 0
            );
        }
    }
}
