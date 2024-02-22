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
                
                SpriteRenderer renderer = new GameObject($"Cell {cell.Index}").AddComponent<SpriteRenderer>();
                renderer.transform.parent = _board.Cells.StorageLocation;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.sprite = _board.Cells.Templates[cell.Type].View;
                renderer.transform.localPosition = cell.Index * renderer.size;

                cell = new(cell.Index, renderer, cell.Type);
                
                if(cell.Type == DataByCellOnBoard.Types.UsedToMove)
                    bounds.Encapsulate(renderer.gameObject.AddComponent<BoxCollider2D>().bounds);
            }

            foreach (ProtoEntity entity in _board.Figures.ItFigureLocatedOnCell)
            {
                DataByFigureOnBoard figure = _board.Figures.Pool.Get(entity);
                DataByCellOnBoard cell = _board.Cells.Pool.Get(entity);

                SpriteRenderer renderer = new GameObject($"Figure {figure.Belong} [{entity}]").AddComponent<SpriteRenderer>();
                renderer.transform.parent = _board.Figures.StorageLocation;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.sprite = _board.Figures.Templates[figure.Belong].View;
                renderer.transform.localPosition = cell.Index * cell.Renderer.size;
                renderer.sortingOrder = 1;
            }

            bounds.Expand(_board.DistanceFromScreenBorder);

            float vertical = bounds.size.y;
            float horizontal = bounds.size.x * _board.Camera.pixelHeight / _board.Camera.pixelWidth;

            _board.Camera.transform.position = bounds.center + new Vector3(0, 0, -10);
            _board.Camera.orthographicSize = Mathf.Max(horizontal, vertical) * 0.5f;
        }
    }
}