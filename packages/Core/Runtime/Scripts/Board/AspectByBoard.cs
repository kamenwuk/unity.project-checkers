using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board
{
    using Figure;
    using Cell;

    [System.Serializable]
    public sealed class AspectByBoard : IProtoAspect
    {
        public AspectByCells Cells => _cells;
        public AspectByFigures Figures => _figures;
        public float Size { get; private set; }
        public Camera Camera => _camera;
        public float DistanceFromScreenBorder => _distanceFromScreenBorder;

        [SerializeField] private Camera _camera = null;
        [SerializeField] private AspectByCells _cells = null;
        [SerializeField] private AspectByFigures _figures = null;
        [SerializeField, Min(0f)] private float _distanceFromScreenBorder  = 0f;

        public void Init(ProtoWorld world)
        {
            world.AddAspect(this);
            _cells.Init(world);
            _figures.Init(world);
        }
        public void PostInit()
        {
            _cells.PostInit();
            _figures.PostInit();
        }
    }
}