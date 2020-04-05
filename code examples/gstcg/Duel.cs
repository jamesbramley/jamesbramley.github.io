
using System.Runtime.InteropServices;
using UnityEngine;
using System;

public class Duel
{
    private System.Random rand = new System.Random();
    public FieldInfo currentFieldInfo = GameObject.Find("FieldCanvas").GetComponent<FieldInfo>();
    public HandInfo currentHandInfo = GameObject.Find("HandCards").GetComponent<HandInfo>();
    public PlayerInfo currentPlayersInfo = GameObject.Find("HandCanvas").GetComponent<PlayerInfo>();

    public Player player1;

    public Player player2;

    public Player currentPlayer;

    private int turnNumber;
    private Phases.PhaseID currentPhase;

    private int mpTurnIncrease = 7;

    public int turnsSinceLastPhaseChange;

    private int phaseDuration = 4; // How long each phase lasts.

    public void SetParticipants(Player p1, Player p2)
    {
        player1 = p1;
        player2 = p2;

        player1.number = 1;
        player2.number = 2;
    }

    public Phases.PhaseID GetCurrentPhase()
    {
        return currentPhase;
    }

    public void SetPhaseDuration(int duration)
    {
        phaseDuration = duration;
    }
    
    public void StartDuel()
    {
        
        currentPlayersInfo.SetPlayerName(1, player1.name);
        currentPlayersInfo.SetPlayerName(2, player2.name);

        player1.deck = PlayerStatus.currentSelectedDeck;
        Debug.Log(player1.deck.Name);
        
        player1.deck.Shuffle();
        player2.deck.Shuffle();
        
        player1.DrawCards(4);
        player2.DrawCards(4);
        player1.mp = 0;
        player2.mp = 0;
        currentPhase = Phases.PhaseID.PostGharb;
        turnsSinceLastPhaseChange = 0;
        
        BeginTurn(player1);
    }
    

    private Player SelectFirstPlayer()
    {
        var n = rand.Next(1);
        switch (n)
        {
            case 0:
                return player1;
            case 1:
                return player2;
        }

        // Fallback
        return player1;
    }

    public void IncreaseMP(Player player, int mp)
    {
        player.mp += mp;
        if (player.mp > 25)
        {
            player.mp = 25;
        }
        currentPlayersInfo.GetPlayerMPUI(player.number).text = player.mp.ToString();
    }

    public Player GetPlayerFromNumber(int num)
    {
        if (num == 1)
        {
            return player1;
        }

        return player2;
    }

    private void BeginTurn(Player player)
    {
        IncreaseMP(player, mpTurnIncrease);
        
        currentPlayersInfo.GetPlayerNameUI(player.number).color = Color.green;
        turnNumber += 1;
        turnsSinceLastPhaseChange += 1;
        if (phaseDuration - turnsSinceLastPhaseChange <= 0)
        {
            AdvancePhase(currentPhase);
            turnsSinceLastPhaseChange = 0;
        }
        currentPlayer = player;
        currentPlayer.DrawCards();
        currentHandInfo.UpdateHandDisplay(player.handData);
    }

    public void EndTurn(Player player)
    {
        var nextPlayer = (player == player1) ? player2 : player1;
        currentPlayersInfo.GetPlayerNameUI(player.number).color = Color.white;
        BeginTurn(nextPlayer);
    }

    public void QuickMPIncrement()
    {
        IncreaseMP(player1, mpTurnIncrease);
        turnNumber += 1;
        turnsSinceLastPhaseChange += 1;
        if (phaseDuration - turnsSinceLastPhaseChange <= 0)
        {
            AdvancePhase(currentPhase);
            turnsSinceLastPhaseChange = 0;
        }
    }

    // Ordinary Phase Advance.
    public void AdvancePhase(Phases.PhaseID phase)
    {
        switch (phase)
        {
                case Phases.PhaseID.PostGharb:
                    currentPhase = Phases.PhaseID.Baking;
                    break;
                case Phases.PhaseID.Baking:
                    currentPhase = Phases.PhaseID.Gamesoc;
                    break;
                case Phases.PhaseID.Gamesoc:
                    currentPhase = Phases.PhaseID.LateGamesoc;
                    break;
                case Phases.PhaseID.LateGamesoc:
                    currentPhase = Phases.PhaseID.PostGharb;
                    break;
        }
    }

    public void SetCurrentPhase(Phases.PhaseID phase)
    {
        currentPhase = phase;
    }
    
}
