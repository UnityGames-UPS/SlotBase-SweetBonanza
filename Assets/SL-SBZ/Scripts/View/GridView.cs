using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SweetBonanza.Model;

namespace SweetBonanza.View
{
    public class GridView : MonoBehaviour
    {
        internal event Action<int> OnScatterAnticipationTriggered;

        [Header("Hierarchy Bindings")]
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private RectMask2D gridMask;
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private Transform slotFrameTransform;

        [Header("Grid Geometry")]
        [SerializeField] private Vector2 cellSize = new Vector2(110f, 110f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(8f, 8f);

        [Header("Animation Timings")]
        [SerializeField] private float initialDropDuration = 0.46f;
        [SerializeField] private float columnStaggerDelay = 0.11f;
        [SerializeField] private float winPulseDuration = 0.90f;
        [SerializeField] private float explodeDuration = 0.26f;
        [SerializeField] private float tumbleFallDuration = 0.24f;

        [Header("Symbol Sprites Configuration")]
        [SerializeField] private Sprite bananaSprite;
        [SerializeField] private Sprite grapesSprite;
        [SerializeField] private Sprite watermelonSprite;
        [SerializeField] private Sprite plumSprite;
        [SerializeField] private Sprite appleSprite;
        [SerializeField] private Sprite blueOvalSprite;
        [SerializeField] private Sprite greenPentagonSprite;
        [SerializeField] private Sprite purpleSquareSprite;
        [SerializeField] private Sprite redHeartSprite;
        [SerializeField] private Sprite lollipopSprite;
        [SerializeField] private Sprite multiplierBombSprite;
        [SerializeField] private Sprite multiplierBombSprite2;
        [SerializeField] private Sprite multiplierBombSprite3;

        [Header("Bomb Bust Effect (30 Frames)")]
        [SerializeField] private Sprite[] bombEffectFrames;
        [SerializeField] private Sprite[] colorfulBombBlastFrames;

        [Header("Symbol Win Animations (All Gems & Fruits)")]
        [SerializeField] private Sprite[] bananaFrames;
        [SerializeField] private Sprite[] grapesFrames;
        [SerializeField] private Sprite[] watermelonFrames;
        [SerializeField] private Sprite[] plumFrames;
        [SerializeField] private Sprite[] appleFrames;
        [SerializeField] private Sprite[] blueCandyFrames;
        [SerializeField] private Sprite[] greenCandyFrames;
        [SerializeField] private Sprite[] purpleCandyFrames;
        [SerializeField] private Sprite[] redHeartFrames;
        [SerializeField] private Sprite[] spiralCandyFrames;
        [SerializeField] private Sprite[] spiralCandyRotationFrames;
        [SerializeField] private Sprite[] glassEffectFrames;

        private static readonly Vector2 PoolParkingPosition = new Vector2(0f, 3000f);
        private static readonly WaitForSeconds WaitSlamStopEnd = new WaitForSeconds(0.04f);
        private static readonly WaitForSeconds WaitSlamExplode = new WaitForSeconds(0.05f);
        private static readonly WaitForSeconds WaitSlamMicro = new WaitForSeconds(0.08f);
        private static readonly WaitForSeconds WaitInitialDropEnd = new WaitForSeconds(0.10f);
        private static readonly WaitForSeconds WaitTumbleEnd = new WaitForSeconds(0.22f);
        private static readonly WaitForSeconds WaitBombStep = new WaitForSeconds(0.20f);
        private static readonly WaitForSeconds WaitBombEnd = new WaitForSeconds(0.25f);
        private static readonly WaitForSeconds WaitScatterHold = new WaitForSeconds(1.20f);
        private static readonly WaitForSeconds WaitScatterSettle = new WaitForSeconds(0.25f);

        private bool isQuickSpinMode = false;
        private bool isSlamStopRequested = false;
        private bool isPoolPrewarmed = false;
        private CellView[,] activeCells = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private CellView[,] scratchGrid = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private readonly List<CellView> cellPool = new List<CellView>(64);
        private List<CellView>[] oldCellsByCol;
        private readonly int[] scattersPerCol = new int[GameConfig.COLUMNS];
        private readonly float[] columnDelays = new float[GameConfig.COLUMNS];
        private readonly float[] columnDurations = new float[GameConfig.COLUMNS];
        private readonly int[] explodedCountPerCol = new int[GameConfig.COLUMNS];
        private readonly List<CellView> scatterCellsScratch = new List<CellView>(12);
        private Dictionary<SymbolType, Sprite> spriteLookup;
        private Dictionary<SymbolType, Sprite[]> animationLookup;

        private WaitForSeconds waitWinPulse;
        private WaitForSeconds waitExplode;

        private int completedDropsCount;
        private int completedFallsCount;
        private int completedSpawnsCount;
        private Action cachedIncrementDrops;
        private Action cachedIncrementFalls;
        private Action cachedIncrementSpawns;
        private Action<CellView> cachedRecycleCell;
        private Action cachedReelStop;
        private Action cachedLastReelStop;

        internal void SetQuickSpinMode(bool isQuick)
        {
            isQuickSpinMode = isQuick;
            if (isQuick)
            {
                initialDropDuration = 0.22f;
                columnStaggerDelay = 0.04f;
                winPulseDuration = 0.40f;
                explodeDuration = 0.15f;
                tumbleFallDuration = 0.16f;
            }
            else
            {
                initialDropDuration = 0.46f;
                columnStaggerDelay = 0.11f;
                winPulseDuration = 0.90f;
                explodeDuration = 0.26f;
                tumbleFallDuration = 0.24f;
            }
            RefreshTimingCaches();
        }

        private void RefreshTimingCaches()
        {
            waitWinPulse = new WaitForSeconds(winPulseDuration);
            waitExplode = new WaitForSeconds(explodeDuration);
        }

        internal void RequestSlamStop()
        {
            if (isSlamStopRequested) return;
            isSlamStopRequested = true;
            if (gridMask != null) gridMask.enabled = false;

            if (activeCells != null)
            {
                for (int c = 0; c < GameConfig.COLUMNS; c++)
                {
                    for (int r = 0; r < GameConfig.ROWS; r++)
                    {
                        var cell = activeCells[c, r];
                        if (cell != null)
                        {
                            cell.CompleteAnimation();
                            cell.RectTransform.anchoredPosition = GetCellPosition(c, r);
                        }
                    }
                }
            }
        }

        internal void ResetSlamStop()
        {
            isSlamStopRequested = false;
        }

        private void Awake()
        {
            InitializeSpriteLookup();
            InitializeAnimationLookup();
            RefreshTimingCaches();

            oldCellsByCol = new List<CellView>[GameConfig.COLUMNS];
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                oldCellsByCol[c] = new List<CellView>(GameConfig.ROWS);
            }

            cachedIncrementDrops = () => completedDropsCount++;
            cachedIncrementFalls = () => completedFallsCount++;
            cachedIncrementSpawns = () => completedSpawnsCount++;
            cachedRecycleCell = RecycleCell;
            cachedReelStop = () =>
            {
                if (!isSlamStopRequested) AudioManager.Instance?.PlayReelStop();
            };
            cachedLastReelStop = () =>
            {
                if (!isSlamStopRequested)
                {
                    AudioManager.Instance?.PlayReelStop();
                    AudioManager.Instance?.StopReelSpinLoop();
                }
            };

            if (slotFrameTransform == null)
            {
                var sf = GameObject.Find("MainCanvas/BG/MainGamePlay/slot frame");
                if (sf != null) slotFrameTransform = sf.transform;
            }
            PrewarmCellPool();
        }

