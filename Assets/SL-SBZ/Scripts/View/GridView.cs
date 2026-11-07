using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using SweetBonanza.Model;

namespace SweetBonanza.View
{
    public class GridView : MonoBehaviour
    {
        internal event Action<int> OnScatterAnticipationTriggered;

        [Header("Hierarchy Bindings")]
        [SerializeField] private RectTransform gridContainer;
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
        public Sprite bananaSprite;
        public Sprite grapesSprite;
        public Sprite watermelonSprite;
        public Sprite plumSprite;
        public Sprite appleSprite;
        public Sprite blueOvalSprite;
        public Sprite greenPentagonSprite;
        public Sprite purpleSquareSprite;
        public Sprite redHeartSprite;
        public Sprite lollipopSprite;
        public Sprite multiplierBombSprite;
        public Sprite multiplierBombSprite2;
        public Sprite multiplierBombSprite3;

        [Header("Bomb Bust Effect (30 Frames)")]
        public Sprite[] bombEffectFrames;
        public Sprite[] colorfulBombBlastFrames;

        [Header("Symbol Win Animations (All Gems & Fruits)")]
        public Sprite[] bananaFrames;
        public Sprite[] grapesFrames;
        public Sprite[] watermelonFrames;
        public Sprite[] plumFrames;
        public Sprite[] appleFrames;
        public Sprite[] blueCandyFrames;
        public Sprite[] greenCandyFrames;
        public Sprite[] purpleCandyFrames;
        public Sprite[] redHeartFrames;
        public Sprite[] spiralCandyFrames;
        public Sprite[] spiralCandyRotationFrames;
        public Sprite[] spiralCandyTransitionFrames;
        public Sprite[] glassEffectFrames;

        private bool isQuickSpinMode = false;
        private bool isSlamStopRequested = false;
        private CellView[,] activeCells = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private readonly List<CellView> cellPool = new List<CellView>();
        private Dictionary<SymbolType, Sprite> spriteLookup;
        private Dictionary<SymbolType, Sprite[]> animationLookup;

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
        }

        internal void RequestSlamStop()
        {
            if (isSlamStopRequested) return;
            isSlamStopRequested = true;

            if (activeCells != null)
            {
                for (int c = 0; c < GameConfig.COLUMNS; c++)
                {
                    for (int r = 0; r < GameConfig.ROWS; r++)
                    {
                        if (activeCells[c, r] != null)
                        {
                            activeCells[c, r].CompleteAnimation();
                        }
                    }
                }
            }

            if (cellPool != null)
            {
                foreach (var cell in cellPool)
                {
                    if (cell != null && cell.gameObject.activeSelf)
                    {
                        cell.CompleteAnimation();
                    }
                }
            }
        }

        private void Awake()
        {
            InitializeSpriteLookup();
            InitializeAnimationLookup();
            if (slotFrameTransform == null)
            {
                var sf = GameObject.Find("MainCanvas/BG/MainGamePlay/slot frame");
                if (sf != null) slotFrameTransform = sf.transform;
            }
        }

        private void Start()
        {
            InitializeInitialGrid();
        }

