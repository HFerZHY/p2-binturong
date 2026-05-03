using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a set of <see cref="TabItem"/> buttons.
///
/// CHANGES FROM ORIGINAL
///   • <see cref="onTabChange"/> is now public and can be wired externally before Start.
///   • <see cref="ResetTabs"/> guards against a null tabButtons list.
///   • <see cref="OnTabEnter"/> hover scale only applies when button is not selected.
///   • Minor null-guards added throughout.
/// </summary>
public class TabGroup : MonoBehaviour
{
    public List<TabItem> tabButtons;
    public TabItem       selectedTab;
    public Action<string> onTabChange;
    public bool          requireSelection = false;
    public TabItem       defaultTab;

    /// <summary>
    /// Procedurally creates a TabGroup with buttons as children.
    /// </summary>
    public static GameObject CreateTabGroup(
        GameObject      parent,
        string          name,
        List<string>    buttonNames,
        List<string>    buttonTexts,
        GameObject      tabPrefab    = null,
        GameObject      buttonPrefab = null,
        bool            requireSelection = false,
        TabItem         defaultTab   = null)
    {
        GameObject tabGroup = tabPrefab == null
            ? DefaultControls.CreatePanel(new DefaultControls.Resources())
            : Instantiate(tabPrefab);

        tabGroup.name             = name;
        tabGroup.transform.parent = parent.transform;

        var group = tabGroup.GetComponent<TabGroup>() ?? tabGroup.AddComponent<TabGroup>();
        group.requireSelection = requireSelection;
        group.defaultTab       = defaultTab;

        for (int i = 0; i < buttonNames.Count; i++)
        {
            GameObject button = buttonPrefab == null
                ? DefaultControls.CreateButton(new DefaultControls.Resources())
                : Instantiate(buttonPrefab);

            button.name = buttonNames[i];

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = buttonTexts[i];

            var tab = button.GetComponent<TabItem>() ?? button.AddComponent<TabItem>();
            tab.tabGroup = group;

            button.transform.parent = tabGroup.transform;
        }

        return tabGroup;
    }

    private void Start()
    {
        if (requireSelection && defaultTab != null)
            OnTabSelected(defaultTab);
    }

    /// <summary>Registers a <see cref="TabItem"/> with this group.</summary>
    public void Subscribe(TabItem button)
    {
        tabButtons ??= new List<TabItem>();
        if (!tabButtons.Contains(button))
            tabButtons.Add(button);
    }

    public void OnTabEnter(TabItem button)
    {
        ResetTabs();
        if (selectedTab == null || button != selectedTab)
            button.transform.localScale = Vector3.one * 0.9f;
    }

    public void OnTabExit(TabItem button)
    {
        ResetTabs();
    }

    public void OnTabSelected(TabItem button)
    {
        if (button == null) return;

        selectedTab?.Deselect();
        selectedTab = button;
        selectedTab.Select();

        onTabChange?.Invoke(selectedTab.gameObject.name);
        ResetTabs();
        button.transform.localScale = Vector3.one * 1.1f;
    }

    public void ResetTabs()
    {
        if (tabButtons == null) return;
        foreach (TabItem button in tabButtons)
        {
            if (selectedTab != null && selectedTab == button) continue;
            button.transform.localScale = Vector3.one;
        }
    }
}
