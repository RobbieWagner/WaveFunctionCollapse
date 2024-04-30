using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using Unity.VisualScripting;

namespace RobbieWagnerGames.WaveFunctionCollapse
{
    public enum TileSelectionStrategy
    {
        Random,
        FavorDefault,
        AvoidMostUsed
    }

    public class ProceduralGrid : MonoBehaviour
    {
        [Header("General")]
        private List<List<Cell>> grid;
        [SerializeField] private Transform gridParentPrefab;
        private Transform gridParent;
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Vector2 originOffset;
        [SerializedDictionary("Tile Type", "Count")] [SerializeField] public SerializedDictionary<TileType, int> tileAmounts;
        [SerializeField] private int sizeX = 50;
        [SerializeField] private int sizeY = 50;

        [Header("Generation")]        
        public List<Tile> possibleTilePrefabs;
        [SerializeField] private Tile debugTile;
        [SerializeField] private TileSelectionStrategy tileSelectionStrategy;
        [SerializeField] private TileType defaultTileType;
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int maxAttempts = 3;
        
        #region General
        private void Awake()
        {
            OnPlaceTile += OnPlaceTileHandler;

            if(generateOnStart)
               StartCoroutine(GenerateTilemap());
        }

        public void InitializeGrid(int sizeX, int sizeY)
        {
            Debug.Log("initializing...");
            if(gridParent != null)
              Destroy(gridParent.gameObject);
            
            Debug.Log("creating grid");
            grid = new List<List<Cell>>();
            Debug.Log("instantiating");
            gridParent = Instantiate(gridParentPrefab);
            Debug.Log("setting parent");
            gridParent.SetParent(transform);
            
            Debug.Log("initializing cells");
            for(int x = 0; x < sizeX; x++) //TODO: May want to collapse to 1D later
            {
                grid.Add(new List<Cell>());
                for(int y = 0; y < sizeY; y++)
                {
                    Cell newCell = new Cell();
                    newCell.Initialize(x, y, this);
                    grid[x].Add(newCell);
                }
            }

            Debug.Log("initialized");
        }

        public TileType GetMostUsedTileType()
        {
            TileType tileType  = TileType.None;
            int currentMax = 0;
            foreach(KeyValuePair<TileType, int> tileAmount in tileAmounts)
            {
                if(tileAmount.Value > currentMax)
                {
                    tileType = tileAmount.Key;
                    currentMax = tileAmount.Value;
                }
            }
            Debug.Log(tileType);
            return tileType;
        }

        public int CountUnsetCells()
        {
            return grid.SelectMany(x => x).Where(x => !x.isTileSet).Count();
        }

        private List<Cell> GetUnsettableCells()
        {
            return grid.SelectMany(x => x).Where(x => !x.cellIsSettable).ToList();
        }

        public Tile SelectTile(List<Tile> tileOptions)
        {
            //Debug.Log(tileOptions.Count);
            switch(tileSelectionStrategy)
            {
                case TileSelectionStrategy.AvoidMostUsed:
                    int max = 0;
                    TileType tileType = TileType.None;
                    foreach(TileType key in tileAmounts.Keys)
                    {
                        if(tileAmounts[key] > max)
                        {
                            max = tileAmounts[key];
                            tileType = key;
                        }
                    }
                    List<Tile> tileOptionsLeastUsed = tileOptions.Where(x => x.main != tileType).ToList();
                    return tileOptionsLeastUsed.Any() ? tileOptionsLeastUsed[UnityEngine.Random.Range(0, tileOptionsLeastUsed.Count)] : 
                        tileOptions.Any() ? tileOptions[UnityEngine.Random.Range(0, tileOptions.Count)] : null;
                case TileSelectionStrategy.FavorDefault:
                    List<Tile> tileOptionsFavored = tileOptions.Where(x => x.main == defaultTileType).ToList();
                    return tileOptionsFavored.Any() ? tileOptionsFavored[UnityEngine.Random.Range(0, tileOptionsFavored.Count)] : 
                        tileOptions.Any() ? tileOptions[UnityEngine.Random.Range(0, tileOptions.Count)] : null;
                case TileSelectionStrategy.Random:
                default:
                return tileOptions.Any() ? tileOptions[UnityEngine.Random.Range(0, tileOptions.Count)] : null;
            }
        }
        #endregion

