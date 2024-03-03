using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using Core.Board.Figure;
using Core.Board.Cell;
using UnityEngine;

namespace Core.Board
{
    public sealed class DrawBoardInSystem : IProtoInitSystem
    {
        [DI] private readonly AspectByBoard _board = null;

        public void Init(IProtoSystems systems)
        {
            Bounds bounds = new();
            foreach(ProtoEntity entity in _board.Cells.ItCell)
            {
                ref DataByCellOnBoard cell = ref _board.Cells.Pool.Get(entity);
                cell = new(cell.Index, new GameObject($"Cell {cell.Index}").AddComponent<SpriteRenderer>(), cell.Type);
                cell.View.transform.parent = _board.Cells.StorageLocation;
                cell.View.sprite = _board.Cells.Templates[cell.Type].View;
                cell.View.transform.localPosition = cell.Index * cell.View.size;
                
                if(cell.Type == DataByCellOnBoard.Types.UsedToMove)
                {
                    BoxCollider2D collider = cell.View.gameObject.AddComponent<BoxCollider2D>();
                    
                    ref DataObjectWithCollider objectWithCollider = ref _board.Cells.PoolCellsWithCollider.Add(entity);
                    objectWithCollider = new(collider.GetInstanceID());
                    
                    bounds.Encapsulate(collider.bounds);
                }
            }

            foreach (ProtoEntity entity in _board.Figures.ItFigureLocatedOnCell)
            {
                DataByCellOnBoard cell = _board.Cells.Pool.Get(entity);

                DataByFigureOnBoard figure = _board.Figures.Pool.Get(entity);
                figure = new(figure.Belong, new GameObject($"Figure {figure.Belong} [{entity}]").AddComponent<SpriteRenderer>(), figure.type);
                figure.View.transform.parent = cell.View.transform;
                figure.View.transform.localPosition = Vector3.zero;
                figure.View.sprite = _board.Figures.Templates[figure.Belong].Views[figure.type];
                figure.View.sortingOrder++;
            }

            bounds.Expand(_board.DistanceFromScreenBorder);

            float vertical = bounds.size.y;
            float horizontal = bounds.size.x * _board.Camera.pixelHeight / _board.Camera.pixelWidth;

            _board.Camera.transform.position = bounds.center + new Vector3(0, 0, -10);
            _board.Camera.orthographicSize = Mathf.Max(horizontal, vertical) * 0.5f;
        }
    }
}