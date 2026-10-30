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
        [SerializeField] private float initialDropDuration = 0.80f;
        [SerializeField] private float columnStaggerDelay = 0.11f;
        [SerializeField] private float winPulseDuration = 0.95f;
        [SerializeField] private float explodeDuration = 0.28f;
        [SerializeField] private float tumbleFallDuration = 0.44f;

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
        [SerializeField] private Sprite[] glassEffectFrames;

        private bool isQuickSpinMode = false;
        private CellView[,] activeCells = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private readonly List<CellView> cellPool = new List<CellView>();
        private Dictionary<SymbolType, Sprite> spriteLookup;
        private Dictionary<SymbolType, Sprite[]> animationLookup;

        internal void SetQuickSpinMode(bool isQuick)
        {
            isQuickSpinMode = isQuick;
            if (isQuick)
            {
                initialDropDuration = 0.28f;
                columnStaggerDelay = 0.04f;
                winPulseDuration = 0.40f;
                explodeDuration = 0.15f;
                tumbleFallDuration = 0.20f;
            }
            else
            {
                initialDropDuration = 0.80f;
                columnStaggerDelay = 0.11f;
                winPulseDuration = 0.95f;
                explodeDuration = 0.28f;
                tumbleFallDuration = 0.44f;
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
                { SymbolType.Lollipop, spiralCandyFrames }
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
            float offset = (cellSize.y + cellSpacing.y) * (spawnIndex + 1);
            return new Vector2(baseTop.x, baseTop.y + offset + 80f);
        }

        internal IEnumerator PlayInitialDropSequence(int[,] matrix)
        {
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

            // 2. Count scatters per column to calculate anticipation delays
            int[] scattersPerCol = new int[GameConfig.COLUMNS];
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (matrix[c, r] == (int)SymbolType.Lollipop) scattersPerCol[c]++;
                }
            }

            int cumulativeScatters = 0;
            float currentCumulativeDelay = 0f;
            bool anticipationTriggered = false;
            float[] columnDelays = new float[GameConfig.COLUMNS];
            float[] columnDurations = new float[GameConfig.COLUMNS];

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                columnDelays[c] = currentCumulativeDelay;
                if (anticipationTriggered && !isQuickSpinMode)
                {
                    columnDurations[c] = initialDropDuration * 1.5f;
                    currentCumulativeDelay += columnStaggerDelay + 0.60f;
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

            // 3. Cascade exit and entrance column by column (the way it goes out is the same way it comes in)
            float clearDuration = isQuickSpinMode ? 0.22f : 0.42f;
            float clearOverlap = totalOldCells > 0 ? (clearDuration * 0.45f) : 0f;
            int completedDrops = 0;
            int totalDrops = GameConfig.TOTAL_CELLS;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                float colStartDelay = columnDelays[c];

                // Old symbols in column c slide down out of the grid with gravitational acceleration
                if (oldCellsByCol[c].Count > 0)
                {
                    foreach (var oldCell in oldCellsByCol[c])
                    {
                        oldCell.PlayClearDropAnimation(colStartDelay, clearDuration, () =>
                        {
                            RecycleCell(oldCell);
                        });
                    }
                }

                // New symbols in column c slide down into the grid right behind them
                float dropInDelay = colStartDelay + clearOverlap;
                float duration = columnDurations[c];

                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    var cell = GetOrCreateCell();
                    activeCells[c, r] = cell;

                    Vector2 targetPos = GetCellPosition(c, r);
                    Vector2 spawnPos = GetSpawnPosition(c, GameConfig.ROWS - r);

                    cell.RectTransform.anchoredPosition = spawnPos;
                    cell.Setup(symbol, GetSymbolSprite(symbol));

                    cell.PlayDropInAnimation(targetPos, dropInDelay, duration, () =>
                    {
                        completedDrops++;
                    });
                }
            }

            while (completedDrops < totalDrops) yield return null;
            AudioManager.Instance?.PlayReelStop();
            yield return new WaitForSeconds(0.12f);
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

            yield return new WaitForSeconds(winPulseDuration);

            // 2. Explode and pop with glass / candy burst effect
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null)
                {
                    cell.PlayExplodeAnimation(glassEffectFrames, () => RecycleCell(cell));
                    activeCells[pos.Column, pos.Row] = null;
                }
            }
            yield return new WaitForSeconds(explodeDuration);

            // 3. Gravity drop for surviving symbols
            var newGrid = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
            int fallingCount = 0;
            int totalFalling = step.FallingSymbols.Count;

            foreach (var fall in step.FallingSymbols)
            {
                var cell = activeCells[fall.Column, fall.FromRow];
                if (cell != null)
                {
                    newGrid[fall.Column, fall.ToRow] = cell;
                    activeCells[fall.Column, fall.FromRow] = null;
                    Vector2 targetPos = GetCellPosition(fall.Column, fall.ToRow);
                    cell.PlayFallAnimation(targetPos, tumbleFallDuration, () => fallingCount++);
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

            // 4. Spawn new symbols from top
            int spawnCount = 0;
            int totalSpawns = step.NewSpawns.Count;
            var colSpawnCounters = new Dictionary<int, int>();

            foreach (var spawn in step.NewSpawns)
            {
                if (!colSpawnCounters.ContainsKey(spawn.Column)) colSpawnCounters[spawn.Column] = 0;
                int spawnIndex = colSpawnCounters[spawn.Column]++;

                var cell = GetOrCreateCell();
                newGrid[spawn.Column, spawn.TargetRow] = cell;

                Vector2 targetPos = GetCellPosition(spawn.Column, spawn.TargetRow);
                Vector2 spawnPos = GetSpawnPosition(spawn.Column, spawnIndex);

                cell.RectTransform.anchoredPosition = spawnPos;
                cell.Setup(spawn.Symbol, GetSymbolSprite(spawn.Symbol));
                cell.PlayFallAnimation(targetPos, tumbleFallDuration + (spawnIndex * 0.04f), () => spawnCount++);
            }

            activeCells = newGrid;
            while (fallingCount < totalFalling || spawnCount < totalSpawns) yield return null;
            AudioManager.Instance?.PlayReelStop();
            yield return new WaitForSeconds(0.22f);
        }

        internal IEnumerator PlayMultiplierBombRevealSequence(List<BombMultiplierInfo> bombs)
        {
            if (bombs == null || bombs.Count == 0) yield break;

            int completed = 0;
            for (int i = 0; i < bombs.Count; i++)
            {
                var bomb = bombs[i];
                var cell = activeCells[bomb.Position.Column, bomb.Position.Row];
                if (cell != null)
                {
                    cell.transform.SetAsLastSibling();
                    Sprite bombSprite = GetMultiplierBombSprite(bomb.MultiplierValue);
                    cell.Setup(SymbolType.MultiplierBomb, bombSprite, bomb.MultiplierValue);
                    cell.PlayMultiplierBombRevealAnimation(bomb.MultiplierValue, () => completed++);
                }
                else completed++;
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

            if (scatterCells.Count >= 4)
            {
                foreach (var cell in scatterCells)
                {
                    cell.transform.SetAsLastSibling();
                    cell.PlayScatterTriggerAnimation(spiralCandyFrames, 1.4f);
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
            cellPool.Add(cell);
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
