using System.Collections.Generic;
using Alchemy.Serialization;
using Leopotam.EcsProto.QoL;
using Alchemy.Inspector;
using Leopotam.EcsProto;
using UnityEngine;

namespace Core.Board.Cell
{
    [System.Serializable, AlchemySerialize]
    public partial class AspectByCells : ProtoAspectInject
    {
        public Vector2Int Quantity => _quantity;
        public Transform StorageLocation => _storageLocation;
        public IReadOnlyDictionary<DataByCellOnBoard.Types, TemplateToCell> Templates => _templates;

        public readonly ProtoIt ItCell = new(It.Inc<DataByCellOnBoard>());
        public readonly ProtoPool<DataByCellOnBoard> Pool = null;

        [AlchemySerializeField, System.NonSerialized, ShowInInspector] private Dictionary<DataByCellOnBoard.Types, TemplateToCell> _templates = new();
        [SerializeField] private Transform _storageLocation = null;
        [SerializeField] private Vector2Int _quantity = Vector2Int.zero;
        
        public override void Init(ProtoWorld world)
        {
            base.Init(world);
        }
    }
}