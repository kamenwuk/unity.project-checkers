namespace Core.Board.Figure
{
    public struct DataByFigureOnBoard
    {
        public readonly Belongs Belong;
        public Types type;

        public DataByFigureOnBoard(Belongs belong, Types type = Types.Default)
        {
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
