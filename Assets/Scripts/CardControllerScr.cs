using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardControllerScr : MonoBehaviour
{
    public Card Card;

    public bool IsPlayerCard;

    public CardInfoScr Info;
    public CardMovementScr Movement;
    public CardAbility Ability;

    GameManagerScr gameManeger;

    public void Init(Card card, bool isPlayerCard)
    {
        Card = card;
        gameManeger = GameManagerScr.Instance;
        IsPlayerCard = isPlayerCard;
        if (isPlayerCard)
        {
            Info.ShowCardInfo();
            GetComponent<AttackedCard>().enabled = false;
        }
        else
            Info.HideCardInfo();
    }

    public void OnCast()
    {
        if (Card.IsSpell && ((SpellCard)Card).SpellTarget != SpellCard.TargetTaype.NO_TARGET)
            return;


        if (IsPlayerCard)
        {
            gameManeger.PlayerHandsCards.Remove(this);
            gameManeger.PlayerFieldCards.Add(this);
            gameManeger.ReduceMana(true, Card.Manacost);
            gameManeger.CheckCardsForManaAvaliability();
        }
        else
        {
            gameManeger.EnemyHandsCards.Remove(this);
            gameManeger.EnemyFieldCards.Add(this);
            gameManeger.ReduceMana(false, Card.Manacost);
            Info.ShowCardInfo();
        }

        Card.IsPlaced = true;

        if (Card.HasAbility)
            Ability.OnCast();

        if (Card.IsSpell)
            UseSpell(null);

        UIController.Instance.UpdateHPAndMana();
    }

    public void UseSpell(CardControllerScr target)
    {
        var spellCard = (SpellCard)Card;
        switch (spellCard.Spell)
        {
            case SpellCard.SpellType.HEAL_ALL_FIELD_CARDS:
                var allCards = IsPlayerCard ? gameManeger.PlayerFieldCards : gameManeger.EnemyFieldCards;
                foreach (var card in allCards)
                {
                    card.Card.Defense += spellCard.SpellValue;
                    card.Info.RefreshData();
                }
                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                var enemyCards = IsPlayerCard ? new List<CardControllerScr>(gameManeger.EnemyFieldCards) : new List<CardControllerScr>(gameManeger.PlayerFieldCards);
                foreach (var card in enemyCards)
                    GiveDamageTo(card, spellCard.SpellValue);
                break;

            case SpellCard.SpellType.HEAL_ALLY_HERO:
                if (IsPlayerCard)
                    gameManeger.CurrentGame.Player.HP += spellCard.SpellValue;
                else
                    gameManeger.CurrentGame.Enemy.HP += spellCard.SpellValue;

                UIController.Instance.UpdateHPAndMana();
                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_HERO:
                if (IsPlayerCard)
                    gameManeger.CurrentGame.Enemy.HP -= spellCard.SpellValue;
                else
                    gameManeger.CurrentGame.Player.HP -= spellCard.SpellValue;

                UIController.Instance.UpdateHPAndMana();
                gameManeger.CheckForResult();
                break;

            case SpellCard.SpellType.HEAL_ALLY_CARD:
                target.Card.Defense += spellCard.SpellValue;
                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_CARD:
                GiveDamageTo(target, spellCard.SpellValue);
                break;

            case SpellCard.SpellType.SHIELD_ON_ALLY_CARD:
                if (!target.Card.Abilities.Exists(x => x == Card.AbilityType.SHIELD))
                    target.Card.Abilities.Add(Card.AbilityType.SHIELD);
                break;

            case SpellCard.SpellType.PROVOCATION_ON_ALLY_CARD:
                if (!target.Card.Abilities.Exists(x => x == Card.AbilityType.PROVOCATION))
                    target.Card.Abilities.Add(Card.AbilityType.PROVOCATION);
                break;

            case SpellCard.SpellType.BUFF_CARD_DAMAGE:
                target.Card.Attack += spellCard.SpellValue;
                break;

            case SpellCard.SpellType.DEBUFF_CARD_DAMAGE:
                target.Card.Attack = Mathf.Clamp(target.Card.Attack - spellCard.SpellValue, 0, int.MaxValue);
                break;
        }
        if (target != null)
        {
            target.Ability.OnCast();
            target.CheckForAlive();
        }
        DestroyCard();
    }

    void GiveDamageTo(CardControllerScr card, int damage)
    {
        card.Card.GetDamag(damage);
        card.CheckForAlive();
        card.OnTakeDamage();
    }

    public void OnTakeDamage(CardControllerScr attacker = null)
    {
        CheckForAlive();
        Ability.OnDamageTake(attacker);
    }

    public void OnDamageDeal()
    {
        Card.TimesDealedDamage++;
        Card.CanAttack = false;
        Info.HighlightCard(false);
        if (Card.HasAbility)
            Ability.OnDamageDeal();

    } 

    public void CheckForAlive()
    {
        if (Card.IsAlive)
        {
            Info.RefreshData();
        }
        else
            DestroyCard();
    }

    public void DestroyCard()
    {
        Movement.OnEndDrag(null);

        RemoveCardFromList(gameManeger.EnemyFieldCards);
        RemoveCardFromList(gameManeger.EnemyHandsCards);
        RemoveCardFromList(gameManeger.PlayerFieldCards);
        RemoveCardFromList(gameManeger.PlayerHandsCards);

        Destroy(gameObject);

    }

    void RemoveCardFromList(List<CardControllerScr> list)
    {
        if (list.Exists(x => x == this))
            list.Remove(this);
    }

}
