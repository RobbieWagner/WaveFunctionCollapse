using UnityEngine;

namespace RobbieWagnerGames.WaveFunctionCollapse
{
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    public static class TileHelper 
    {
        public static bool CanTilesConnect(Tile tile1, Tile tile2, Direction directionOfTile2)
        {
            switch (directionOfTile2)
            {
                case Direction.Left:
                return tile1.left == tile2.right || tile1.left == TileType.Any || tile2.right == TileType.Any;
                case Direction.Right:
                return tile1.right == tile2.left || tile1.right == TileType.Any || tile2.left == TileType.Any;
                case Direction.Up:
                return tile1.top == tile2.bottom || tile1.top == TileType.Any || tile2.bottom == TileType.Any;
                case Direction.Down:
                return tile1.bottom == tile2.top || tile1.bottom == TileType.Any || tile2.top == TileType.Any;
                default:
                return false;
            }
        }    
    }
}