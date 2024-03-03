using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Core
{
    public readonly struct DataObjectWithCollider
    {
        public readonly int ID;

        public DataObjectWithCollider(int id)
        {
            ID = id;
        }
    }
}
