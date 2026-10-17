using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Historia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// Corre el flashback (T-131): una secuencia de planos de cámara con subtítulo y fundidos. Cada plano busca su punto en
    /// <c>Registro&lt;IPuntoHistoria&gt;</c>, pone la cámara en su pose y lo activa mientras dura. Mantener ESPACIO salta al final.
    /// Al terminar avanza la historia a Despertar y va a la ciudad (guardando). Usa tiempo sin escala.
    /// </summary>
    public class DirectorFlashback : MonoBehaviour
    {
        [SerializeField] private DatosFlashback datos;
        [Tooltip("Rutas de las escenas. Demo por defecto; Pruebas_Flashback usa DatosEscenas_Pruebas.")]
        [SerializeField] private DatosEscenas datosEscenas;
        [SerializeField] private Camera camara;
        [SerializeField] private PantallaSubtitulos subtitulos;
        [Tooltip("Mantener para saltar el flashback.")]
        [SerializeField] private InputAction saltar = new InputAction("FlashbackSaltar", InputActionType.Button, "<Keyboard>/space");

        private bool saltando;
        private float segundosMantenido;
        private IPuntoHistoria puntoActivo;

        public bool Corriendo { get; private set; }
        public bool Terminado { get; private set; }
        public bool Saltado => saltando;
        public int IndicePlano { get; private set; } = -1;
        public DatosFlashback Datos => datos;
        public Camera Camara => camara;
        public PantallaSubtitulos Subtitulos => subtitulos;

        /// <summary>Se avisa al empezar cada plano (cámara ya en su pose, aún en negro), con su índice.</summary>
        public event Action<int> PlanoIniciado;

        /// <summary>Se avisa una vez al terminar, justo antes de pedir el cambio de escena.</summary>
        public event Action Finalizado;

        private void Awake()
        {
            if (datos == null || datosEscenas == null || camara == null || subtitulos == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en DirectorFlashback de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            saltar.Enable();
        }

        private void OnDisable()
        {
            saltar.Disable();
            SoltarPunto();
        }

        private void Update()
        {
            if (!Corriendo || saltando)
            {
                return;
            }
            if (saltar.IsPressed())
            {
                segundosMantenido += Time.unscaledDeltaTime;
                if (segundosMantenido >= datos.SegundosParaSaltar)
                {
                    saltando = true;
                }
            }
            else
            {
                segundosMantenido = 0f;
            }
        }

        private IEnumerator Start()
        {
            if (!enabled)
            {
                yield break;
            }
            // Un cuadro: la partida se carga y los puntos se registran.
            yield return null;
            Corriendo = true;
            IHistoria historia = Registro<IHistoria>.Actual;
            bool enFlashback = historia != null && historia.Etapa == EtapaPrologo.Flashback;
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido == null)
            {
                Debug.LogWarning("[Progresion] DirectorFlashback sin pantalla de fundido: corre sin fundidos.", this);
            }
            else
            {
                fundido.Fijar(1f);
            }
            subtitulos.MostrarAviso(datos.TextoSalto);

            for (int i = 0; i < datos.Planos.Count && !saltando; i++)
            {
                yield return CorrerPlano(i, datos.Planos[i], fundido);
            }

            if (saltando)
            {
                SoltarPunto();
                subtitulos.Ocultar();
                yield return Fundir(fundido, 1f, datos.SegundosFundido, false);
            }
            subtitulos.MostrarAviso(string.Empty);
            Corriendo = false;
            Terminado = true;
            Finalizado?.Invoke();

            if (!enFlashback)
            {
                Debug.LogWarning("[Progresion] El flashback terminó fuera de la etapa Flashback: no se avanza la historia ni se cambia de escena.", this);
                yield break;
            }
            ServicioHistoria servicio = historia as ServicioHistoria;
            if (servicio == null || !servicio.Avanzar(EtapaPrologo.Despertar))
            {
                Debug.LogError("[Progresion] DirectorFlashback no pudo avanzar la historia a Despertar.", this);
            }
            CambioEscena.Ir(datosEscenas.RutaPara(EtapaPrologo.Despertar));
        }

        private IEnumerator CorrerPlano(int indice, PlanoFlashback plano, PantallaFundido fundido)
        {
            IndicePlano = indice;
            IPuntoHistoria punto = BuscarPunto(plano.idPunto);
            if (punto == null)
            {
                Debug.LogWarning("[Progresion] Falta el punto de historia '" + plano.idPunto + "': el plano " + (indice + 1)
                    + " se queda en la pose anterior.", this);
            }
            else
            {
                Pose pose = punto.Pose;
                camara.transform.SetPositionAndRotation(pose.position, pose.rotation);
                puntoActivo = punto;
                punto.Activar(true);
            }
            subtitulos.Mostrar(plano.subtitulo);
            PlanoIniciado?.Invoke(indice);

            yield return Fundir(fundido, 0f, datos.SegundosFundido, true);
            yield return Esperar(plano.segundos);
            if (saltando)
            {
                yield break; // el final lo cierra Start, con su fundido
            }
            yield return Fundir(fundido, 1f, datos.SegundosFundido, true);
            SoltarPunto();
            subtitulos.Ocultar();
        }

        private void SoltarPunto()
        {
            if (puntoActivo != null)
            {
                IPuntoHistoria punto = puntoActivo;
                puntoActivo = null;
                punto.Activar(false);
            }
        }

        private static IPuntoHistoria BuscarPunto(string id)
        {
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private IEnumerator Esperar(float segundos)
        {
            float transcurrido = 0f;
            while (transcurrido < segundos && !saltando)
            {
                yield return null;
                transcurrido += Time.unscaledDeltaTime;
            }
        }

        // Un fundido que se corta si se salta (cuando puedeCortarse). Sin pantalla de fundido solo espera.
        private IEnumerator Fundir(PantallaFundido fundido, float alfa, float segundos, bool puedeCortarse)
        {
            if (fundido == null)
            {
                yield return Esperar(segundos);
                yield break;
            }
            IEnumerator paso = fundido.Fundir(alfa, segundos);
            while ((!puedeCortarse || !saltando) && paso.MoveNext())
            {
                yield return paso.Current;
            }
        }
    }
}
