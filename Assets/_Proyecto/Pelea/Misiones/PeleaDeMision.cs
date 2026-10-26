using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Progresion;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Misiones
{
    /// <summary>
    /// Las peleas callejeras de misión (T-185, D-tony-35, D-tony-147): cuando el paso en curso es Pelear levanta una emboscada en el
    /// punto de la misión con los datos que le tocan en <see cref="ConfigPeleasMision"/>, y al vencerla cumple el paso. Si el mecha del
    /// jugador cae en KO durante la pelea, la misión falla por KO. Si la misión termina por otra causa, la emboscada se retira sin dejar
    /// rivales. No es una pelea del flujo: no hay campana, presentador ni premio de liga. Va en PeleasDeMision.prefab.
    /// </summary>
    public class PeleaDeMision : MonoBehaviour
    {
        [SerializeField] private ConfigPeleasMision config;

        private DirectorEmboscada director;
        private DatosEmboscada datosActuales;
        private IPeleador jugadorPeleador;
        private string idMision = "";
        private int indicePaso = -1;
        private bool cumplida;
        private bool objetivoFijado;
        private bool avisadoSinDatos;
        private bool avisadoSinPunto;

        /// <summary>True mientras hay una pelea de misión levantada.</summary>
        public bool HayPelea => director != null;

        /// <summary>La emboscada en curso, o null.</summary>
        public DirectorEmboscada Director => director;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Pelea] PeleaDeMision en " + name + " no tiene ConfigPeleasMision.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Retirar();
        }

        private void Update()
        {
            ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
            bool hayPaso = misiones != null && misiones.HayMision && misiones.PasoActual.Tipo == TipoPaso.Pelear;
            if (!hayPaso)
            {
                Retirar();
                idMision = "";
                indicePaso = -1;
                cumplida = false;
                return;
            }

            if (misiones.Actual.Id != idMision || misiones.IndicePaso != indicePaso)
            {
                Retirar();
                idMision = misiones.Actual.Id;
                indicePaso = misiones.IndicePaso;
                cumplida = false;
            }
            if (director == null && !cumplida) Levantar(misiones);
            if (director != null) ActualizarObjetivo(misiones);
        }

        private void Levantar(ServicioMisiones misiones)
        {
            DatosEmboscada datos = config.Buscar(idMision);
            if (datos == null)
            {
                if (!avisadoSinDatos)
                {
                    avisadoSinDatos = true;
                    Debug.LogWarning("[Pelea] La misión " + idMision + " tiene un paso Pelear y no está en ConfigPeleasMision.", this);
                }
                return;
            }
            IPuntoMision punto = BuscarPunto(misiones.PasoActual.IdPunto);
            if (punto == null)
            {
                if (!avisadoSinPunto)
                {
                    avisadoSinPunto = true;
                    Debug.LogWarning("[Pelea] Falta el punto " + misiones.PasoActual.IdPunto + " de la pelea de " + idMision + ".", this);
                }
                return;
            }

            // Inactivo mientras se configura: el director valida sus datos en Awake.
            GameObject objeto = new GameObject("PeleaDeMision " + idMision);
            objeto.SetActive(false);
            objeto.transform.SetPositionAndRotation(punto.Pose.position, punto.Pose.rotation);
            DirectorEmboscada nuevo = objeto.AddComponent<DirectorEmboscada>();
            nuevo.Configurar(datos);
            objeto.SetActive(true);
            nuevo.Habilitada = true;
            nuevo.Terminada += AlTerminarLaPelea;
            director = nuevo;
            datosActuales = datos;
            ResolverJugador();
        }

        private void ResolverJugador()
        {
            jugadorPeleador = null;
            foreach (MonoBehaviour candidato in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Jugador)
                {
                    jugadorPeleador = peleador;
                    break;
                }
            }
            if (jugadorPeleador != null) jugadorPeleador.Vida.KO += AlCaerElJugador;
        }

        private void AlTerminarLaPelea()
        {
            cumplida = true;
            Retirar();
            Registro<ServicioMisiones>.Actual?.CumplirPasoExterno(TipoPaso.Pelear);
        }

        private void AlCaerElJugador()
        {
            cumplida = true;
            Retirar();
            Registro<ServicioMisiones>.Actual?.FallarExterno(MotivoFalloMision.KO);
        }

        // Quita la emboscada y con ella a los rivales que queden.
        private void Retirar()
        {
            if (jugadorPeleador != null)
            {
                jugadorPeleador.Vida.KO -= AlCaerElJugador;
                jugadorPeleador = null;
            }
            objetivoFijado = false;
            if (director == null) return;
            director.Terminada -= AlTerminarLaPelea;
            Destroy(director.gameObject);
            director = null;
            datosActuales = null;
        }

        // A pie pide el mecha (con el texto de la emboscada); en el mecha dice el texto del paso.
        private void ActualizarObjetivo(ServicioMisiones misiones)
        {
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            IJugador jugador = Registro<IJugador>.Actual;
            IPuntoMision punto = BuscarPunto(misiones.PasoActual.IdPunto);
            if (objetivos == null || jugador == null || punto == null) return;
            string texto = jugador.Modo == ModoJugador.Mecha ? misiones.PasoActual.Texto : datosActuales.TextoLlamar;
            Objetivo deseado = new Objetivo(texto, punto.Pose.position);
            bool igual = objetivoFijado && objetivos.HayObjetivo && objetivos.Actual.Texto == deseado.Texto
                && objetivos.Actual.TienePosicion && objetivos.Actual.Posicion == deseado.Posicion;
            if (igual) return;
            objetivos.Fijar(deseado);
            objetivoFijado = true;
        }

        private static IPuntoMision BuscarPunto(string id)
        {
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto != null && punto.Id == id) return punto;
            }
            return null;
        }
    }
}
