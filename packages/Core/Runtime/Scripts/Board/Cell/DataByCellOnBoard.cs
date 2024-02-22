using UnityEngine;

namespace Core.Board.Cell
{
    public readonly struct DataByCellOnBoard
    {
        public readonly SpriteRenderer Renderer;
        public readonly Types Type;
        public readonly Vector2Int Index;

        public DataByCellOnBoard(Vector2Int index, SpriteRenderer renderer = null, Types type = Types.Default)
        {
            Index = index;
            Type = type;
            Renderer = renderer;
        }
        public enum Types : int
        {
            Default = 0,
            UsedToMove = 1
        }
    }
}