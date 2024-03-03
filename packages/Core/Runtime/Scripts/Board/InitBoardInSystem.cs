using System.Collections.Generic;
using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board
{
    using Figure;
    using Cell;

    public sealed class InitBoardInSystem : IProtoInitSystem
    {
        [DI] private readonly AspectByBoard _board = null;

        private const int NUMBER_ROWS_FIGURES = 3;

        public void Init(IProtoSystems systems)
        {
            ProtoWorld world = systems.World();
            Dictionary<Vector2Int, ProtoEntity> arrayCellByIndex = new();
            for (int x = -_board.Cells.Quantity.x / 2; x < _board.Cells.Quantity.x / 2; x++)
            {
                for (int y = -_board.Cells.Quantity.y / 2; y < _board.Cells.Quantity.y / 2; y++)
                {
                    Vector2Int index = new(x, y);
                    ProtoEntity entity = world.NewEntity();
                    ref DataByCellOnBoard cell = ref _board.Cells.Pool.Add(entity);

                    cell = new(index, type: (DataByCellOnBoard.Types)Mathf.Abs(Mathf.Abs(x) % 2 - Mathf.Abs(y) % 2));
                    arrayCellByIndex.Add(index, entity);
                }
            }
            Debug.Log(_board.Cells.Pool.Len());

            int[] rowsToFigure = new int[NUMBER_ROWS_FIGURES * 2]; 
            for(int index = 0; index < NUMBER_ROWS_FIGURES; index++)
            {
                int number = NUMBER_ROWS_FIGURES - index;
                rowsToFigure[index] = number - (_board.Cells.Quantity.y / 2) - 1;
                rowsToFigure[^(index + 1)] = (_board.Cells.Quantity.y / 2) - number;

            }

            for(int indexToRow = 0; indexToRow < rowsToFigure.Length; indexToRow++)
            {
                int row = rowsToFigure[indexToRow];
                for (int x = -_board.Cells.Quantity.x / 2; x < _board.Cells.Quantity.x / 2; x++)
                {
                    if (Mathf.Abs(Mathf.Abs(x) % 2 - Mathf.Abs(row) % 2) == 0)
                        continue;

                    Vector2Int index = new(x, row);
                    ProtoEntity entity = arrayCellByIndex[index];

                    ref DataByFigureOnBoard figure = ref _board.Figures.Pool.Add(entity);
                    figure = new(belong: (DataByFigureOnBoard.Belongs)Mathf.Sign(row - 1));
                }
            }
            Debug.Log(_board.Figures.Pool.Len());
        }
    }
}