using System;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEngine;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// GUARDIA del rival (T-201, D-tony-196): si aprendió GUARDIA, sube los brazos al ver venir el golpe del jugador y lo bloquea (bloqueo
    /// normal, nunca perfecto) con la probabilidad de su nivel. Lee el <see cref="GolpeMecha"/> del objetivo y aplica
    /// <see cref="ReglaGuardiaRival"/> a la <see cref="GuardiaMecha"/> del rival (en mando por código). Va dentro de los controlados del
    /// <c>PeleadorMecha</c>: el aturdimiento y el KO apagan este componente y bajan la guardia.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(GuardiaMecha))]
    [RequireComponent(typeof(AdaptacionRival))]
    public class GuardiaRival : MonoBehaviour
    {
        private RivalBasico rival;
        private GuardiaMecha guardia;
        private AdaptacionRival adaptacion;
        private HabilidadRival habilidad;
        private IPeleador propio;
        private ReglaGuardiaRival regla;
        private Func<float> azar = () => UnityEngine.Random.value;
        private GameObject objetivoConocido;
        private GolpeMecha golpeObjetivo;
        private IPeleador peleadorObjetivo;
        private bool suscrito;

        /// <summary>Estado de la regla (para pruebas).</summary>
        public EstadoGuardiaRival Estado => regla != null ? regla.Estado : EstadoGuardiaRival.Abajo;

        /// <summary>Cambia el azar de la decisión de cubrirse (0 a 1). Solo para pruebas; null vuelve al azar normal.</summary>
        public void FijarAzar(Func<float> nuevo)
        {
            azar = nuevo ?? (() => UnityEngine.Random.value);
        }

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            guardia = GetComponent<GuardiaMecha>();
            adaptacion = GetComponent<AdaptacionRival>();
            habilidad = GetComponent<HabilidadRival>();
            propio = GetComponent<IPeleador>();
            if (propio == null || adaptacion.Datos == null)
            {
                Debug.LogError("[IA] Faltan referencias en GuardiaRival de " + name, this);
                enabled = false;
                return;
            }
            DatosAdaptacionRival datos = adaptacion.Datos;
            regla = new ReglaGuardiaRival(datos.ProbabilidadGuardia, datos.ReaccionGuardia, datos.SostenerTrasImpacto, () => azar());
        }

        private void OnEnable()
        {
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
            suscrito = true;
        }

        private void OnDisable()
        {
            if (suscrito)
            {
                EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
                suscrito = false;
            }
            if (regla != null)
            {
                regla.Reiniciar();
            }
            if (guardia != null)
            {
                guardia.Sostener(false);
            }
        }

        private void Update()
        {
            if (rival.Objetivo != objetivoConocido)
            {
                ResolverObjetivo();
            }
            bool habilidadActiva = rival.Detenido || (habilidad != null && habilidad.EnAviso);
            if (habilidadActiva && regla.Estado != EstadoGuardiaRival.Abajo)
            {
                // No bloquea a propósito mientras prepara una habilidad.
                regla.Reiniciar();
            }
            bool libre = rival.Fase == FaseGolpe.Listo && !habilidadActiva;
            bool preparando = golpeObjetivo != null && golpeObjetivo.Fase == FaseGolpe.Preparando;
            guardia.Sostener(regla.Actualizar(Time.deltaTime, adaptacion.Nivel(TipoAdaptacion.Guardia), preparando, libre));
        }

        private void ResolverObjetivo()
        {
            objetivoConocido = rival.Objetivo;
            golpeObjetivo = objetivoConocido != null ? objetivoConocido.GetComponent<GolpeMecha>() : null;
            peleadorObjetivo = objetivoConocido != null ? objetivoConocido.GetComponent<IPeleador>() : null;
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (!ReferenceEquals(objetivo, propio) || info.Tipo != TipoDanio.Golpe || resultado.Bloqueo != TipoBloqueo.Normal
                || peleadorObjetivo == null || !ReferenceEquals(info.Atacante, peleadorObjetivo))
            {
                return;
            }
            // La guardia del rival solo la sube esta adaptación: un bloqueo normal con ella arriba es suyo.
            if (regla.Sosteniendo && guardia.EstaArriba)
            {
                adaptacion.AvisarUso(TipoAdaptacion.Guardia);
            }
        }
    }
}
