using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game
{
    public Player Player, Enemy;
    public List<Card> EnemyDeck, PlayerDeck;

    public Game()
    {
        EnemyDeck = GiveDeckCard();
        PlayerDeck = GiveDeckCard();

        Player = new Player();
        Enemy = new Player();
    }

    List<Card> GiveDeckCard()
    {
        List<Card> list = new List<Card>();
        list.Add(CardManager.AllCards[6].GetCopy());
        for (int i = 0; i < 20; i++)
        {
            var card = CardManager.AllCards[Random.Range(0, CardManager.AllCards.Count)];

            if (card.IsSpell)
                list.Add(((SpellCard)card).GetCopy());
            else
                list.Add(card.GetCopy());

        }
        return list;
    }
}


public class GameManagerScr : MonoBehaviour
{

    public static GameManagerScr Instance;

    public Game CurrentGame;
    public Transform EnemyHand, PlayerHand,
                     EnemyField, PlayerField;
    public GameObject CardPref;
    int Turn, TurnTime = 30;

    public AttackedHero EnemyHero, PlayerHero;
    public AI EnemyAI;
    public bool IsPlayerTurn
    {
        get
        {
            return Turn % 2 == 0;
        }
    }
    public List<CardControllerScr> PlayerHandsCards = new List<CardControllerScr>(),
                                   PlayerFieldCards = new List<CardControllerScr>(),
                                   EnemyHandsCards = new List<CardControllerScr>(),
                                   EnemyFieldCards = new List<CardControllerScr>();


    void Awake()
    {
        if(Instance == null)
        Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        foreach (var card in PlayerHandsCards)
            Destroy(card.gameObject);
        foreach (var card in EnemyFieldCards)
            Destroy(card.gameObject);
        foreach (var card in PlayerFieldCards)
            Destroy(card.gameObject);
        foreach (var card in EnemyHandsCards)
            Destroy(card.gameObject);

        PlayerHandsCards.Clear();
        PlayerFieldCards.Clear();
        EnemyHandsCards.Clear();
        EnemyFieldCards.Clear();

        StartGame();
    }

    public void StartGame()
    {

        Turn = 0;
        

        CurrentGame = new Game();

        GiveHandCards(CurrentGame.EnemyDeck, EnemyHand);
        GiveHandCards(CurrentGame.PlayerDeck, PlayerHand);

        UIController.Instance.StartGame();

        StartCoroutine(TurnFunc());
    }

    void GiveHandCards(List<Card> deck, Transform hand)
    {
        int i = 0;
        while (i++ < 4)
            GiveCardToHand(deck, hand);
    }

    void GiveCardToHand(List<Card> deck, Transform hand)
    {
        if (deck.Count == 0)
            return;

        CreateCardPref(deck[0], hand);

        deck.RemoveAt(0);
    }

    void CreateCardPref(Card card, Transform hand)
    {
        GameObject cardGO = Instantiate(CardPref, hand, false);
        CardControllerScr cardC = cardGO.GetComponent<CardControllerScr>();

        cardC.Init(card, hand == PlayerHand);
        if (cardC.IsPlayerCard)
            PlayerHandsCards.Add(cardC);
        else
            EnemyHandsCards.Add(cardC);
    }

    IEnumerator TurnFunc()
    {
        TurnTime = 30;
        UIController.Instance.UpdateTurnTime(TurnTime);
        foreach (var card in PlayerFieldCards)
            card.Info.HighlightCard(false);

        CheckCardsForManaAvaliability();

        if (IsPlayerTurn)
        {
            foreach (var card in PlayerFieldCards)
            {
                card.Card.CanAttack = true;
                card.Info.HighlightCard(true);
                card.Ability.OnNewTurn();
            }
            while (TurnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(TurnTime);
                yield return new WaitForSeconds(1);
            }
            ChangeTurn();
        }
        else
        {
            foreach (var card in EnemyFieldCards)
            {
                card.Card.CanAttack = true;
                card.Ability.OnNewTurn();
            }

            EnemyAI.MakeTurn();
            while (TurnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(TurnTime);
                yield return new WaitForSeconds(1);
            }
            ChangeTurn();
        }

        
    }

    

    public void ChangeTurn()
    {
        StopAllCoroutines();

        Turn++;

        UIController.Instance.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            GiveNewCards();
            CurrentGame.Player.IncreasseManapool();
            CurrentGame.Player.RestoreRoundMana();
            UIController.Instance.UpdateHPAndMana();
        }
        else
        {
            CurrentGame.Enemy.IncreasseManapool();
            CurrentGame.Enemy.RestoreRoundMana();
        }
        StartCoroutine(TurnFunc());
    }

    void GiveNewCards()
    {
        GiveCardToHand(CurrentGame.EnemyDeck, EnemyHand);
        GiveCardToHand(CurrentGame.PlayerDeck, PlayerHand);
    }

    public void CardsFight(CardControllerScr attacker, CardControllerScr defender)
    {
        defender.Card.GetDamag(attacker.Card.Attack);
        attacker.OnDamageDeal();
        defender.OnTakeDamage(attacker);
        attacker.Card.GetDamag(defender.Card.Attack);
        attacker.OnTakeDamage();
        attacker.CheckForAlive();
        defender.CheckForAlive();
    }

  

    public void ReduceMana(bool playerMana, int manacost)
    {
        if (playerMana)
            CurrentGame.Player.Mana -= manacost;
        else
            CurrentGame.Enemy.Mana -= manacost;
        UIController.Instance.UpdateHPAndMana();
    }

    public void DamageHero(CardControllerScr card, bool isEnemyAttacked)
    {
        if (isEnemyAttacked)
            CurrentGame.Enemy.GetDamage(card.Card.Attack);
        else
            CurrentGame.Player.GetDamage(card.Card.Attack);

        UIController.Instance.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckForResult()
    {
        if (CurrentGame.Enemy.HP == 0 || CurrentGame.Player.HP ==0)
        {
            StopAllCoroutines();
            UIController.Instance.ShowResult();
        }
       
    }

    public void CheckCardsForManaAvaliability()
    {
        foreach (var card in PlayerHandsCards)
            card.Info.HighlightManaAvalibality(CurrentGame.Player.Mana);
    }

    public void HighlightTargets(CardControllerScr attacker, bool highlight)
    {
        List<CardControllerScr> targets = new List<CardControllerScr>();
        if (attacker.Card.IsSpell)
        {
            var spellCard = (SpellCard)attacker.Card;
            if (spellCard.SpellTarget == SpellCard.TargetTaype.NO_TARGET)
                targets = new List<CardControllerScr>();
            else if (spellCard.SpellTarget == SpellCard.TargetTaype.ALLY_CARD_TARGET)
                targets = PlayerFieldCards;
            else
                targets = EnemyFieldCards;
        }
        else
        { if (EnemyFieldCards.Exists(x => x.Card.IsProvocation))
                targets = EnemyFieldCards.FindAll(x => x.Card.IsProvocation);
            else
            {
                targets = EnemyFieldCards;
                if(!attacker.Card.IsSpell)
                    EnemyHero.HighlightAsTarget(highlight);
            }
        }
        foreach (var card in targets)
        {
            if (attacker.Card.IsSpell)
                card.Info.HighlightAsSpellTarget(highlight);
            else
                card.Info.HighlightAsTarget(highlight);
        }
    }
    public void Exit()
    {
            Application.Quit();
    }
}
