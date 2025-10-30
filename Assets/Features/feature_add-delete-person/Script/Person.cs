using System;
using UnityEngine;

public class Person : MonoBehaviour
{
    [SerializeField] private PersonData data;   
    public PersonData Data => data;
    public int Id => data != null ? data.id : -1;
    public bool IsGhost => data != null && data.kind == PersonKind.Ghost;

    public bool IsSeated { get; private set; } = false;

    [Header("Visuals")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite standingSprite;
    [SerializeField] Sprite sittingSprite;
    [SerializeField] RuntimeAnimatorController standingAnim;
    [SerializeField] RuntimeAnimatorController sittingAnim;
    [SerializeField] Animator animator;

    void Awake()
    {
        if (data == null)
            Debug.LogError($"{name}: PersonData is not assigned on prefab!", this);
        DebugUtils.LogAllValues(data);
    }

    public void OnAddedToTaxi()
    {
        IsSeated = true;
        UpdateVisual();
    }

    public void OnRemovedFromTaxi()
    {
        IsSeated = false;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (spriteRenderer)
        {
            spriteRenderer.sprite = IsSeated ? sittingSprite : standingSprite;
        }

        if (animator)
        {
            animator.runtimeAnimatorController = IsSeated ? sittingAnim : standingAnim;
        }
    }

}
