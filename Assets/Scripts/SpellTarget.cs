using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpellTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {

        if (!GameManagerScr.Instance.IsPlayerTurn)
            return;

        CardControllerScr spell = eventData.pointerDrag.GetComponent<CardControllerScr>(),
                          target = GetComponent<CardControllerScr>();

        if (spell && spell.Card.IsSpell &&spell.IsPlayerCard &&
            target.Card.IsPlaced && GameManagerScr.Instance.CurrentGame.Player.Mana >= spell.Card.Manacost)
        {
            var spellcard = (SpellCard)spell.Card;
            if ((spellcard.SpellTarget == SpellCard.TargetTaype.ALLY_CARD_TARGET && target.IsPlayerCard) || (spellcard.SpellTarget == SpellCard.TargetTaype.ENEMY_CARD_TARGET && !target.IsPlayerCard))
            {
                GameManagerScr.Instance.ReduceMana(true, spell.Card.Manacost);
                spell.UseSpell(target);
                GameManagerScr.Instance.CheckCardsForManaAvaliability();

            }
        }

    }
}
