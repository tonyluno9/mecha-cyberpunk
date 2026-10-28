using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using UnityEngine;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>
    /// Los encargos de la calle (T-186): por cada <see cref="IPuntoMision"/> de tipo Encargo que tiene una misión de barrio que ofrecer,
    /// un <see cref="IInteractuable"/> "E · AYUDAR: TÍTULO" que abre la <see cref="PantallaMisiones"/> con esa misión. Si la misión no está
    /// disponible dice el motivo (sin E). Llama <c>Activar(true)</c> al punto mientras tiene algo que ofrecer. Va en JugadorAPie.prefab junto
    /// al teléfono; mira los puntos cada <c>segundosRevision</c> para no recorrer el catálogo en cada cuadro.
    /// </summary>
    public class EncargosCalle : MonoBehaviour
    {
        [SerializeField] private ConfigPantallaMisiones config;
        [Tooltip("Cada cuántos segundos se buscan los puntos nuevos y se actualiza lo que ofrecen (s).")]
        [Min(0.05f)] [SerializeField] private float segundosRevision = 0.5f;

        private sealed class Encargo : IInteractuable
        {
            private readonly EncargosCalle dueno;
            public IPuntoMision Punto { get; }
            public DatosMision Mision { get; set; }
            public MotivoMision Motivo { get; set; }
            public bool Registrado { get; set; }

            public Encargo(EncargosCalle dueno, IPuntoMision punto)
            {
                this.dueno = dueno;
                Punto = punto;
            }

            public Vector3 Posicion => Punto.Pose.position;
            public string Texto => dueno.TextoDe(this);
            public bool PuedeUsar => dueno.PuedeUsarEncargo(this);
            public void Usar() => dueno.UsarEncargo(this);
        }

        private readonly Dictionary<IPuntoMision, Encargo> encargos = new Dictionary<IPuntoMision, Encargo>();
        private readonly List<IPuntoMision> quitados = new List<IPuntoMision>();
        private float restante;

        /// <summary>Cuántos encargos tienen algo que ofrecer ahora.</summary>
        public int CantidadOfrecidos
        {
            get
            {
                int cuenta = 0;
                foreach (Encargo e in encargos.Values)
                {
                    if (e.Registrado) cuenta++;
                }
                return cuenta;
            }
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[UI] Falta la config en EncargosCalle de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            restante = 0f;
        }

        private void OnDisable()
        {
            foreach (Encargo e in encargos.Values)
            {
                Quitar(e);
            }
            encargos.Clear();
        }

        private void Update()
        {
            restante -= Time.deltaTime;
            if (restante > 0f)
            {
                return;
            }
            restante = segundosRevision;
            Revisar();
        }

        private void Revisar()
        {
            ServicioMisiones servicio = Registro<ServicioMisiones>.Actual;
            quitados.Clear();
            foreach (IPuntoMision punto in encargos.Keys)
            {
                if (!Existe(punto))
                {
                    quitados.Add(punto);
                }
            }
            foreach (IPuntoMision punto in quitados)
            {
                Quitar(encargos[punto]);
                encargos.Remove(punto);
            }
            if (servicio == null)
            {
                return;
            }
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto == null || punto.Tipo != TipoPuntoMision.Encargo)
                {
                    continue;
                }
                if (!encargos.TryGetValue(punto, out Encargo encargo))
                {
                    encargo = new Encargo(this, punto);
                    encargos[punto] = encargo;
                }
                Actualizar(encargo, servicio);
            }
        }

        // Elige la misión de barrio del punto: la primera disponible o, si ninguna lo está, la primera.
        private void Actualizar(Encargo encargo, ServicioMisiones servicio)
        {
            DatosMision elegida = null;
            MotivoMision motivo = MotivoMision.FaltanPuntos;
            foreach (OfertaMision oferta in servicio.Ofertas(OrigenMision.Calle, encargo.Punto.Id))
            {
                if (oferta.Datos.Tipo != TipoMision.Barrio)
                {
                    continue;
                }
                if (elegida == null || (motivo != MotivoMision.Disponible && oferta.Motivo == MotivoMision.Disponible))
                {
                    elegida = oferta.Datos;
                    motivo = oferta.Motivo;
                }
            }
            encargo.Mision = elegida;
            encargo.Motivo = motivo;
            // Con otra misión en curso el vecino calla: sus puntos los gobierna el servicio y no debe competir con la E de los pasos.
            bool ofrece = elegida != null && motivo != MotivoMision.OtraActiva;
            if (ofrece && !encargo.Registrado)
            {
                Registro<IInteractuable>.Agregar(encargo);
                encargo.Registrado = true;
                encargo.Punto.Activar(true);
            }
            else if (!ofrece && encargo.Registrado)
            {
                Quitar(encargo, elegida == null);
            }
        }

        private static bool Existe(IPuntoMision buscado)
        {
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto == buscado)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Quitar(Encargo encargo, bool apagarPunto = true)
        {
            if (encargo.Registrado)
            {
                Registro<IInteractuable>.Quitar(encargo);
                encargo.Registrado = false;
                if (apagarPunto)
                {
                    encargo.Punto.Activar(false);
                }
            }
        }

        private string TextoDe(Encargo encargo)
        {
            if (encargo.Mision == null)
            {
                return string.Empty;
            }
            if (encargo.Motivo == MotivoMision.Disponible)
            {
                return string.Format(config.FormatoAyudar, encargo.Mision.Titulo);
            }
            ServicioReputacion reputacion = Registro<ServicioReputacion>.Actual;
            return FormatoMisiones.Motivo(encargo.Motivo, encargo.Mision, reputacion != null ? reputacion.Datos : null, config);
        }

        private bool PuedeUsarEncargo(Encargo encargo)
        {
            PantallaMisiones pantalla = Registro<PantallaMisiones>.Actual;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            return encargo.Mision != null && encargo.Motivo == MotivoMision.Disponible && pantalla != null && !pantalla.Visible
                && jugador != null && jugador.Modo == ModoJugador.APie;
        }

        private void UsarEncargo(Encargo encargo)
        {
            if (!PuedeUsarEncargo(encargo))
            {
                return;
            }
            Registro<PantallaMisiones>.Actual.Mostrar(config.TituloEncargo, OrigenMision.Calle, encargo.Punto.Id, null, false);
        }
    }
}