        private void PrewarmCellPool()
        {
            if (isPoolPrewarmed) return;
            isPoolPrewarmed = true;

            Transform parent = gridContainer != null ? gridContainer : transform;
            var existing = parent.GetComponentsInChildren<CellView>(true);
            foreach (var cell in existing)
            {
                if (cell != null && !cellPool.Contains(cell))
                {
                    if (!cell.gameObject.activeSelf) cell.gameObject.SetActive(true);
                    cell.ResetCell();
                    if (cell.RectTransform != null) cell.RectTransform.anchoredPosition = PoolParkingPosition;
                    cellPool.Add(cell);
                }
            }

            while (cellPool.Count < 60 && cellPrefab != null)
            {
                var cell = Instantiate(cellPrefab, parent);
                if (!cell.gameObject.activeSelf) cell.gameObject.SetActive(true);
                cell.ResetCell();
                if (cell.RectTransform != null) cell.RectTransform.anchoredPosition = PoolParkingPosition;
                cellPool.Add(cell);
            }
        }

        private void Start()
        {
            if (gridMask != null) gridMask.enabled = false;
            InitializeInitialGrid();
        }

        internal void InitializeInitialGrid(int[,] matrix = null)
        {
            if (matrix == null && activeCells[0, 0] != null) return;

            ClearGrid();
            PrewarmCellPool();

            if (matrix == null)
            {
                matrix = GenerateDefaultStartingMatrix();
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    var cell = GetOrCreateCell();
                    activeCells[c, r] = cell;

                    Vector2 targetPos = GetCellPosition(c, r);
                    cell.RectTransform.anchoredPosition = targetPos;
                    cell.RectTransform.localScale = Vector3.one;
                    cell.Setup(symbol, GetSymbolSprite(symbol));
                    cell.gameObject.SetActive(true);
                }
            }
        }

