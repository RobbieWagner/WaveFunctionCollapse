using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobbieWagnerGames.WaveFunctionCollapse
{
    public enum TileType
    {
        Any = -2,
        None = -1,
        Water = 0,
        Sand = 1,
        Grass = 2,
    }
    public class Tile : MonoBehaviour
    {

        [Header("Surrounding Tiles (Static)")]
        public TileType top = TileType.Any;
        public TileType bottom = TileType.Any;
        public TileType left = TileType.Any;
        public TileType right = TileType.Any;
        public TileType main = TileType.Any;

        public SpriteRenderer tileSprite;
        //public int requiredConnections = 4;

        public virtual void Initialize(ref List<List<Cell>> grid, Cell parentCell)
        {
            
        }
    }
}
