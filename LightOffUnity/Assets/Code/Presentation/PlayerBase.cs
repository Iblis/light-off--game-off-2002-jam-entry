using LightOff.Logic;
using UnityEngine;

namespace LightOff.Presentation
{
    public abstract class PlayerBase : MonoBehaviour
    {
        protected virtual void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public virtual void UpdateFrom(IEntityState state)
        {
            // TODO: is there a way to 'cast' System.Numerics.Vector2 to UnityEngine.Vector3?
            transform.position = new Vector3(state.Position.X, state.Position.Y);
            if (state.Health == 0)
            {
                _renderer.color = UnityEngine.Color.gray;
            }
        }

        protected SpriteRenderer _renderer;
    }
}
