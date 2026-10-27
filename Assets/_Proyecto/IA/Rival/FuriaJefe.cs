using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Pelea de jefe en dos tiempos: cuando la vida del rival baja al umbral entra en furia (pega más, ataca más seguido,
    /// se mueve más rápido y el cuerpo pulsa en rojo) hasta que la pelea se prepara de nuevo. Va en el prefab del jefe.
    /// No va junto a <see cref="PiedadRival"/>: el jefe sí se puede perder.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(VidaMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class FuriaJefe : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConfigFuriaJefe config;

        private RivalBasico rival;
        private VidaMecha vidaPropia;
        private PeleadorMecha peleador;
        private PresentacionRival presentacion;
        private ReglaFuriaJefe regla;
        private MaterialPropertyBlock bloque;

        public bool EnFuria => regla != null && regla.EnFuria;

        /// <summary>Se avisa una vez, al entrar en furia.</summary>
        public event Action FuriaIniciada;

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            vidaPropia = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            presentacion = GetComponent<PresentacionRival>();
            if (GetComponent<PiedadRival>() != null)
            {
                Debug.LogError("[IA] FuriaJefe y PiedadRival no van juntos en " + name + ": el jefe sí se puede perder.", this);
                enabled = false;
                return;
            }
            if (config == null)
            {
                Debug.LogError("[IA] Falta el ConfigFuriaJefe en FuriaJefe de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaFuriaJefe(config.UmbralVida);
            bloque = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            peleador.PeleaPreparada += Reiniciar;
        }

        private void OnDisable()
        {
            peleador.PeleaPreparada -= Reiniciar;
            Reiniciar();
        }

        private void Update()
        {
            if (regla.Actualizar(vidaPropia.Actual, vidaPropia.Maxima))
            {
                rival.MultiplicadorDanio = config.MultiplicadorDanio;
                rival.MultiplicadorEspera = config.MultiplicadorEspera;
                rival.MultiplicadorVelocidad = config.MultiplicadorVelocidad;
                Debug.Log("[IA] " + peleador.Nombre + " entra en furia.");
                FuriaIniciada?.Invoke();
            }
        }

        private void LateUpdate()
        {
            if (!regla.EnFuria || presentacion == null || presentacion.Parpadeando)
            {
                return;
            }
            // Entre un parpadeo de daño y otro, el cuerpo pulsa del color base al rojo.
            float t = Mathf.PingPong(Time.time * config.FrecuenciaPulso * 2f, 1f);
            Renderer cuerpo = presentacion.Cuerpo;
            Color colorBase = cuerpo.sharedMaterial.GetColor(IdColorBase);
            cuerpo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, Color.Lerp(colorBase, config.ColorFuria, t));
            cuerpo.SetPropertyBlock(bloque);
        }

        private void Reiniciar()
        {
            if (regla == null)
            {
                return;
            }
            bool estabaEnFuria = regla.EnFuria;
            regla.Reiniciar();
            if (!estabaEnFuria)
            {
                return;
            }
            rival.MultiplicadorDanio = 1f;
            rival.MultiplicadorEspera = 1f;
            rival.MultiplicadorVelocidad = 1f;
            if (presentacion != null)
            {
                presentacion.Cuerpo.SetPropertyBlock(null);
            }
        }
    }
}
