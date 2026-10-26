using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using SweetBonanza.Core;

namespace SweetBonanza.View
{
    internal class GridView : MonoBehaviour
    {
        [Header("Symbol Sprites Configuration")]
        [SerializeField] private Sprite spriteBanana;
        [SerializeField] private Sprite spriteGrapes;
        [SerializeField] private Sprite spriteWatermelon;
        [SerializeField] private Sprite spritePlum;
        [SerializeField] private Sprite spriteApple;
        [SerializeField] private Sprite spriteBlueCandy;
        [SerializeField] private Sprite spriteGreenCandy;
        [SerializeField] private Sprite spritePurpleCandy;
        [SerializeField] private Sprite spriteRedHeart;
        [SerializeField] private Sprite spriteScatter;
        [SerializeField] private Sprite spriteMultiplierBomb;

        [Header("Grid Layout Settings")]
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(145f, 125f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(10f, 10f);

        [Header("Animation Settings")]
        [SerializeField] private float initialDropDuration = 0.45f;
        [SerializeField] private float columnStaggerDelay = 0.05f;
        [SerializeField] private float winPulseDuration = 0.42f;
        [SerializeField] private float explodeDuration = 0.25f;
        [SerializeField] private float tumbleFallDuration = 0.35f;

        private CellView[,] activeCells = new CellView[GameConfig.COLUMNS, GameConfig.ROWS];
        private readonly List<CellView> cellPool = new List<CellView>();

        private Dictionary<SymbolType, Sprite> spriteLookup;

        private void Awake()
        {
            InitializeSpriteLookup();
        }

        private void InitializeSpriteLookup()
        {
            spriteLookup = new Dictionary<SymbolType, Sprite>
            {
                { SymbolType.Banana, spriteBanana },
                { SymbolType.Grapes, spriteGrapes },
                { SymbolType.Watermelon, spriteWatermelon },
                { SymbolType.Plum, spritePlum },
                { SymbolType.Apple, spriteApple },
                { SymbolType.BlueCandy, spriteBlueCandy },
                { SymbolType.GreenCandy, spriteGreenCandy },
                { SymbolType.PurpleCandy, spritePurpleCandy },
                { SymbolType.RedHeart, spriteRedHeart },
                { SymbolType.Scatter, spriteScatter },
                { SymbolType.MultiplierBomb, spriteMultiplierBomb }
            };
        }

        internal void SetQuickSpinMode(bool isQuick)
        {
            if (isQuick)
            {
                initialDropDuration = 0.22f;
                columnStaggerDelay = 0.025f;
                winPulseDuration = 0.25f;
                explodeDuration = 0.15f;
                tumbleFallDuration = 0.18f;
            }
            else
            {
                initialDropDuration = 0.45f;
                columnStaggerDelay = 0.05f;
                winPulseDuration = 0.42f;
                explodeDuration = 0.25f;
                tumbleFallDuration = 0.35f;
            }
        }

        internal Sprite GetSymbolSprite(SymbolType symbol)
        {
            if (spriteLookup == null) InitializeSpriteLookup();
            return spriteLookup.TryGetValue(symbol, out var sprite) ? sprite : null;
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
            ClearGrid();

            int completedDrops = 0;
            int totalDrops = GameConfig.TOTAL_CELLS;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                float columnDelay = c * columnStaggerDelay;

                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    var cell = GetOrCreateCell();
                    activeCells[c, r] = cell;

                    Vector2 targetPos = GetCellPosition(c, r);
                    Vector2 spawnPos = GetSpawnPosition(c, GameConfig.ROWS - r);

                    cell.RectTransform.anchoredPosition = spawnPos;
                    cell.Setup(symbol, GetSymbolSprite(symbol));

                    cell.PlayDropInAnimation(targetPos, columnDelay, initialDropDuration, () =>
                    {
                        completedDrops++;
                    });
                }
            }

            while (completedDrops < totalDrops)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        internal IEnumerator PlayTumbleStepSequence(TumbleStep step)
        {
            int pulseCount = 0;
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null)
                {
                    cell.PlayWinPulseAnimation(() => pulseCount++);
                }
                else
                {
                    pulseCount++;
                }
            }

            yield return new WaitForSeconds(winPulseDuration);

            int explodeCount = 0;
            foreach (var pos in step.ExplodedPositions)
            {
                var cell = activeCells[pos.Column, pos.Row];
                if (cell != null)
                {
                    cell.PlayExplodeAnimation(() =>
                    {
                        RecycleCell(cell);
                        explodeCount++;
                    });
                    activeCells[pos.Column, pos.Row] = null;
                }
                else
                {
                    explodeCount++;
                }
            }

            yield return new WaitForSeconds(explodeDuration);

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
                else
                {
                    fallingCount++;
                }
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (activeCells[c, r] != null && newGrid[c, r] == null)
                    {
                        newGrid[c, r] = activeCells[c, r];
                    }
                }
            }

            int spawnCount = 0;
            int totalSpawns = step.NewSpawns.Count;

            var colSpawnCounters = new Dictionary<int, int>();
            foreach (var spawn in step.NewSpawns)
            {
                if (!colSpawnCounters.ContainsKey(spawn.Column))
                {
                    colSpawnCounters[spawn.Column] = 0;
                }
                int spawnIndex = colSpawnCounters[spawn.Column]++;

                var cell = GetOrCreateCell();
                newGrid[spawn.Column, spawn.TargetRow] = cell;

                Vector2 targetPos = GetCellPosition(spawn.Column, spawn.TargetRow);
                Vector2 spawnPos = GetSpawnPosition(spawn.Column, spawnIndex);

                cell.RectTransform.anchoredPosition = spawnPos;
                cell.Setup(spawn.Symbol, GetSymbolSprite(spawn.Symbol));

                cell.PlayFallAnimation(targetPos, tumbleFallDuration + (spawnIndex * 0.05f), () => spawnCount++);
            }

            activeCells = newGrid;

            while (fallingCount < totalFalling || spawnCount < totalSpawns)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.12f);
        }

        internal IEnumerator PlayMultiplierBombRevealSequence(List<BombMultiplierInfo> bombs)
        {
            if (bombs == null || bombs.Count == 0) yield break;

            int completed = 0;
            foreach (var bomb in bombs)
            {
                var cell = activeCells[bomb.Position.Column, bomb.Position.Row];
                if (cell != null)
                {
                    cell.PlayMultiplierBombRevealAnimation(bomb.MultiplierValue, () => completed++);
                }
                else
                {
                    completed++;
                }
            }

            while (completed < bombs.Count)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
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
