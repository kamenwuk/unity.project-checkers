using UnityEngine;

namespace Core.Board.Cell
{
    public readonly struct DataByCellOnBoard
    {
        public readonly bool UsedToMove;
        public readonly Vector2Int Index;

        public DataByCellOnBoard(Vector2Int index, bool usedToMove)
        {
            Index = index;
            UsedToMove = usedToMove;
        }
    }
}