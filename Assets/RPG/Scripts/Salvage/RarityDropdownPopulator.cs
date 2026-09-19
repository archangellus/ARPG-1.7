using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PLAYERTWO.ARPGProject;

[RequireComponent(typeof(Dropdown))]
public class RarityDropdownPopulator : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;

    private bool populated;

    private void Reset()   => dropdown = GetComponent<Dropdown>();
    private void Awake()   { if (dropdown == null) dropdown = GetComponent<Dropdown>(); }
    private void OnEnable() { if (!populated) Populate(); }

    public void Populate()
    {
        var rarities = GameDatabase.instance.itemRarities;

        var options = new List<Dropdown.OptionData>(rarities.Count + 1);
        options.Add(new Dropdown.OptionData("None"));          // index 0 -> rarityId -1

        for (int i = 0; i < rarities.Count; i++)              // index i+1 -> rarityId i
            options.Add(new Dropdown.OptionData(rarities[i].name));

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();

        populated = true;
    }
}