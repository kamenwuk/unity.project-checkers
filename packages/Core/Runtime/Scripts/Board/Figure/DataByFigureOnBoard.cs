using UnityEngine;

namespace Core.Board.Figure
{
    public struct DataByFigureOnBoard
    {
        public readonly SpriteRenderer View;
        public readonly Belongs Belong;
        public Types type;

        public DataByFigureOnBoard(Belongs belong, SpriteRenderer view = null, Types type = Types.Default)
        {
            View = view;
            Belong = belong;
            this.type = type;
        }

        public enum Types
        {
            Default,
            Queen
        }
        public enum Belongs : int
        {
            White = 1,
            Black = -1
        }
    }
}
