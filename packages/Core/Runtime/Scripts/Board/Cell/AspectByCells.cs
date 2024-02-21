using Leopotam.EcsProto.QoL;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board.Cell
{
    [System.Serializable]
    public sealed class AspectByCells : ProtoAspectInject
    {
        public Vector2Int Quantity => _quantity;
        public Transform StorageLocation => _storageLocation;
        public Sprite SpriteForRegularCell => _spriteForRegularCell;
        public Sprite SpriteCellUsedToMove => _spriteCellUsedToMove;

        public readonly ProtoIt ItCell = new(It.Inc<DataByCellOnBoard>());
        public readonly ProtoPool<DataByCellOnBoard> Pool = null;

        [SerializeField] private Transform _storageLocation = null;
        [SerializeField] private Vector2Int _quantity = Vector2Int.zero;
        [SerializeField] private Sprite _spriteForRegularCell = null;
        [SerializeField] private Sprite _spriteCellUsedToMove = null;

        public override void Init(ProtoWorld world)
        {
            base.Init(world);
        }
    }
}