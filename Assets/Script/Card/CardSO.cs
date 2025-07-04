using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum ECardTag
{
    GakuenIdolmaster,
    KamenRider,
    The100Girlfriends,
}
public enum ECardType
{
    Character,
    Event,
    Field,
    Raid,
}

public enum EcardTrigger
{
    None,
    Get,
    Color,
    Raid,
    Draw,
    Active,
    Final,
    Special,
}

public enum EcardColor
{
    Blue,
    Red,
    Yellow,
    Black,
}

public enum EcardRare
{
    C,
    U,
    UP,
    R,
    RP,
    SR,
    SRP, 
    SRPP,
}



[System.Serializable]
public class Card
{
    [Header("카드 정보")]
    public int cardId;
    public Sprite artwork;
    public ECardTag cardTag;
    public string cardName;
    public EcardRare cardRare;
    public ECardType cardType;
    public EcardColor cardColor;
    public int cardUseCost;
    public int cardUseActivePoint;
    public int cardMakeCost;
    [Multiline(6)]
    public string cardExplanation;
    public int cardBattlePoint;
    public EcardTrigger cardTrigger;
    [Header("카드 효과")]
    public List<ECardEffect> cardEffects;
}

[CreateAssetMenu(fileName ="NewCard", menuName = "Deck/Card")]
public class CardSO : ScriptableObject
{
    public List<Card> cards = new List<Card>();
}
