using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Progresion;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Emboscada
{
    /// <summary>
    /// Emboscada.prefab (T-071): cuando el jugador llega, aparece la pandilla; los rivales solo pelean mientras el jugador va
    /// en el mecha (un golpe de mecha mataría a una persona a pie). Termina cuando todos caen y avisa <see cref="Terminada"/>.
    /// Trata a los rivales solo como <see cref="IPeleador"/>. Opcionalmente fija el objetivo de la historia en cada fase
    /// (D-tony-35). Se coloca con su +Z hacia el fondo del callejón.
    /// </summary>
    public class DirectorEmboscada : MonoBehaviour
    {
        private class Rival
        {
            public GameObject objeto;
            public IPeleador peleador;
            public float tiempoKO = -1f;
        }

        [SerializeField] private DatosEmboscada datos;
        [Tooltip("Si es verdadero, empieza sola cuando el jugador entra al radio. Si no, solo con Empezar().")]
        [SerializeField] private bool activarAlAcercarse = true;

        private readonly List<Rival> rivales = new List<Rival>();
        private MaquinaEmboscada maquina;
        private bool objetivoFijado;

        public EstadoEmboscada Estado => maquina != null ? maquina.Estado : EstadoEmboscada.Esperando;

        /// <summary>Los rivales que aparecieron (los que ya se retiraron siguen en la lista como destruidos).</summary>
        public IReadOnlyList<IPeleador> Rivales
        {
            get
            {
                List<IPeleador> lista = new List<IPeleador>(rivales.Count);
                foreach (Rival rival in rivales)
                {
                    lista.Add(rival.peleador);
                }
                return lista;
            }
        }

        /// <summary>Avisa una sola vez, cuando todos los rivales cayeron.</summary>
        public event Action Terminada;
        /// <summary>Avisa cada cambio de fase.</summary>
        public event Action<EstadoEmboscada> EstadoCambio;

        private void Awake()
        {
            if (datos == null || datos.PrefabRival == null)
            {
                Debug.LogError("[Pelea] Faltan datos o prefab de rival en DirectorEmboscada de " + name, this);
                enabled = false;
                return;
            }
            maquina = new MaquinaEmboscada(datos.SegundosAparicion);
            maquina.EstadoCambio += AlCambiarEstado;
            maquina.Terminada += AlTerminar;
        }

        /// <summary>Empieza la emboscada sin esperar al jugador. Solo la primera vez.</summary>
        public void Empezar()
        {
            if (maquina != null)
            {
                maquina.Empezar();
            }
        }

        private void Update()
        {
            if (maquina == null)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ModoJugador modo = jugador != null ? jugador.Modo : ModoJugador.APie;
            bool cerca = activarAlAcercarse && jugador != null && jugador.Raiz != null && DistanciaPlana(jugador.Raiz.position) <= datos.RadioActivacion;
            maquina.Avanzar(Time.deltaTime, cerca, modo, ContarEnPie());
            AplicarControl();
            RetirarCuerpos();
            ActualizarObjetivo(modo);
        }

        private void OnDestroy()
        {
            foreach (Rival rival in rivales)
            {
                if (rival.objeto != null)
                {
                    Destroy(rival.objeto);
                }
            }
        }

        private float DistanciaPlana(Vector3 punto)
        {
            Vector3 delta = punto - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static bool EnPie(Rival rival)
        {
            return rival.objeto != null && !rival.peleador.Vida.EstaKO;
        }

        private int ContarEnPie()
        {
            int cuenta = 0;
            foreach (Rival rival in rivales)
            {
                if (EnPie(rival))
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private void AlCambiarEstado(EstadoEmboscada estado)
        {
            if (estado == EstadoEmboscada.Aparicion)
            {
                Aparecer();
            }
            EstadoCambio?.Invoke(estado);
        }

        private void AlTerminar()
        {
            QuitarObjetivo();
            Terminada?.Invoke();
        }

        private void Aparecer()
        {
            foreach (DatosEmboscada.PoseRival pose in datos.Poses)
            {
                Vector3 posicion = transform.TransformPoint(pose.posicion);
                Quaternion giro = transform.rotation * Quaternion.Euler(0f, pose.giroY, 0f);
                GameObject objeto = Instantiate(datos.PrefabRival, posicion, giro);
                IPeleador peleador = objeto.GetComponent<IPeleador>();
                if (peleador == null)
                {
                    Debug.LogError("[Pelea] El prefab de rival de " + name + " no tiene IPeleador.", this);
                    Destroy(objeto);
                    continue;
                }
                peleador.ActivarControl(false);
                rivales.Add(new Rival { objeto = objeto, peleador = peleador });
            }
        }

        // Los rivales solo tienen control mientras se pelea; se revisa cada frame para no depender del Start de cada rival.
        private void AplicarControl()
        {
            bool debenPelear = maquina.Estado == EstadoEmboscada.Peleando;
            foreach (Rival rival in rivales)
            {
                if (EnPie(rival) && rival.peleador.ControlActivo != debenPelear)
                {
                    rival.peleador.ActivarControl(debenPelear);
                }
            }
        }

        // Un cuerpo caído se retira tras unos segundos: estorbaba a los que quedaban en pie.
        private void RetirarCuerpos()
        {
            foreach (Rival rival in rivales)
            {
                if (rival.objeto == null)
                {
                    continue;
                }
                if (!rival.peleador.Vida.EstaKO)
                {
                    continue;
                }
                if (rival.tiempoKO < 0f)
                {
                    rival.tiempoKO = Time.time;
                }
                else if (Time.time - rival.tiempoKO >= datos.SegundosRetiroCuerpo)
                {
                    Destroy(rival.objeto);
                }
            }
        }

        private void ActualizarObjetivo(ModoJugador modo)
        {
            if (!datos.FijaObjetivos)
            {
                return;
            }
            EstadoEmboscada estado = maquina.Estado;
            if (estado == EstadoEmboscada.Esperando || estado == EstadoEmboscada.Superada)
            {
                return;
            }
            Objetivo deseado = modo == ModoJugador.Mecha ? new Objetivo(datos.TextoDerrotar, IdLugar.Ninguno) : ObjetivoDeSubida();
            FijarObjetivo(deseado);
        }

        // A pie: si el mecha ya está esperando, súbete; si no, llámalo desde el punto de llamado más cercano.
        private Objetivo ObjetivoDeSubida()
        {
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable.Modo == ModoJugador.Mecha && montable.PuedeSubir && HaLlegadoAUnPunto(montable.PuntoSubida))
                {
                    return new Objetivo(datos.TextoSubir, montable.PuntoSubida);
                }
            }
            IPuntoLlamadoMecha cercano = null;
            float mejor = float.MaxValue;
            foreach (IPuntoLlamadoMecha punto in Registro<IPuntoLlamadoMecha>.Todos)
            {
                if (!punto.Habilitado)
                {
                    continue;
                }
                float distancia = DistanciaPlana(punto.Llegada.position);
                if (distancia < mejor)
                {
                    mejor = distancia;
                    cercano = punto;
                }
            }
            return cercano != null
                ? new Objetivo(datos.TextoLlamar, cercano.Llegada.position)
                : new Objetivo(datos.TextoLlamar, IdLugar.Ninguno);
        }

        // El mecha "llegó" cuando está junto a la llegada de algún punto de llamado (en el taller no cuenta, aunque se pueda subir).
        private bool HaLlegadoAUnPunto(Vector3 puntoSubida)
        {
            foreach (IPuntoLlamadoMecha punto in Registro<IPuntoLlamadoMecha>.Todos)
            {
                Vector3 delta = puntoSubida - punto.Llegada.position;
                delta.y = 0f;
                if (delta.magnitude <= datos.MetrosMechaLlegado)
                {
                    return true;
                }
            }
            return false;
        }

        private void FijarObjetivo(Objetivo objetivo)
        {
            ServicioObjetivos servicio = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (servicio == null)
            {
                return;
            }
            bool igual = objetivoFijado && servicio.HayObjetivo && servicio.Actual.Texto == objetivo.Texto
                && servicio.Actual.TienePosicion == objetivo.TienePosicion && servicio.Actual.Posicion == objetivo.Posicion;
            if (igual)
            {
                return;
            }
            servicio.Fijar(objetivo);
            objetivoFijado = true;
        }

        private void QuitarObjetivo()
        {
            if (!objetivoFijado)
            {
                return;
            }
            objetivoFijado = false;
            ServicioObjetivos servicio = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (servicio != null)
            {
                servicio.Quitar();
            }
        }

        private void OnDrawGizmos()
        {
            if (datos == null)
            {
                return;
            }
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0.001f, 1f));
            Gizmos.DrawWireSphere(Vector3.zero, datos.RadioActivacion);
            Gizmos.matrix = Matrix4x4.identity;
            foreach (DatosEmboscada.PoseRival pose in datos.Poses)
            {
                Vector3 posicion = transform.TransformPoint(pose.posicion);
                Quaternion giro = transform.rotation * Quaternion.Euler(0f, pose.giroY, 0f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(posicion + Vector3.up, 0.6f);
                Gizmos.DrawLine(posicion, posicion + giro * Vector3.forward * 2f);
            }
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        }
    }
}
