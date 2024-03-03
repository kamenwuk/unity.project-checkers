using UnityEngine.InputSystem;
using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board.Figure
{
    public sealed class SelectFigureInSystem : IProtoInitSystem, IProtoDestroySystem
    {
        //[DI] private readonly ISetMoveToFigure _setMoveToFigure = default;
        [DI] private readonly AspectByBoard _board = null;

        private readonly InputSchemeOnBoard _inputScheme = null;
        
        private ProtoWorld _world = null;
        
        public SelectFigureInSystem(InputSchemeOnBoard inputScheme)
        {
            _inputScheme = inputScheme;
        }
        public void Init(IProtoSystems systems)
        {
            _world = systems.World();
            _inputScheme.Figure.Select.performed += FindFigure;
            _inputScheme.Enable();
        }
        public void Destroy()
        {
            _inputScheme.Disable();
            _inputScheme.Figure.Select.performed -= FindFigure;
        }
        private void FindFigure(InputAction.CallbackContext context)
        {
            //if (_setMoveToFigure.Selected != null)
            //    return;

            RaycastHit2D hit = Physics2D.Raycast(_board.Camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()), Vector2.zero);

            if (hit.collider == null) return;

            _board.ItCellWithFigure.BeginCaching();
            {
                foreach (ProtoEntity entity in _board.ItCellWithFigure)
                {
                    DataObjectWithCollider dataObjectWithCollider = _board.Cells.PoolCellsWithCollider.Get(entity);
                    if (dataObjectWithCollider.ID != hit.collider.GetInstanceID())
                        continue;

                    Debug.Log(hit.collider.name);
                    //DataByFigureOnBoard figure =  _board.Figures.Pool.Get(entity);
                    //_setMoveToFigure.Selected = _world.PackEntity(entity);
                    break;
                }
            }
            _board.ItCellWithFigure.EndCaching();
        }
    }
}