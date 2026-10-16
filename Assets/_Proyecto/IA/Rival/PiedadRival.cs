using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Hace que el rival no deje perder: sus golpes nunca dejan a su objetivo debajo del piso de vida y, cuando la vida del
    /// objetivo baja del umbral, se debilita (pega menos, ataca menos seguido, se mueve más lento, recibe más daño y se
    /// ve oscuro y pulsando) hasta que la pelea se prepara de nuevo. Va en el prefab del rival; sin él, el rival no tiene piedad.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(VidaMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class PiedadRival : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConfigPiedadRival config;

        private RivalBasico rival;
        private VidaMecha vidaPropia;
        private PeleadorMecha peleador;
        private PresentacionRival presentacion;
        private ReglaPiedad regla;
        private MaterialPropertyBlock bloque;
        private GameObject objetivoConocido;
        private IVida vidaObjetivo;

        public bool EstaDebil => regla != null && regla.EstaDebil;

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            vidaPropia = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            presentacion = GetComponent<PresentacionRival>();
            if (config == null)
            {
                Debug.LogError("[IA] Falta el ConfigPiedadRival en PiedadRival de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaPiedad(config.FraccionPiso, config.UmbralDebilitar);
            bloque = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            rival.LimitarDanio = LimitarDanio;
            peleador.PeleaPreparada += Reiniciar;
        }

        private void OnDisable()
        {
            peleador.PeleaPreparada -= Reiniciar;
            if (rival != null)
            {
                rival.LimitarDanio = null;
            }
            Reiniciar();
        }

        private void Update()
        {
            if (regla.EstaDebil)
            {
                return;
            }
            ResolverObjetivo();
            if (vidaObjetivo != null && regla.Actualizar(vidaObjetivo.Actual, vidaObjetivo.Maxima))
            {
                Debilitarse();
            }
        }

        private void LateUpdate()
        {
            if (!regla.EstaDebil || presentacion == null || presentacion.Parpadeando)
            {
                return;
            }
            // Entre un parpadeo de daño y otro, el cuerpo pulsa oscuro: así se ve que el rival está débil.
            float t = Mathf.PingPong(Time.time * config.FrecuenciaPulso * 2f, 1f);
            Renderer cuerpo = presentacion.Cuerpo;
            cuerpo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, Color.Lerp(config.ColorDebil, config.ColorDebilAlterno, t));
            cuerpo.SetPropertyBlock(bloque);
        }

        private void ResolverObjetivo()
        {
            if (rival.Objetivo == objetivoConocido)
            {
                return;
            }
            objetivoConocido = rival.Objetivo;
            IPeleador peleadorObjetivo = objetivoConocido != null ? objetivoConocido.GetComponent<IPeleador>() : null;
            vidaObjetivo = peleadorObjetivo != null ? peleadorObjetivo.Vida : null;
        }

        private float LimitarDanio(float danio)
        {
            ResolverObjetivo();
            if (vidaObjetivo == null)
            {
                return danio;
            }
            return regla.DanioAPedir(danio, vidaObjetivo.Actual, vidaObjetivo.Maxima);
        }

        private void Debilitarse()
        {
            rival.MultiplicadorDanio = config.MultiplicadorDanio;
            rival.MultiplicadorEspera = config.MultiplicadorEspera;
            rival.MultiplicadorVelocidad = config.MultiplicadorVelocidad;
            vidaPropia.MultiplicadorDanioRecibido = config.MultiplicadorDanioRecibido;
            Debug.Log("[IA] " + peleador.Nombre + " se debilita.");
        }

        private void Reiniciar()
        {
            if (regla == null)
            {
                return;
            }
            bool estabaDebil = regla.EstaDebil;
            regla.Reiniciar();
            if (!estabaDebil)
            {
                return;
            }
            rival.MultiplicadorDanio = 1f;
            rival.MultiplicadorEspera = 1f;
            rival.MultiplicadorVelocidad = 1f;
            vidaPropia.MultiplicadorDanioRecibido = 1f;
            if (presentacion != null)
            {
                presentacion.Cuerpo.SetPropertyBlock(null);
            }
        }
    }
}
