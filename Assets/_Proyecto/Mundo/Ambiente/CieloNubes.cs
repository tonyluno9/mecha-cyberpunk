using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Presentación: el domo de nubes (shader MechaCyberpunk/NubesNoche) sigue a la cámara y su opacidad sigue el peso
    /// de noche de <see cref="ConfigCicloDia"/> según la hora del reloj del mundo: de día no se ve.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CieloNubes : MonoBehaviour
    {
        private const float OpacidadMinima = 0.001f;
        private static readonly int IdOpacidad = Shader.PropertyToID("_Opacidad");
        private static readonly int IdVelocidad = Shader.PropertyToID("_Velocidad");

        [SerializeField] private ConfigAmbienteNoche config;
        [SerializeField] private ConfigCicloDia ciclo;

        private MeshRenderer domo;
        private MaterialPropertyBlock bloque;
        private Camera camara;

        private void Awake()
        {
            domo = GetComponent<MeshRenderer>();
            if (config == null || ciclo == null || domo.sharedMaterial == null)
            {
                Debug.LogError("[Mundo] CieloNubes en '" + name + "' necesita config, ciclo y material.", this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
            transform.localScale = Vector3.one * (config.RadioDomo * 2f);
        }

        private void LateUpdate()
        {
            if (camara == null || !camara.isActiveAndEnabled)
            {
                camara = Camera.main;
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            Colocar(camara, reloj != null ? (float?)reloj.Hora : null);
        }

        /// <summary>
        /// Pone el domo en la cámara con la opacidad de esa hora (sin hora: noche completa). Lo usa LateUpdate y, en el editor,
        /// las capturas del cielo (S2n), que no corren Awake.
        /// </summary>
        public void Colocar(Camera camaraDomo, float? hora)
        {
            if (domo == null)
            {
                domo = GetComponent<MeshRenderer>();
            }
            if (config == null || ciclo == null || domo == null)
            {
                return;
            }
            if (bloque == null)
            {
                bloque = new MaterialPropertyBlock();
                transform.localScale = Vector3.one * (config.RadioDomo * 2f);
            }
            if (camaraDomo != null)
            {
                transform.position = camaraDomo.transform.position;
            }
            float peso = hora.HasValue ? ciclo.PesoNoche(hora.Value) : 1f;
            float opacidad = config.OpacidadNubes * peso;
            domo.enabled = opacidad > OpacidadMinima;
            domo.GetPropertyBlock(bloque);
            bloque.SetFloat(IdOpacidad, opacidad);
            bloque.SetVector(IdVelocidad, config.VelocidadNubes);
            domo.SetPropertyBlock(bloque);
        }
    }
}
