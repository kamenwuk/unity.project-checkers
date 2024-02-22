using UnityEngine;

namespace Core.Board.Cell
{
    [CreateAssetMenu(fileName = "TemplateToCell", menuName = "Tools/Template/Cell")]
    public sealed class TemplateToCell : ScriptableObject
    {
        public Sprite View => _view;
        [SerializeField] private Sprite _view;
    }
}