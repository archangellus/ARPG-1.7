namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Flags that identify item types/slots. Used to restrict which item types an affix entry
    /// or a Socketable's attribute bonus applies to.
    /// </summary>
    [System.Flags]
    public enum ItemScope
    {
        None = 0,
        Blade = 1 << 0,
        Bow = 1 << 1,

        /// <summary>Convenience flag — matches both <see cref="Blade"/> and <see cref="Bow"/>.</summary>
        Weapon = Blade | Bow,

        Helm = 1 << 2,
        Chest = 1 << 3,
        Pants = 1 << 4,
        Gloves = 1 << 5,
        Boots = 1 << 6,

        /// <summary>Convenience flag — matches all specific armor slots.</summary>
        Armor = Helm | Chest | Pants | Gloves | Boots,
        Shield = 1 << 7,
        Ring = 1 << 8,
        Amulet = 1 << 9,
    }
}