        internal void InitializeInitialGrid(int[,] matrix = null)
        {
            if (matrix == null && activeCells[0, 0] != null) return;

            ClearGrid();

            // Adopt pre-existing scene cells into pool to reuse them cleanly
            Transform parent = gridContainer != null ? gridContainer : transform;
            var existing = parent.GetComponentsInChildren<CellView>(true);
            foreach (var cell in existing)
            {
                if (cell != null && !cellPool.Contains(cell))
                {
                    cell.ResetCell();
                    cell.gameObject.SetActive(false);
                    cellPool.Add(cell);
                }
            }

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

        internal Sprite GetMultiplierBombSprite(int multiplier)
        {
            if (multiplier >= 50 && multiplierBombSprite3 != null) return multiplierBombSprite3;
            if (multiplier >= 10 && multiplierBombSprite2 != null) return multiplierBombSprite2;
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

        internal IEnumerator PlayInitialDropSequence(int[,] matrix)
        {
            isSlamStopRequested = false;
            // 1. Gather active cells by column so exit and entrance can cascade synchronously
            var oldCellsByCol = new List<CellView>[GameConfig.COLUMNS];
            int totalOldCells = 0;
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                oldCellsByCol[c] = new List<CellView>();
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
                    foreach (var oldCell in oldCellsByCol[c])
                    {
                        oldCell.PlayClearDropAnimation(oldColDelay, clearDuration, () =>
                        {
                            RecycleCell(oldCell);
                        });
                    }
                }
            }

            // 3. Count scatters per column to calculate anticipation delays
            int[] scattersPerCol = new int[GameConfig.COLUMNS];
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (matrix[c, r] == (int)SymbolType.Lollipop) scattersPerCol[c]++;
                }
            }

            // 4. Calculate timing for incoming new wave:
            // Screen clears cleanly: Column 0 enters top-left as column 5 plunges out at bottom-right
            float entranceStartDelay = totalOldCells > 0
                ? ((GameConfig.COLUMNS - 1) * clearStagger + clearDuration * 0.40f)
                : 0f;

            int cumulativeScatters = 0;
            float currentCumulativeDelay = entranceStartDelay;
            bool anticipationTriggered = false;
            float[] columnDelays = new float[GameConfig.COLUMNS];
            float[] columnDurations = new float[GameConfig.COLUMNS];

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
            int completedDrops = 0;
            int totalDrops = GameConfig.TOTAL_CELLS;
            float rowStagger = isQuickSpinMode ? 0.02f : 0.065f;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                float dropInDelay = columnDelays[c];
                float duration = columnDurations[c];
                int colCompleted = 0;

                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    var cell = GetOrCreateCell();
                    activeCells[c, r] = cell;

                    Vector2 targetPos = GetCellPosition(c, r);
                    Vector2 spawnPos = GetSpawnPosition(c, r);

                    cell.RectTransform.anchoredPosition = spawnPos;
                    cell.Setup(symbol, GetSymbolSprite(symbol));
                    cell.transform.SetSiblingIndex(c * GameConfig.ROWS + r);

                    // Row-stagger (r * rowStagger): creates the bubbly stacking wave where Row 0 lands first, Row 1 lands on top of it, Row 2 next, etc.
                    float symbolDropDelay = dropInDelay + (r * rowStagger);

                    cell.PlayDropInAnimation(targetPos, symbolDropDelay, duration, () =>
                    {
                        completedDrops++;
                        colCompleted++;
                        if (colCompleted == GameConfig.ROWS && !isSlamStopRequested)
                        {
                            AudioManager.Instance?.PlayReelStop();
                        }
                    });
                }
            }

            while (completedDrops < totalDrops)
            {
                if (isSlamStopRequested)
                {
                    for (int c = 0; c < GameConfig.COLUMNS; c++)
                    {
                        for (int r = 0; r < GameConfig.ROWS; r++)
                        {
                            if (activeCells[c, r] != null)
                            {
                                activeCells[c, r].CompleteAnimation();
                            }
                        }
                    }
                }
                yield return null;
            }
            BringScattersAndBombsToFront();
            bool wasSlam = isSlamStopRequested;
            if (wasSlam) AudioManager.Instance?.PlayReelStop();
            isSlamStopRequested = false;
            yield return new WaitForSeconds(wasSlam ? 0.04f : 0.10f);
        }

        internal IEnumerator PlayTumbleStepSequence(TumbleStep step)
        {
            // 1. Play 3D Win Animations and show Floating Cluster Win Amounts
            AudioManager.Instance?.PlayWinLinePhase1Start();

            if (step.Wins != null && step.Wins.Count > 0)
            {
                foreach (var win in step.Wins)
                {
                    Sprite[] frames = GetSymbolAnimationFrames(win.Symbol);
                    if (win.Positions != null && win.Positions.Count > 0)
                    {
                        // Choose center position of cluster to display floating win label
                        GridPosition centerPos = win.Positions[win.Positions.Count / 2];

                        foreach (var pos in win.Positions)
                        {
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
            else
            {
                foreach (var pos in step.ExplodedPositions)
                {
                    var cell = activeCells[pos.Column, pos.Row];
                    if (cell != null)
                    {
                        cell.transform.SetAsLastSibling();
                        Sprite[] frames = GetSymbolAnimationFrames(cell.CurrentSymbol);
                        cell.PlayWinAnimation(frames, winPulseDuration, 0, null);
                    }
                }
            }

            yield return new WaitForSeconds(isSlamStopRequested ? 0.08f : winPulseDuration);

            // 2. Explode and pop with glass / candy burst effect while playing 3D symbol animation
            if (step.ExplodedPositions != null && step.ExplodedPositions.Count > 0)
            {
                AudioManager.Instance?.PlaySymbolPop();
            }
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null)
                {
                    Sprite[] fruitFrames = GetSymbolAnimationFrames(cell.CurrentSymbol);
                    cell.PlayExplodeAnimation(glassEffectFrames, () => RecycleCell(cell), fruitFrames);
                    activeCells[pos.Column, pos.Row] = null;
                }
            }
            yield return new WaitForSeconds(isSlamStopRequested ? 0.05f : explodeDuration);

            // Calculate exploded count per column to build the straight lane stack
            int[] explodedCountPerCol = new int[GameConfig.COLUMNS];
            foreach (var pos in step.ExplodedPositions)
            {
                explodedCountPerCol[pos.Column]++;
            }

            // 3. Gravity drop for surviving symbols (straight lane drop: delay = 0f, uniform duration)
            var newGrid = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
            int fallingCount = 0;
            int totalFalling = step.FallingSymbols.Count;
            float tumbleDuration = isQuickSpinMode ? 0.16f : tumbleFallDuration;

            foreach (var fall in step.FallingSymbols)
            {
                var cell = activeCells[fall.Column, fall.FromRow];
                if (cell != null)
                {
                    newGrid[fall.Column, fall.ToRow] = cell;
                    activeCells[fall.Column, fall.FromRow] = null;
                    Vector2 targetPos = GetCellPosition(fall.Column, fall.ToRow);
                    cell.PlayFallAnimation(targetPos, tumbleDuration, () => fallingCount++, 0f);
                }
                else fallingCount++;
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null && newGrid[c, r] == null) newGrid[c, r] = activeCells[c, r];
                }
            }

            // 4. Spawn new symbols directly in the lane stack above each column (straight unified drop)
            int spawnCount = 0;
            int totalSpawns = step.NewSpawns.Count;

            foreach (var spawn in step.NewSpawns)
            {
                var cell = GetOrCreateCell();
                newGrid[spawn.Column, spawn.TargetRow] = cell;

                Vector2 targetPos = GetCellPosition(spawn.Column, spawn.TargetRow);
                int emptyCount = explodedCountPerCol[spawn.Column];
                Vector2 spawnPos = GetCellPosition(spawn.Column, spawn.TargetRow + emptyCount);

                cell.RectTransform.anchoredPosition = spawnPos;
                cell.Setup(spawn.Symbol, GetSymbolSprite(spawn.Symbol));
                cell.PlayFallAnimation(targetPos, tumbleDuration, () => spawnCount++, 0f);
            }

            activeCells = newGrid;
            // Normalize rendering hierarchy so z-order is clean and predictable
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null)
                    {
                        activeCells[c, r].transform.SetSiblingIndex(c * GameConfig.ROWS + r);
                    }
                }
            }

            while (fallingCount < totalFalling || spawnCount < totalSpawns)
            {
                if (isSlamStopRequested)
                {
                    for (int c = 0; c < GameConfig.COLUMNS; c++)
                    {
                        for (int r = 0; r < GameConfig.ROWS; r++)
                        {
                            if (activeCells[c, r] != null)
                            {
                                activeCells[c, r].CompleteAnimation();
                            }
                        }
                    }
                }
                yield return null;
            }
            BringScattersAndBombsToFront();
            bool wasSlam = isSlamStopRequested;
            AudioManager.Instance?.PlayReelStop();
            isSlamStopRequested = false;
            yield return new WaitForSeconds(wasSlam ? 0.04f : 0.22f);
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

            int completed = 0;
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

                    // High multipliers (25x+) trigger the explosive Colorful Bomb blast; standard bombs fallback to colorfulBombBlastFrames if bombEffectFrames is empty
                    Sprite[] blastFrames = (bombEffectFrames != null && bombEffectFrames.Length > 0 && bomb.MultiplierValue < 25)
                        ? bombEffectFrames
                        : (colorfulBombBlastFrames != null && colorfulBombBlastFrames.Length > 0 ? colorfulBombBlastFrames : bombEffectFrames);

                    Vector3 bombPos = cell.transform.position;
                    int bombVal = bomb.MultiplierValue;
                    int curRunning = runningMultiplier;

                    cell.PlayMultiplierBombRevealAnimation(bombVal, () => completed++, blastFrames);
                    onBombDetonated?.Invoke(bombVal, bombPos, curRunning);
                }
                else completed++;

                // Stagger detonations if multiple bombs land for arcade excitement
                if (bombs.Count > 1 && i < bombs.Count - 1)
                {
                    yield return new WaitForSeconds(0.35f);
                }
            }

            while (completed < bombs.Count) yield return null;
            yield return new WaitForSeconds(0.35f);
        }

        internal IEnumerator PlayScatterCelebrationSequence()
        {
            var scatterCells = new List<CellView>();
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null && activeCells[c, r].CurrentSymbol == SymbolType.Lollipop)
                    {
                        scatterCells.Add(activeCells[c, r]);
                    }
                }
            }

            if (scatterCells.Count >= 3)
            {
                // Play special 32-frame Spiral candy transition when Scatters trigger Free Spins
                Sprite[] triggerFrames = (spiralCandyTransitionFrames != null && spiralCandyTransitionFrames.Length > 0)
                    ? spiralCandyTransitionFrames
                    : spiralCandyFrames;

                foreach (var cell in scatterCells)
                {
                    cell.transform.SetAsLastSibling();
                    cell.PlayScatterTriggerAnimation(triggerFrames, 1.4f);
                }
                yield return new WaitForSeconds(1.5f);
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
                cell.gameObject.SetActive(true);
                return cell;
            }

            Transform parent = gridContainer != null ? gridContainer : transform;
            var newCell = Instantiate(cellPrefab, parent);
            newCell.gameObject.SetActive(true);
            return newCell;
        }

        private void RecycleCell(CellView cell)
        {
            if (cell == null) return;
            cell.ResetCell();
            cell.gameObject.SetActive(false);
            if (!cellPool.Contains(cell))
            {
                cellPool.Add(cell);
            }
        }

        private void OnDestroy()
        {
            ClearGrid();
            foreach (var cell in cellPool)
            {
                if (cell != null) Destroy(cell.gameObject);
            }
            cellPool.Clear();
        }
    }
}