        private int[,] GenerateDefaultStartingMatrix()
        {
            return new int[GameConfig.COLUMNS, GameConfig.ROWS]
            {
                // Column 0 (Left): Row 0 (Bottom) to Row 4 (Top)
                { (int)SymbolType.Apple, (int)SymbolType.GreenPentagon, (int)SymbolType.GreenPentagon, (int)SymbolType.Banana, (int)SymbolType.Banana },
                // Column 1
                { (int)SymbolType.RedHeart, (int)SymbolType.Lollipop, (int)SymbolType.PurpleSquare, (int)SymbolType.PurpleSquare, (int)SymbolType.Watermelon },
                // Column 2
                { (int)SymbolType.Grapes, (int)SymbolType.Watermelon, (int)SymbolType.Watermelon, (int)SymbolType.Apple, (int)SymbolType.Apple },
                // Column 3
                { (int)SymbolType.BlueOval, (int)SymbolType.GreenPentagon, (int)SymbolType.GreenPentagon, (int)SymbolType.Banana, (int)SymbolType.Banana },
                // Column 4
                { (int)SymbolType.Grapes, (int)SymbolType.Plum, (int)SymbolType.Plum, (int)SymbolType.BlueOval, (int)SymbolType.BlueOval },
                // Column 5 (Right)
                { (int)SymbolType.Watermelon, (int)SymbolType.Watermelon, (int)SymbolType.RedHeart, (int)SymbolType.RedHeart, (int)SymbolType.PurpleSquare }
            };
        }

        private void InitializeSpriteLookup()
        {
            spriteLookup = new Dictionary<SymbolType, Sprite>
            {
                { SymbolType.Banana, bananaSprite },
                { SymbolType.Grapes, grapesSprite },
                { SymbolType.Watermelon, watermelonSprite },
                { SymbolType.Plum, plumSprite },
                { SymbolType.Apple, appleSprite },
                { SymbolType.BlueOval, blueOvalSprite },
                { SymbolType.GreenPentagon, greenPentagonSprite },
                { SymbolType.PurpleSquare, purpleSquareSprite },
                { SymbolType.RedHeart, redHeartSprite },
                { SymbolType.Lollipop, lollipopSprite },
                { SymbolType.MultiplierBomb, multiplierBombSprite }
            };
        }

