using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LightOff.Presentation
{
    public class Player : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            _flashLight = GetComponentInChildren<Light2D>();
        }

        void SetFlashlight(bool enabled)
        {
            _flashLight.enabled = enabled;
        }

        Light2D _flashLight;
    }
}
