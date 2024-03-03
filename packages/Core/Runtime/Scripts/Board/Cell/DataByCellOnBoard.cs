using UnityEngine;

namespace Core.Board.Cell
{
    public readonly struct DataByCellOnBoard
    {
        public readonly SpriteRenderer View;
        public readonly Types Type;
        public readonly Vector2Int Index;

        public DataByCellOnBoard(Vector2Int index, SpriteRenderer view = null, Types type = Types.Default)
        {
            Index = index;
            Type = type;
            View = view;
        }
        public enum Types : int
        {
            Default = 0,
            UsedToMove = 1
        }
    }
}