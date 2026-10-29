using System;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEngine;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// FINTAS del rival (T-202, D-tony-196): si aprendió FINTAS, alarga al azar el aviso de su golpe y a veces amaga (enseña el golpe y lo
    /// guarda). Aplica <see cref="ReglaFintas"/> con los ganchos de <see cref="RivalBasico"/>. Las fintas son solo del golpe: la habilidad
    /// de los jefes no cambia. Va dentro de los controlados del <c>PeleadorMecha</c>.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(AdaptacionRival))]
    public class FintasRival : MonoBehaviour
    {
        private RivalBasico rival;
        private AdaptacionRival adaptacion;
        private ReglaFintas regla;
        private Func<float> azar = () => UnityEngine.Random.value;
        private bool amagoPendiente;
        private bool amagando;
        private float segundosAmago;
        private Func<float> gancho;

        /// <summary>Cambia el azar (0 a 1) de las fintas. Solo para pruebas; null vuelve al azar normal.</summary>
        public void FijarAzar(Func<float> nuevo)
        {
            azar = nuevo ?? (() => UnityEngine.Random.value);
        }

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            adaptacion = GetComponent<AdaptacionRival>();
            if (adaptacion.Datos == null || rival.Datos == null || rival.Datos.Mecha == null)
            {
                Debug.LogError("[IA] Faltan referencias en FintasRival de " + name, this);
                enabled = false;
                return;
            }
            DatosAdaptacionRival datos = adaptacion.Datos;
            regla = new ReglaFintas(datos.ExtraAvisoMaximo, datos.ProbabilidadAmago, datos.FraccionAmago, datos.EsperaTrasAmago, () => azar());
            gancho = PedirExtra;
        }

        private void OnEnable()
        {
            rival.PreparacionExtra = gancho;
            rival.GolpeIniciado += AlIniciarGolpe;
        }

        private void OnDisable()
        {
            rival.GolpeIniciado -= AlIniciarGolpe;
            if (rival.PreparacionExtra == gancho)
            {
                rival.PreparacionExtra = null;
            }
            amagoPendiente = false;
            amagando = false;
            if (regla != null)
            {
                regla.Reiniciar();
            }
        }

        // RivalBasico lo pide justo al empezar cada golpe: aquí se decide el extra y si es un amago.
        private float PedirExtra()
        {
            amagoPendiente = regla.Empezar(adaptacion.Nivel(TipoAdaptacion.Fintas), out float extra);
            return extra;
        }

        private void AlIniciarGolpe()
        {
            amagando = amagoPendiente;
            amagoPendiente = false;
            segundosAmago = regla.SegundosDeAmago(rival.Datos.Mecha.PreparacionGolpe);
        }

        private void Update()
        {
            if (!amagando)
            {
                return;
            }
            if (!rival.EnAviso)
            {
                amagando = false;
                return;
            }
            if (rival.SegundosDeAviso >= segundosAmago)
            {
                amagando = false;
                if (rival.CancelarGolpe(regla.EsperaTrasAmago))
                {
                    adaptacion.AvisarUso(TipoAdaptacion.Fintas);
                }
            }
        }
    }
}
