using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board
{
    using Figure;
    using Cell;
    using Leopotam.EcsProto.QoL;

    [System.Serializable]
    public sealed class AspectByBoard : ProtoAspectInject
    {
        public AspectByCells Cells => _cells;
        public AspectByFigures Figures => _figures;
        public float Size { get; private set; }
        public Camera Camera => _camera;
        public float DistanceFromScreenBorder => _distanceFromScreenBorder;
        
        public readonly ProtoItCached ItCellWithFigure = new(It.Inc<DataByCellOnBoard, DataByFigureOnBoard, DataObjectWithCollider>());

        [SerializeField] private Camera _camera = null;
        [SerializeField] private AspectByCells _cells = null;
        [SerializeField] private AspectByFigures _figures = null;
        [SerializeField, Min(0f)] private float _distanceFromScreenBorder  = 0f;
    }
}