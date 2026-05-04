using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a set of <see cref="TabItem"/> buttons.
///
/// USAGE MODES
///
///   A) Pre-existing hierarchy (recommended)
///      Place TabGroup on a GameObject in the scene (e.g. TabBar with a
///      Horizontal Layout Group).  Assign <see cref="buttonPrefab"/> in the
///      Inspector, then call <see cref="RebuildTabs"/> with a list of
///      <see cref="TabDefinition"/> descriptors at runtime.  TabGroup will
///      spawn/reuse/remove child TabItem GameObjects as needed.
///
///   B) Fully procedural
///      Call the static <see cref="CreateTabGroup"/> factory, which creates the
///      parent GameObject, attaches TabGroup, then calls RebuildTabs internally.
/// </summary>
public class TabGroup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("Prefab instantiated for each tab. Must have a TabItem component " +
             "and a TextMeshProUGUI somewhere in its hierarchy for the label.")]
    public GameObject buttonPrefab;

    [Tooltip("If true, one tab is always selected. Selects the first tab automatically.")]
    public bool requireSelection = false;

    // ── Runtime state ─────────────────────────────────────────────────────────

    /// <summary>All currently active tab buttons, in order.</summary>
    public List<TabItem> tabButtons { get; private set; } = new();

    /// <summary>The currently selected tab, or null if none.</summary>
    public TabItem selectedTab { get; private set; }

    /// <summary>Fired when a tab is selected. Argument is the tab's GameObject name.</summary>
    public Action<string> onTabChange;

    // ── Tab definition ────────────────────────────────────────────────────────

    [Serializable]
    public class TabDefinition
    {
        /// <summary>GameObject name assigned to the spawned button.</summary>
        public string id;
        /// <summary>Label shown on the button.</summary>
        public string label;
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        if (requireSelection && tabButtons.Count > 0)
            OnTabSelected(tabButtons[0]);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns or reuses one child button per entry in <paramref name="definitions"/>,
    /// removes surplus buttons, and wires each to this TabGroup.
    /// Restores the previously selected tab by id if it still exists after rebuild.
    /// </summary>
    public void RebuildTabs(List<TabDefinition> definitions, GameObject prefabOverride = null)
    {
        if (definitions == null || definitions.Count == 0)
        {
            ClearAllTabs();
            return;
        }

        var prefab = prefabOverride != null ? prefabOverride : buttonPrefab;
        if (prefab == null)
        {
            Debug.LogError("[TabGroup] No buttonPrefab assigned. Cannot rebuild tabs.", this);
            return;
        }

        string previouslySelectedId = selectedTab != null ? selectedTab.gameObject.name : null;

        // Collect existing TabItem children for pooling.
        var existing = new List<TabItem>();
        foreach (Transform child in transform)
        {
            var t = child.GetComponent<TabItem>();
            if (t != null) existing.Add(t);
        }

        tabButtons.Clear();
        selectedTab = null;

        for (int i = 0; i < definitions.Count; i++)
        {
            TabItem tab;
            if (i < existing.Count)
            {
                tab = existing[i];
                tab.gameObject.SetActive(true);
            }
            else
            {
                var go = Instantiate(prefab, transform);
                tab = go.GetComponent<TabItem>();
                if (tab == null)
                {
                    Debug.LogError(
                        $"[TabGroup] buttonPrefab '{prefab.name}' has no TabItem component.", this);
                    Destroy(go);
                    continue;
                }
            }

            tab.gameObject.name = definitions[i].id;
            tab.tabGroup        = this;

            var label = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = definitions[i].label;

            tabButtons.Add(tab);
        }

        // Deactivate surplus pooled buttons.
        for (int i = definitions.Count; i < existing.Count; i++)
            existing[i].gameObject.SetActive(false);

        // Restore or default selection.
        if (requireSelection)
        {
            var toSelect = tabButtons.Find(t => t.gameObject.name == previouslySelectedId)
                           ?? tabButtons[0];
            OnTabSelected(toSelect);
        }
    }

    /// <summary>
    /// Registers a <see cref="TabItem"/> manually (called from TabItem.Start
    /// when tabs are placed in the scene without RebuildTabs).
    /// </summary>
    public void Subscribe(TabItem button)
    {
        if (!tabButtons.Contains(button))
            tabButtons.Add(button);
    }

    public void OnTabEnter(TabItem button)
    {
        ResetTabs();
        // if (selectedTab == null || button != selectedTab)
            // button.transform.localScale = Vector3.one * 0.9f;
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
        // button.transform.localScale = Vector3.one * 1.1f;
    }

    public void ResetTabs()
    {
        if (tabButtons == null) return;
        foreach (var button in tabButtons)
        {
            if (button == null) continue;
            if (selectedTab != null && selectedTab == button) continue;
            button.transform.localScale = Vector3.one;
        }
    }

    // ── Static factory ────────────────────────────────────────────────────────

    public static TabGroup CreateTabGroup(
        GameObject          parent,
        string              name,
        List<TabDefinition> definitions,
        GameObject          tabGroupPrefab   = null,
        GameObject          buttonPrefab     = null,
        bool                requireSelection = false)
    {
        var go = tabGroupPrefab != null
            ? Instantiate(tabGroupPrefab, parent.transform)
            : new GameObject(name, typeof(RectTransform));

        go.name             = name;
        go.transform.parent = parent.transform;

        var group = go.GetComponent<TabGroup>() ?? go.AddComponent<TabGroup>();
        group.requireSelection = requireSelection;
        if (buttonPrefab != null)
            group.buttonPrefab = buttonPrefab;

        group.RebuildTabs(definitions);
        return group;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void ClearAllTabs()
    {
        foreach (var t in tabButtons)
            if (t != null) t.gameObject.SetActive(false);

        tabButtons.Clear();
        selectedTab = null;
    }
}