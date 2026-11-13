namespace SweetBonanza.Model
{
    internal enum SymbolType
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

    internal enum GameState
    {
        Initializing,
        Idle,
        Spinning,
        DroppingInitial,
        TumblingDown,
        EvaluatingMultipliers
    }
}