        private void InitializeAnimationLookup()
        {
            Sprite[] lollipopJump = (spiralCandyRotationFrames != null && spiralCandyRotationFrames.Length > 0)
                ? spiralCandyRotationFrames
                : spiralCandyFrames;

            animationLookup = new Dictionary<SymbolType, Sprite[]>
            {
                { SymbolType.Banana, bananaFrames },
                { SymbolType.Grapes, grapesFrames },
                { SymbolType.Watermelon, watermelonFrames },
                { SymbolType.Plum, plumFrames },
                { SymbolType.Apple, appleFrames },
                { SymbolType.BlueOval, blueCandyFrames },
                { SymbolType.GreenPentagon, greenCandyFrames },
                { SymbolType.PurpleSquare, purpleCandyFrames },
                { SymbolType.RedHeart, redHeartFrames },
                { SymbolType.Lollipop, lollipopJump }
            };
        }

        internal Sprite GetSymbolSprite(SymbolType symbol)
        {
            if (spriteLookup == null) InitializeSpriteLookup();
            return spriteLookup.TryGetValue(symbol, out var sprite) ? sprite : null;
        }

        internal Sprite[] GetSymbolAnimationFrames(SymbolType symbol)
        {
            if (animationLookup == null) InitializeAnimationLookup();
            return animationLookup.TryGetValue(symbol, out var frames) ? frames : null;
        }

        internal Sprite GetMultiplierBombSprite(int multiplier = 0)
        {
            return multiplierBombSprite != null ? multiplierBombSprite : GetSymbolSprite(SymbolType.MultiplierBomb);
        }

        internal Vector2 GetCellPosition(int column, int row)
        {
            float totalWidth = (GameConfig.COLUMNS - 1) * (cellSize.x + cellSpacing.x);
            float totalHeight = (GameConfig.ROWS - 1) * (cellSize.y + cellSpacing.y);
            float startX = -totalWidth * 0.5f;
            float startY = -totalHeight * 0.5f;
            return new Vector2(startX + column * (cellSize.x + cellSpacing.x), startY + row * (cellSize.y + cellSpacing.y));
        }

        internal Vector2 GetSpawnPosition(int column, int spawnIndex)
        {
            Vector2 baseTop = GetCellPosition(column, GameConfig.ROWS - 1);
            float rowStep = cellSize.y + cellSpacing.y;
            // Elevate sufficiently high above the top mask so symbols enter with high downward momentum
            float offset = rowStep * (spawnIndex + 1) + 220f;
            return new Vector2(baseTop.x, baseTop.y + offset);
        }

