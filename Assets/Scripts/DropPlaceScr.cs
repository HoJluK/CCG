using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum FieldType
{
    SELF_HAND,
    SELF_FIELD,
    ENEMY_HAND,
    ENEMY_FIELD
}

public class DropPlaceScr : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public FieldType Type;

    public void OnDrop(PointerEventData eventData)
    {
        if (Type != FieldType.SELF_FIELD)
            return;

        CardControllerScr card = eventData.pointerDrag.GetComponent<CardControllerScr>();

        if (card && GameManagerScr.Instance.IsPlayerTurn && GameManagerScr.Instance.CurrentGame.Player.Mana >= card.GetComponent<CardControllerScr>().Card.Manacost && !card.GetComponent<CardControllerScr>().Card.IsPlaced)
        {
            if(!card.Card.IsSpell)
                card.Movement.DefaultParent = transform;

            card.OnCast();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || Type == FieldType.ENEMY_FIELD || Type == FieldType.ENEMY_HAND || Type == FieldType.SELF_HAND)
            return;

        CardMovementScr card = eventData.pointerDrag.GetComponent<CardMovementScr>();

        if (card)
            card.DefaultTmpCardParent = transform;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardMovementScr card = eventData.pointerDrag.GetComponent<CardMovementScr>();

        if (card && card.DefaultTmpCardParent == transform)
            card.DefaultTmpCardParent = card.DefaultParent;
    }
}
