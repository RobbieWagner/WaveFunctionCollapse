using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace RobbieWagnerGames.WaveFunctionCollapse
{
    public class CellTileContext
    {
        public Tile tile;
        public int connections;

        public CellTileContext(Tile _tile, int _connections)
        {
            tile = _tile;
            connections = _connections;
        }

        public override string ToString()
        {
            return $"left:{tile.left} right:{tile.right} bottom:{tile.bottom} top:{tile.top} connections:{connections}";
        }
    }

    public class Cell
    {
        public Tile currentTilePrefab {get; private set;}
        public Tile currentTileInstance;
        public TileType tileType;
        public bool isTileSet => currentTilePrefab != null;
        public bool cellIsSettable {get; private set;}
        private List<CellTileContext> possibleTiles;

        public int xPos;
        public int yPos;

        public void Initialize(int x, int y, ProceduralGrid grid)
        {
            possibleTiles = new List<CellTileContext>();
            foreach(Tile tile in grid.possibleTilePrefabs)
            {
                possibleTiles.Add(new CellTileContext(tile, 4));
            }
            cellIsSettable = true;
            xPos = x;
            yPos = y;
        }

        public void Reset()
        {
            //Debug.Log("reset");
            cellIsSettable = true; 
        }

        public void RemoveInvalidPossibilitiesByTile(Tile adjacentTile, Direction tileDirection)
        {
            foreach(CellTileContext context in possibleTiles)
            {
                if(!TileHelper.CanTilesConnect(context.tile, adjacentTile, tileDirection))
                    context.connections--;
            }
            if(xPos == 1 && yPos == 1)
                Debug.Log($"{tileDirection} {this}");
            //possibleTiles = possibleTiles.Where(x => x.connections > 2).ToList();
        }

        public int CountPossibilities(int minConnections)
        {
            //return possibleTiles.Where(c => c.connections >= minConnections).Select(x => x.tile).Count();
            return possibleTiles.Select(t => t.connections).Aggregate((a,b) => a + b);
        }

        public bool SetTile(Tile tile)
        {
            if(currentTileInstance != null)
                return false;

            currentTilePrefab = tile;
            return true;
        }

        public bool SetTile(out Tile selectedTile, ProceduralGrid grid, int minConnections = 3)
        {
            if(!possibleTiles.Any())
            {
                selectedTile = null;
                cellIsSettable = false;
                return false;
            }

            selectedTile = grid.SelectTile(possibleTiles.Where(c => c.connections >= minConnections).Select(x => x.tile).ToList());
            if(selectedTile == null)
            {
                cellIsSettable = false;
                return false;
            }
            currentTilePrefab = selectedTile;
            tileType = currentTilePrefab.main;
            return true;
        }

        public override string ToString()
        {
            string prefab = currentTilePrefab != null ? currentTilePrefab.name : "NONE";
            string instance = currentTileInstance != null ? currentTileInstance.name : "NONE";
            string connections = string.Join('\n', possibleTiles.Select(t => t.ToString()));
            return $"position: {xPos},{yPos}\ntile prefab: {prefab}\ntile instance: {instance}\nConnection Info:\n{connections}";
        }
    }
}