using Core.Board.Cell;
using Core.Board.Figure;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using UnityEngine;

namespace Core.Board
{
    public sealed class DrawBoardToSystem : IProtoInitSystem
    {
        [DI] private readonly AspectByBoard _board = null;
        // TODO: Bring it to mind
        public void Init(IProtoSystems systems)
        {
            Bounds bounds = new();
            Vector2 size = Vector2.zero;
            foreach(ProtoEntity entity in _board.Cells.ItCell)
            {
                DataByCellOnBoard cell = _board.Cells.Pool.Get(entity);
                SpriteRenderer renderer = new GameObject($"Cell {cell.Index}").AddComponent<SpriteRenderer>();
                renderer.transform.parent = _board.Cells.StorageLocation;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.sprite = cell.UsedToMove ? _board.Cells.SpriteCellUsedToMove : _board.Cells.SpriteForRegularCell;
                renderer.transform.localPosition = cell.Index * renderer.size;
                
                bounds.Encapsulate(renderer.gameObject.AddComponent<BoxCollider2D>().bounds);
                
                size = renderer.size;
            }

            foreach (ProtoEntity entity in _board.Figures.ItFigureLocatedOnCell)
            {
                DataByFigureOnBoard figure = _board.Figures.Pool.Get(entity);
                DataByCellOnBoard cell = _board.Cells.Pool.Get(entity);

                SpriteRenderer renderer = new GameObject($"Figure {figure.Belong} [{entity}]").AddComponent<SpriteRenderer>();
                renderer.transform.parent = _board.Figures.StorageLocation;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.sprite = figure.Belong == DataByFigureOnBoard.Belongs.White ? _board.Figures.SpriteFigureBelongingToWhite : _board.Figures.SpriteFigureBelongingToBlack;
                renderer.transform.localPosition = cell.Index * size;
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