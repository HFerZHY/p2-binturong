using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A single tab button that communicates with its parent <see cref="TabGroup"/>.
///
/// CHANGES FROM ORIGINAL
///   • Removed the empty Update() method.
///   • background is populated in Awake (not Start) so it is safe to query
///     before the first frame.
///   • Subscribe to TabGroup happens in Awake so external code that calls
///     TabGroup.OnTabSelected in Start can rely on the button being registered.
///   • Added a null-guard on tabGroup in OnPointer* methods.
/// </summary>
[RequireComponent(typeof(Image))]
public class TabItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public TabGroup tabGroup;
    public Image    background;

    public Action<string> onTabSelected;
    public Action<string> onTabDeselected;

    private void Awake()
    {
        background = GetComponent<Image>();
    }

    private void Start()
    {
        if (tabGroup != null)
            tabGroup.Subscribe(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tabGroup?.OnTabEnter(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        tabGroup?.OnTabSelected(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tabGroup?.OnTabExit(this);
    }

    public void Select()
    {
        onTabSelected?.Invoke(gameObject.name);
    }

    public void Deselect()
    {
        onTabDeselected?.Invoke(gameObject.name);
    }
}
