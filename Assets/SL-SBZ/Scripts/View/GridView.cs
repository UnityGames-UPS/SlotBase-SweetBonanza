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
        [SerializeField] private float initialDropDuration = 0.50f;
        [SerializeField] private float columnStaggerDelay = 0.07f;
        [SerializeField] private float winPulseDuration = 0.38f;
        [SerializeField] private float explodeDuration = 0.22f;
        [SerializeField] private float tumbleFallDuration = 0.32f;

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

        private bool isQuickSpinMode = false;
        private CellView[,] activeCells = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private readonly List<CellView> cellPool = new List<CellView>();
        private Dictionary<SymbolType, Sprite> spriteLookup;

        internal void SetQuickSpinMode(bool isQuick)
        {
            isQuickSpinMode = isQuick;
            if (isQuick)
            {
                initialDropDuration = 0.22f;
                columnStaggerDelay = 0.03f;
                winPulseDuration = 0.18f;
                explodeDuration = 0.12f;
                tumbleFallDuration = 0.16f;
            }
            else
            {
                initialDropDuration = 0.50f;
                columnStaggerDelay = 0.07f;
                winPulseDuration = 0.38f;
                explodeDuration = 0.22f;
                tumbleFallDuration = 0.32f;
            }
        }

        private void Awake()
        {
            InitializeSpriteLookup();
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

        internal Sprite GetSymbolSprite(SymbolType symbol)
        {
            if (spriteLookup == null) InitializeSpriteLookup();
            return spriteLookup.TryGetValue(symbol, out var sprite) ? sprite : null;
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
            // 1. Wipe previous spin symbols downwards if any exist
            int clearedCount = 0;
            int totalToClear = 0;
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null) totalToClear++;
                }
            }

            if (totalToClear > 0)
            {
                for (int c = 0; c < GameConfig.COLUMNS; c++)
                {
                    float colWipeDelay = c * (isQuickSpinMode ? 0.015f : 0.03f);
                    for (int r = 0; r < GameConfig.ROWS; r++)
                    {
                        var oldCell = activeCells[c, r];
                        if (oldCell != null)
                        {
                            oldCell.PlayClearDropAnimation(colWipeDelay, () =>
                            {
                                RecycleCell(oldCell);
                                clearedCount++;
                            });
                            activeCells[c, r] = null;
                        }
                    }
                }
                while (clearedCount < totalToClear) yield return null;
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

            // 3. Drop in new columns sequentially
            int completedDrops = 0;
            int totalDrops = GameConfig.TOTAL_CELLS;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                float delay = columnDelays[c];
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

                    cell.PlayDropInAnimation(targetPos, delay, duration, () => completedDrops++);
                }
            }

            while (completedDrops < totalDrops) yield return null;
            yield return new WaitForSeconds(0.08f);
        }

        internal IEnumerator PlayTumbleStepSequence(TumbleStep step)
        {
            // 1. Win pulse
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null) cell.PlayWinPulseAnimation(null);
            }
            yield return new WaitForSeconds(winPulseDuration);

            // 2. Explode and pop with burst particle effect
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null)
                {
                    cell.PlayExplodeAnimation(() => RecycleCell(cell));
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
            yield return new WaitForSeconds(0.10f);
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