        internal IEnumerator PlayInitialDropSequence(int[,] matrix, List<BombMultiplierInfo> bombs = null)
        {
            if (gridMask != null) gridMask.enabled = true;
            // 1. Gather active cells by column so exit and entrance can cascade synchronously
            int totalOldCells = 0;
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                oldCellsByCol[c].Clear();
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null)
                    {
                        oldCellsByCol[c].Add(activeCells[c, r]);
                        activeCells[c, r] = null;
                        totalOldCells++;
                    }
                }
            }

            // 2. Cascade old symbols out column-by-column (left to right cascade)
            float clearDuration = isQuickSpinMode ? 0.18f : 0.28f;
            float clearStagger = isQuickSpinMode ? 0.025f : 0.055f;

            if (totalOldCells > 0)
            {
                for (int c = 0; c < GameConfig.COLUMNS; c++)
                {
                    float oldColDelay = c * clearStagger;
                    var colList = oldCellsByCol[c];
                    for (int i = 0; i < colList.Count; i++)
                    {
                        var oldCell = colList[i];
                        if (isSlamStopRequested)
                        {
                            RecycleCell(oldCell);
                        }
                        else
                        {
                            oldCell.PlayClearDropAnimation(oldColDelay, clearDuration, cachedRecycleCell);
                        }
                    }
                }
            }

            // 3. Count scatters per column to calculate anticipation delays
            Array.Clear(scattersPerCol, 0, scattersPerCol.Length);
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (matrix[c, r] == (int)SymbolType.Lollipop) scattersPerCol[c]++;
                }
            }

            // 4. Calculate timing for incoming new wave:
            // Screen clears cleanly: Column 0 enters top-left as column 5 plunges out at bottom-right
            float entranceStartDelay = (totalOldCells > 0 && !isSlamStopRequested)
                ? ((GameConfig.COLUMNS - 1) * clearStagger + clearDuration * 0.40f)
                : 0f;

            int cumulativeScatters = 0;
            float currentCumulativeDelay = entranceStartDelay;
            bool anticipationTriggered = false;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                columnDelays[c] = currentCumulativeDelay;
                if (anticipationTriggered && !isQuickSpinMode)
                {
                    columnDurations[c] = initialDropDuration * 1.35f;
                    currentCumulativeDelay += columnStaggerDelay + 0.55f;
                }
                else
                {
                    columnDurations[c] = initialDropDuration;
                    currentCumulativeDelay += columnStaggerDelay;
                }

                cumulativeScatters += scattersPerCol[c];
                if (cumulativeScatters >= 3 && !anticipationTriggered && c < GameConfig.COLUMNS - 1 && !isQuickSpinMode)
                {
                    anticipationTriggered = true;
                    currentCumulativeDelay += 0.45f;
                    OnScatterAnticipationTriggered?.Invoke(c + 1);
                    if (slotFrameTransform != null)
                    {
                        slotFrameTransform.DOKill();
                        slotFrameTransform.DOPunchScale(Vector3.one * 0.02f, 0.45f, 4, 0.5f);
                    }
                }
            }

            // 5. New symbols enter and land with the bubbly, bouncy stacking wave
            completedDropsCount = 0;
            int totalDrops = GameConfig.TOTAL_CELLS;
            float rowStagger = isQuickSpinMode ? 0.02f : 0.065f;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                float dropInDelay = columnDelays[c];
                float duration = columnDurations[c];
                Action colImpactAction = (c == GameConfig.COLUMNS - 1) ? cachedLastReelStop : cachedReelStop;

                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    var cell = GetOrCreateCell();
                    activeCells[c, r] = cell;

                    Vector2 targetPos = GetCellPosition(c, r);
                    Vector2 spawnPos = GetSpawnPosition(c, r);

                    cell.RectTransform.anchoredPosition = spawnPos;

                    int mult = 0;
                    if (symbol == SymbolType.MultiplierBomb && bombs != null)
                    {
                        for (int b = 0; b < bombs.Count; b++)
                        {
                            if (bombs[b].Position.Column == c)
                            {
                                mult = bombs[b].MultiplierValue;
                                break;
                            }
                        }
                    }

                    Sprite sprite = (symbol == SymbolType.MultiplierBomb) ? GetMultiplierBombSprite(mult) : GetSymbolSprite(symbol);
                    cell.Setup(symbol, sprite, mult);
                    int targetSibling = c * GameConfig.ROWS + r;
                    if (cell.transform.GetSiblingIndex() != targetSibling)
                    {
                        cell.transform.SetSiblingIndex(targetSibling);
                    }

                    // Row-stagger (r * rowStagger): creates the bubbly stacking wave where Row 0 lands first, Row 1 lands on top of it, Row 2 next, etc.
                    float symbolDropDelay = dropInDelay + (r * rowStagger);
                    cell.PlayDropInAnimation(targetPos, symbolDropDelay, duration, cachedIncrementDrops, null, r == 0 ? colImpactAction : null);
                }
            }

            while (completedDropsCount < totalDrops)
            {
                if (isSlamStopRequested)
                {
                    for (int c = 0; c < GameConfig.COLUMNS; c++)
                    {
                        for (int r = 0; r < GameConfig.ROWS; r++)
                        {
                            var cell = activeCells[c, r];
                            if (cell != null)
                            {
                                cell.CompleteAnimation();
                                cell.RectTransform.anchoredPosition = GetCellPosition(c, r);
                            }
                        }
                    }
                    break;
                }
                yield return null;
            }
            BringScattersAndBombsToFront();
            if (gridMask != null) gridMask.enabled = false;
            bool wasSlam = isSlamStopRequested;
            if (wasSlam) AudioManager.Instance?.PlayReelStop();
            yield return wasSlam ? WaitSlamStopEnd : WaitInitialDropEnd;
        }

        internal IEnumerator PlayTumbleStepSequence(TumbleStep step, List<BombMultiplierInfo> bombs = null)
        {
            // 1. Play 3D Win Animations and show Floating Cluster Win Amounts
            AudioManager.Instance?.PlayWinLinePhase1Start();

            if (step.Wins != null && step.Wins.Count > 0)
            {
                for (int w = 0; w < step.Wins.Count; w++)
                {
                    var win = step.Wins[w];
                    Sprite[] frames = GetSymbolAnimationFrames(win.Symbol);
                    if (win.Positions != null && win.Positions.Count > 0)
                    {
                        // Choose center position of cluster to display floating win label
                        GridPosition centerPos = win.Positions[win.Positions.Count / 2];

                        for (int p = 0; p < win.Positions.Count; p++)
                        {
                            var pos = win.Positions[p];
                            var cell = activeCells[pos.Column, pos.Row];
                            if (cell != null)
                            {
                                cell.transform.SetAsLastSibling();
                                double amountToShow = pos.Equals(centerPos) ? win.WinAmount : 0;
                                cell.PlayWinAnimation(frames, winPulseDuration, amountToShow, null);
                            }
                        }
                    }
                }
            }
            else if (step.ExplodedPositions != null)
            {
                for (int p = 0; p < step.ExplodedPositions.Count; p++)
                {
                    var pos = step.ExplodedPositions[p];
                    var cell = activeCells[pos.Column, pos.Row];
                    if (cell != null)
                    {
                        cell.transform.SetAsLastSibling();
                        Sprite[] frames = GetSymbolAnimationFrames(cell.CurrentSymbol);
                        cell.PlayWinAnimation(frames, winPulseDuration, 0, null);
                    }
                }
            }

            yield return isSlamStopRequested ? WaitSlamMicro : waitWinPulse;

            // 2. Explode and pop with glass / candy burst effect while playing 3D symbol animation
            if (step.ExplodedPositions != null && step.ExplodedPositions.Count > 0)
            {
                AudioManager.Instance?.PlaySymbolPop();
                for (int p = 0; p < step.ExplodedPositions.Count; p++)
                {
                    var pos = step.ExplodedPositions[p];
                    var cell = activeCells[pos.Column, pos.Row];
                    if (cell != null)
                    {
                        Sprite[] fruitFrames = GetSymbolAnimationFrames(cell.CurrentSymbol);
                        cell.PlayExplodeAnimation(glassEffectFrames, cachedRecycleCell, fruitFrames);
                        activeCells[pos.Column, pos.Row] = null;
                    }
                }
            }
            yield return isSlamStopRequested ? WaitSlamExplode : waitExplode;

            // Calculate exploded count per column to build the straight lane stack
            Array.Clear(explodedCountPerCol, 0, explodedCountPerCol.Length);
            if (step.ExplodedPositions != null)
            {
                for (int p = 0; p < step.ExplodedPositions.Count; p++)
                {
                    explodedCountPerCol[step.ExplodedPositions[p].Column]++;
                }
            }

            // 3. Gravity drop for surviving symbols (straight lane drop: delay = 0f, uniform duration)
            Array.Clear(scratchGrid, 0, scratchGrid.Length);
            completedFallsCount = 0;
            int totalFalling = step.FallingSymbols.Count;
            float tumbleDuration = isQuickSpinMode ? 0.16f : tumbleFallDuration;

            for (int f = 0; f < totalFalling; f++)
            {
                var fall = step.FallingSymbols[f];
                var cell = activeCells[fall.Column, fall.FromRow];
                if (cell != null)
                {
                    scratchGrid[fall.Column, fall.ToRow] = cell;
                    activeCells[fall.Column, fall.FromRow] = null;
                    Vector2 targetPos = GetCellPosition(fall.Column, fall.ToRow);
                    cell.PlayFallAnimation(targetPos, tumbleDuration, cachedIncrementFalls, 0f);
                }
                else completedFallsCount++;
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null && scratchGrid[c, r] == null) scratchGrid[c, r] = activeCells[c, r];
                }
            }

            // 4. Spawn new symbols directly in the lane stack above each column (straight unified drop)
            if (step.NewSpawns.Count > 0 && gridMask != null)
            {
                gridMask.enabled = true;
            }

            completedSpawnsCount = 0;
            int totalSpawns = step.NewSpawns.Count;

            for (int s = 0; s < totalSpawns; s++)
            {
                var spawn = step.NewSpawns[s];
                var cell = GetOrCreateCell();
                scratchGrid[spawn.Column, spawn.TargetRow] = cell;

                Vector2 targetPos = GetCellPosition(spawn.Column, spawn.TargetRow);
                int emptyCount = explodedCountPerCol[spawn.Column];
                Vector2 spawnPos = GetCellPosition(spawn.Column, spawn.TargetRow + emptyCount);

                cell.RectTransform.anchoredPosition = spawnPos;

                int mult = 0;
                if (spawn.Symbol == SymbolType.MultiplierBomb && bombs != null)
                {
                    for (int b = 0; b < bombs.Count; b++)
                    {
                        if (bombs[b].Position.Column == spawn.Column)
                        {
                            mult = bombs[b].MultiplierValue;
                            break;
                        }
                    }
                }

                Sprite sprite = (spawn.Symbol == SymbolType.MultiplierBomb) ? GetMultiplierBombSprite(mult) : GetSymbolSprite(spawn.Symbol);
                cell.Setup(spawn.Symbol, sprite, mult);
                cell.PlayFallAnimation(targetPos, tumbleDuration, cachedIncrementSpawns, 0f);
            }

            var prevActive = activeCells;
            activeCells = scratchGrid;
            scratchGrid = prevActive;

            // Normalize rendering hierarchy so z-order is clean and predictable
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    var cell = activeCells[c, r];
                    if (cell != null)
                    {
                        int targetSibling = c * GameConfig.ROWS + r;
                        if (cell.transform.GetSiblingIndex() != targetSibling)
                        {
                            cell.transform.SetSiblingIndex(targetSibling);
                        }
                    }
                }
            }

            while (completedFallsCount < totalFalling || completedSpawnsCount < totalSpawns)
            {
                if (isSlamStopRequested)
                {
                    for (int c = 0; c < GameConfig.COLUMNS; c++)
                    {
                        for (int r = 0; r < GameConfig.ROWS; r++)
                        {
                            var cell = activeCells[c, r];
                            if (cell != null)
                            {
                                cell.CompleteAnimation();
                                cell.RectTransform.anchoredPosition = GetCellPosition(c, r);
                            }
                        }
                    }
                    break;
                }
                yield return null;
            }
            BringScattersAndBombsToFront();
            if (gridMask != null) gridMask.enabled = false;
            bool wasSlam = isSlamStopRequested;
            AudioManager.Instance?.PlayReelStop();
            yield return wasSlam ? WaitSlamStopEnd : WaitTumbleEnd;
        }

        private void BringScattersAndBombsToFront()
        {
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    var cell = activeCells[c, r];
                    if (cell != null && (cell.CurrentSymbol == SymbolType.Lollipop || cell.CurrentSymbol == SymbolType.MultiplierBomb))
                    {
                        cell.transform.SetAsLastSibling();
                    }
                }
            }
        }

        internal IEnumerator PlayMultiplierBombRevealSequence(List<BombMultiplierInfo> bombs, Action<int, Vector3, int> onBombDetonated = null)
        {
            if (bombs == null || bombs.Count == 0) yield break;

            int runningMultiplier = 0;
            for (int i = 0; i < bombs.Count; i++)
            {
                var bomb = bombs[i];
                runningMultiplier += bomb.MultiplierValue;
                var cell = activeCells[bomb.Position.Column, bomb.Position.Row];
                if (cell != null)
                {
                    cell.transform.SetAsLastSibling();
                    Sprite bombSprite = GetMultiplierBombSprite(bomb.MultiplierValue);
                    cell.Setup(SymbolType.MultiplierBomb, bombSprite, bomb.MultiplierValue);

                    Vector3 bombPos = cell.transform.position;
                    int bombVal = bomb.MultiplierValue;
                    int curRunning = runningMultiplier;

                    bool bombCompleted = false;
                    cell.PlayMultiplierBombRevealAnimation(bombVal, () => bombCompleted = true, bombEffectFrames, colorfulBombBlastFrames, () =>
                    {
                        onBombDetonated?.Invoke(bombVal, bombPos, curRunning);
                    });

                    while (!bombCompleted) yield return null;
                    yield return WaitBombStep;
                }
            }

            yield return WaitBombEnd;
        }

        internal IEnumerator PlayScatterCelebrationSequence()
        {
            scatterCellsScratch.Clear();
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null && activeCells[c, r].CurrentSymbol == SymbolType.Lollipop)
                    {
                        scatterCellsScratch.Add(activeCells[c, r]);
                    }
                }
            }

            if (scatterCellsScratch.Count >= 3)
            {
                // Play special 32-frame Spiral candy transition when Scatters trigger Free Spins
                Sprite[] triggerFrames = (spiralCandyRotationFrames != null && spiralCandyRotationFrames.Length > 0)
                    ? spiralCandyRotationFrames
                    : spiralCandyFrames;

                for (int i = 0; i < scatterCellsScratch.Count; i++)
                {
                    var cell = scatterCellsScratch[i];
                    cell.transform.SetAsLastSibling();
                    cell.PlayScatterTriggerAnimation(triggerFrames, 1.4f);
                    cell.transform.DOScale(1.75f, 0.25f).SetEase(Ease.OutBack);
                }
                yield return WaitScatterHold;
                for (int i = 0; i < scatterCellsScratch.Count; i++)
                {
                    scatterCellsScratch[i].transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.InOutQuad);
                }
                yield return WaitScatterSettle;
            }
        }

        internal void ClearGrid()
        {
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null)
                    {
                        RecycleCell(activeCells[c, r]);
                        activeCells[c, r] = null;
                    }
                }
            }
        }

        private CellView GetOrCreateCell()
        {
            if (cellPool.Count > 0)
            {
                var cell = cellPool[cellPool.Count - 1];
                cellPool.RemoveAt(cellPool.Count - 1);
                if (!cell.gameObject.activeSelf) cell.gameObject.SetActive(true);
                return cell;
            }

            Transform parent = gridContainer != null ? gridContainer : transform;
            var newCell = Instantiate(cellPrefab, parent);
            if (!newCell.gameObject.activeSelf) newCell.gameObject.SetActive(true);
            return newCell;
        }

        private void RecycleCell(CellView cell)
        {
            if (cell == null) return;
            cell.ResetCell();
            if (cell.RectTransform != null) cell.RectTransform.anchoredPosition = PoolParkingPosition;
            if (!cellPool.Contains(cell))
            {
                cellPool.Add(cell);
            }
        }

        private void OnDisable()
        {
            if (slotFrameTransform != null) slotFrameTransform.DOKill();
        }

        private void OnDestroy()
        {
            if (slotFrameTransform != null) slotFrameTransform.DOKill();
            ClearGrid();
            for (int i = 0; i < cellPool.Count; i++)
            {
                if (cellPool[i] != null) Destroy(cellPool[i].gameObject);
            }
            cellPool.Clear();
        }
    }
}
