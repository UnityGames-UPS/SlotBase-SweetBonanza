namespace SweetBonanza.Model
{
    public enum SymbolType
    {
        Banana = 0,
        Grapes = 1,
        Watermelon = 2,
        Plum = 3,
        Apple = 4,
        BlueOval = 5,
        GreenPentagon = 6,
        PurpleSquare = 7,
        RedHeart = 8,
        Lollipop = 9,
        MultiplierBomb = 10
    }

    public enum GameState
    {
        Initializing,
        Idle,
        Spinning,
        DroppingInitial,
        HighlightingWins,
        ExplodingSymbols,
        TumblingDown,
        SpawningNewSymbols,
        EvaluatingMultipliers,
        FreeSpinsTransition,
        RoundFinished
    }

    public enum WinTier
    {
        None = 0,
        Tier8To9 = 1,
        Tier10To11 = 2,
        Tier12Plus = 3
    }
}
