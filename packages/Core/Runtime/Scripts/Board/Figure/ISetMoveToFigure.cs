using Leopotam.EcsProto.QoL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Board.Figure
{
    public interface ISetMoveToFigure
    {
        public ProtoPackedEntity? Selected { get; set; }
    }
}