        #region Generation
        public IEnumerator GenerateTilemap()
        {
            //Debug.Log("generating...");
            //int attempt = 1;
            int requiredConnections = 4;
            List<Cell> unsettableCells = new List<Cell>();
            InitializeGrid(sizeX, sizeY);

            Tile tileSelection = possibleTilePrefabs[UnityEngine.Random.Range(0, possibleTilePrefabs.Count)];
            SetTile(tileSelection, UnityEngine.Random.Range(0, sizeX), UnityEngine.Random.Range(0, sizeY));

            // Keep going until map is filled
            while(CountUnsetCells() > 0)
            {
                //Debug.Log(CountUnsetCells());
                //Set the next tile
                SetNextTile(requiredConnections);
                yield return null;

                //If no more tiles can be set, reduce the requirement on connections.
                if(CountUnsetCells() - GetUnsettableCells().Count <= 0)
                {
                    Debug.Log("reduce connection requirement");
                    unsettableCells = GetUnsettableCells();
                    if(requiredConnections == 0)
                    {
                        if(unsettableCells.Any())
                        {
                            foreach(Cell cell in unsettableCells)
                            {
                                cell.Reset();
                                var mostFrequentTypes = GetConnectedTileTypes(cell).Where(x => x != TileType.None)
                                                .GroupBy(t => t)
                                                .OrderByDescending(t => t.Count())
                                                .Select(k => k.Key);
                                
                                TileType backupType = mostFrequentTypes.Any() ? mostFrequentTypes.First() : TileType.Any;
                                var possibleTiles = possibleTilePrefabs.Where(x => x.main == backupType);
                                if(possibleTiles.Any())
                                    cell.SetTile(possibleTiles.ToList()[UnityEngine.Random.Range(0, possibleTiles.Count())]);
                                else
                                    cell.SetTile(debugTile);
                                
                            }
                        }
                    }
                    else
                    {
                        foreach(Cell cell in unsettableCells)
                            cell.Reset();
                        requiredConnections--;
                    }
                }
            }

            Debug.Log("done generating tiles");
        }

        private List<TileType> GetConnectedTileTypes(Cell cell)
        {
            int x = cell.xPos;
            int y = cell.yPos;

            return new List<TileType>
            {
                x + 1 < grid.Count && grid[x+1][y].currentTileInstance != null ? grid[x+1][y].currentTileInstance.left : TileType.None,
                x - 1 > -1 && grid[x-1][y].currentTileInstance != null ? grid[x-1][y].currentTileInstance.right : TileType.None,
                y + 1 < grid[0].Count && grid[x][y+1].currentTileInstance != null ? grid[x][y+1].currentTileInstance.bottom : TileType.None,
                y - 1 > -1 && grid[x][y-1].currentTileInstance != null ? grid[x][y-1].currentTileInstance.top : TileType.None
            };
        }

        private void OnPlaceTileHandler(Tile tile, Cell cell)
        {
            if (tileAmounts.Keys.Contains(tile.main))
                tileAmounts[tile.main]++;
        }

        public bool SetTile(Tile tile, int x, int y)
        {
            Cell cell = grid[x][y];
            //Debug.Log($"{cell}");
            if(!cell.SetTile(tile))
                return false;
            //Debug.Log($"new {cell}");
            InstantiateTile(tile, cell);
            //Debug.Log("tile set");
            return true;
        }

        public bool SetTile(Cell cell, int minimumConnections)
        {
            Tile selectedTile;
            if(!cell.SetTile(out selectedTile, this, minimumConnections))
                return false;
            InstantiateTile(selectedTile, cell);
            //Debug.Log("random tile set");
            return true;
        }

        public void SetNextTile(int minimumConnections)
        {
            Cell nextCell = grid.SelectMany(x => x)
                            .Where(cell => !cell.isTileSet && cell.cellIsSettable)
                            .OrderBy(x => x.CountPossibilities(minimumConnections)).FirstOrDefault();

            SetTile(nextCell, minimumConnections);
        }

        private void InstantiateTile(Tile tile, Cell cell)
        {
            Tile instantiatedTile = Instantiate(tile, gridParent);
            cell.currentTileInstance = instantiatedTile;
            UpdateGrid(instantiatedTile, cell.xPos, cell.yPos);
            OnPlaceTile?.Invoke(tile, cell);
        }
        public delegate void OnPlaceTileDelegate(Tile tile, Cell cell);
        public event OnPlaceTileDelegate OnPlaceTile;

        private void UpdateGrid(Tile tile, int x, int y)
        {
            tile.transform.position = originOffset + new Vector2(x, y);
            //Debug.Log($"{tile.transform.position} {x} {y}");
            if(x + 1 < grid.Count)
                grid[x+1][y].RemoveInvalidPossibilitiesByTile(tile, Direction.Left);
            if(x - 1 > -1)
                grid[x-1][y].RemoveInvalidPossibilitiesByTile(tile, Direction.Right);
            if(y + 1 < grid[0].Count)
                grid[x][y+1].RemoveInvalidPossibilitiesByTile(tile, Direction.Down);
            if(y - 1 > -1)
                grid[x][y-1].RemoveInvalidPossibilitiesByTile(tile, Direction.Up);
        }

        #endregion
    }
}