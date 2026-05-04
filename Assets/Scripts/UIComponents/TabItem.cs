using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A single tab button that communicates with its parent <see cref="TabGroup"/>.
///
/// Requires a <see cref="Button"/> component (which itself contains an Image as
/// its targetGraphic), giving built-in visual transition states for free.
///
/// Pointer events handle mouse interaction.
/// <see cref="ISelectHandler"/> / <see cref="IDeselectHandler"/> mirror the same
/// callbacks for gamepad and keyboard navigation, where hover does not exist.
/// <see cref="ISubmitHandler"/> confirms selection via gamepad/keyboard.
/// </summary>
[RequireComponent(typeof(Button))]
public class TabItem : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    public TabGroup tabGroup;

    /// <summary>The Button component on this GameObject.</summary>
    public Button button { get; private set; }

    /// <summary>Fired when this tab becomes selected. Argument is the GameObject name.</summary>
    public Action<string> onTabSelected;

    /// <summary>Fired when this tab is deselected. Argument is the GameObject name.</summary>
    public Action<string> onTabDeselected;

    /// <summary>
    /// Fired on pointer enter and on EventSystem select (gamepad/keyboard focus).
    /// Use this to show a detail panel or preview without committing a selection.
    /// </summary>
    public Action<string> onTabHighlighted;

    /// <summary>
    /// Fired on pointer exit and on EventSystem deselect.
    /// Use this to hide a detail panel that was shown by <see cref="onTabHighlighted"/>.
    /// </summary>
    public Action<string> onTabUnhighlighted;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Self-register when placed manually in the scene.
        // RebuildTabs sets tabGroup and manages the list itself, making this a no-op.
        if (tabGroup != null)
            tabGroup.Subscribe(this);
    }

    // ── Pointer (mouse) ───────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        tabGroup?.OnTabEnter(this);
        onTabHighlighted?.Invoke(gameObject.name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tabGroup?.OnTabExit(this);
        onTabUnhighlighted?.Invoke(gameObject.name);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        tabGroup?.OnTabSelected(this);
    }

    // ── EventSystem navigation (gamepad / keyboard) ───────────────────────────

    /// <summary>EventSystem focus moved onto this button (e.g. gamepad stick).</summary>
    public void OnSelect(BaseEventData eventData)
    {
        tabGroup?.OnTabEnter(this);
        onTabHighlighted?.Invoke(gameObject.name);
    }

    /// <summary>EventSystem focus moved away from this button.</summary>
    public void OnDeselect(BaseEventData eventData)
    {
        tabGroup?.OnTabExit(this);
        onTabUnhighlighted?.Invoke(gameObject.name);
    }

    /// <summary>Gamepad/keyboard confirm pressed while this button has focus.</summary>
    public void OnSubmit(BaseEventData eventData)
    {
        tabGroup?.OnTabSelected(this);
    }

    // ── Selection state ───────────────────────────────────────────────────────

    public void Select()   => onTabSelected?.Invoke(gameObject.name);
    public void Deselect() => onTabDeselected?.Invoke(gameObject.name);
}