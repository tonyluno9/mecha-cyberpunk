using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// La misión en escena (T-184, D-tony-147): una a la vez. Fija el objetivo de cada paso, enciende los puntos, cumple <c>IrA</c> por
    /// distancia y <c>Recoger</c>/<c>Entregar</c> con E, cobra o castiga al terminar y guarda qué se hizo y cuándo (clave <c>misiones</c>).
    /// La misión en curso no se guarda: al cargar queda cancelada sin castigo. Va en ServiciosProgresion.prefab.
    /// </summary>
    public class ServicioMisiones : MonoBehaviour, IMisiones, IGuardable
    {
        public const string Clave = "misiones";
        private const string PrefijoRecoger = "RECOGER: ";
        private const string PrefijoEntregar = "ENTREGAR: ";
        private const string AvisoFavorLleno = "Favor perdido: el bolsillo estaba lleno.";

        [Tooltip("Lista de todas las misiones.")]
        [SerializeField] private CatalogoMisiones catalogo;

        [Serializable]
        private class EntradaGuardada
        {
            public string id;
            public int veces;
            public int ultimoDia;
        }

        [Serializable]
        private class DatosGuardados
        {
            public List<EntradaGuardada> misiones = new List<EntradaGuardada>();
        }

        // El E de un paso Recoger o Entregar. Se registra solo mientras dura el paso.
        private sealed class InteractuablePaso : IInteractuable
        {
            private readonly ServicioMisiones dueno;
            public Vector3 Posicion { get; }
            public string Texto { get; }

            public InteractuablePaso(ServicioMisiones dueno, Vector3 posicion, string texto)
            {
                this.dueno = dueno;
                Posicion = posicion;
                Texto = texto;
            }

            public bool PuedeUsar
            {
                get
                {
                    IJugador jugador = Registro<IJugador>.Actual;
                    return jugador != null && jugador.Modo == ModoJugador.APie;
                }
            }

            public void Usar()
            {
                if (PuedeUsar) dueno.CumplirPasoActual();
            }
        }

        private readonly MaquinaMision maquina = new MaquinaMision();
        private readonly ModeloHistorialMisiones historial = new ModeloHistorialMisiones();
        private readonly HashSet<string> avisados = new HashSet<string>();
        private IPuntoMision puntoActual;
        private InteractuablePaso interactuable;
        private bool suscrito;
        private Action<TipoMision, bool> terminadaContrato;
        private ResumenMision ultimoResumen;

        public CatalogoMisiones Catalogo => catalogo;
        public string ClaveGuardado => Clave;
        public bool HayMision => maquina.Activa;
        public TipoMision Tipo => maquina.Datos != null ? maquina.Datos.Tipo : TipoMision.Barrio;
        public string Titulo => maquina.Activa ? maquina.Datos.Titulo : "";

        /// <summary>La misión en curso, o null.</summary>
        public DatosMision Actual => maquina.Activa ? maquina.Datos : null;

        /// <summary>El paso en curso, o null.</summary>
        public PasoMision PasoActual => maquina.PasoActual;

        /// <summary>Índice del paso en curso (desde 0).</summary>
        public int IndicePaso => maquina.IndicePaso;

        /// <summary>Segundos que quedan para cumplirla; 0 si no hay límite o no hay misión.</summary>
        public float SegundosRestantes => maquina.Restante;

        /// <summary>Cuántas veces se cumplió una misión.</summary>
        public int VecesCumplida(string id) => historial.Veces(id);

        /// <summary>Último día en que se cumplió, o <see cref="ContextoMision.Nunca"/>.</summary>
        public int UltimoDiaCumplida(string id) => historial.UltimoDia(id);

        /// <summary>Resumen de la última misión que terminó.</summary>
        public ResumenMision UltimoResumen => ultimoResumen;

        public event Action Cambio;
        public event Action<DatosMision> Aceptada;
        public event Action PasoCambio;
        public event Action<DatosMision, bool, ResumenMision> Terminada;

        event Action<TipoMision, bool> IMisiones.Terminada
        {
            add => terminadaContrato += value;
            remove => terminadaContrato -= value;
        }

        private void Awake()
        {
            if (catalogo == null)
            {
                Debug.LogError("[Progresion] ServicioMisiones en " + name + " no tiene CatalogoMisiones.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IMisiones>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioMisiones>.Agregar(this);
            maquina.PasoCambio += AlCambiarPaso;
            maquina.Terminada += AlTerminar;
            suscrito = true;
        }

        private void OnDisable()
        {
            if (suscrito)
            {
                maquina.PasoCambio -= AlCambiarPaso;
                maquina.Terminada -= AlTerminar;
                suscrito = false;
            }
            Registro<IMisiones>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioMisiones>.Quitar(this);
            LimpiarPaso();
        }

        private void Update()
        {
            if (!maquina.Activa) return;
            maquina.Avanzar(Time.deltaTime);
            if (!maquina.Activa) return;
            PasoMision paso = maquina.PasoActual;
            if (paso.Tipo != TipoPaso.IrA || puntoActual == null) return;
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null) return;
            Vector3 d = jugador.Raiz.position - puntoActual.Pose.position;
            d.y = 0f;
            if (d.magnitude <= paso.Radio) CumplirPasoActual();
        }

        // ---------- Ofertas ----------

        /// <summary>
        /// Las misiones de ese origen con la razón por la que se ofrecen o no. Con <paramref name="idEncargo"/> solo las de ese punto
        /// Encargo (para Calle).
        /// </summary>
        public List<OfertaMision> Ofertas(OrigenMision origen, string idEncargo = null)
        {
            var lista = new List<OfertaMision>();
            if (catalogo == null) return lista;
            foreach (DatosMision m in catalogo.Misiones)
            {
                if (m == null || m.Origen != origen) continue;
                if (!string.IsNullOrEmpty(idEncargo) && m.IdEncargo != idEncargo) continue;
                lista.Add(new OfertaMision(m, Motivo(m)));
            }
            return lista;
        }

        /// <summary>Por qué se ofrece o no una misión ahora.</summary>
        public MotivoMision Motivo(DatosMision datos)
        {
            if (datos == null) return MotivoMision.FaltanPuntos;
            return ReglaMisiones.Decidir(datos, ConstruirContexto(datos));
        }

        private ContextoMision ConstruirContexto(DatosMision datos)
        {
            IHistoria historia = Registro<IHistoria>.Actual;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            IReputacion reputacion = Registro<IReputacion>.Actual;
            IProgresoLigas ligas = Registro<IProgresoLigas>.Actual;
            ServicioFavores favores = Registro<ServicioFavores>.Actual;
            return new ContextoMision
            {
                Etapa = historia != null ? historia.Etapa : EtapaPrologo.Ninguna,
                HayReloj = reloj != null,
                Franja = reloj != null ? reloj.Franja : FranjaDia.Dia,
                Dia = reloj != null ? reloj.Dia : 0,
                Liga = ligas != null ? ligas.LigaActual : 1,
                NivelBarrio = reputacion != null ? reputacion.Nivel(TipoReputacion.Barrio) : 0,
                NivelCorporaciones = reputacion != null ? reputacion.Nivel(TipoReputacion.Corporaciones) : 0,
                CantidadFavor = favores != null ? (Func<TipoFavor, int>)favores.Cantidad : null,
                OtraActiva = maquina.Activa,
                UltimoDiaCumplida = historial.UltimoDia(datos.Id),
                ExistenPuntos = ExistenPuntos(datos)
            };
        }

        private bool ExistenPuntos(DatosMision datos)
        {
            if (datos.Origen == OrigenMision.Calle && !string.IsNullOrEmpty(datos.IdEncargo) && BuscarPunto(datos.IdEncargo) == null)
            {
                Avisar("punto:" + datos.IdEncargo, "falta el punto " + datos.IdEncargo + " (misión " + datos.Id + ")");
                return false;
            }
            foreach (PasoMision paso in datos.Pasos)
            {
                if (string.IsNullOrEmpty(paso.IdPunto)) continue;
                if (BuscarPunto(paso.IdPunto) == null)
                {
                    Avisar("punto:" + paso.IdPunto, "falta el punto " + paso.IdPunto + " (misión " + datos.Id + ")");
                    return false;
                }
            }
            return true;
        }

        private static IPuntoMision BuscarPunto(string id)
        {
            foreach (IPuntoMision p in Registro<IPuntoMision>.Todos)
            {
                if (p != null && p.Id == id) return p;
            }
            return null;
        }

        // ---------- Aceptar, abandonar y pasos ----------

        /// <summary>Acepta la misión si está <see cref="MotivoMision.Disponible"/>. Gasta el favor requerido. False si no se pudo.</summary>
        public bool Aceptar(DatosMision datos)
        {
            if (datos == null || Motivo(datos) != MotivoMision.Disponible) return false;
            if (datos.ExigeFavor)
            {
                ServicioFavores favores = Registro<ServicioFavores>.Actual;
                if (favores == null || !favores.Gastar(datos.FavorRequerido)) return false;
            }
            if (!maquina.Empezar(datos)) return false;
            Aceptada?.Invoke(datos);
            EmpezarPaso();
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Deja la misión en curso: castiga un poco. Sin misión no hace nada.</summary>
        public void Abandonar() => maquina.Fallar(MotivoFalloMision.Abandono);

        /// <summary>Falla la misión en curso por ese motivo (por ejemplo KO en una pelea). Sin misión no hace nada.</summary>
        public void FallarExterno(MotivoFalloMision motivo) => maquina.Fallar(motivo == MotivoFalloMision.Ninguno ? MotivoFalloMision.Abandono : motivo);

        /// <summary>Cumple el paso actual si es de ese tipo (para Pelear y Escapar, que llegan en T-185). True si lo cumplió.</summary>
        public bool CumplirPasoExterno(TipoPaso tipo)
        {
            if (!maquina.Activa || maquina.PasoActual.Tipo != tipo) return false;
            CumplirPasoActual();
            return true;
        }

        private void CumplirPasoActual()
        {
            if (!maquina.Activa) return;
            float calor = maquina.PasoActual.Calor;
            if (calor > 0f)
            {
                ICalor servicioCalor = Registro<ICalor>.Actual;
                if (servicioCalor != null) servicioCalor.Sumar(calor, MotivoCalor.Mision);
                else Avisar("calor", "no hay ICalor; el calor de la misión no se suma");
            }
            LimpiarPaso();
            maquina.Cumplir();
        }

        private void AlCambiarPaso()
        {
            EmpezarPaso();
            PasoCambio?.Invoke();
            Cambio?.Invoke();
        }

        private void EmpezarPaso()
        {
            PasoMision paso = maquina.PasoActual;
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            puntoActual = string.IsNullOrEmpty(paso.IdPunto) ? null : BuscarPunto(paso.IdPunto);
            if (puntoActual != null)
            {
                Pose pose = puntoActual.Pose;
                if (objetivos != null) objetivos.Fijar(new Objetivo(paso.Texto, pose.position));
                puntoActual.Activar(true);
                if (paso.Tipo == TipoPaso.Recoger || paso.Tipo == TipoPaso.Entregar)
                {
                    string prefijo = paso.Tipo == TipoPaso.Recoger ? PrefijoRecoger : PrefijoEntregar;
                    interactuable = new InteractuablePaso(this, pose.position, prefijo + paso.Texto);
                    Registro<IInteractuable>.Agregar(interactuable);
                }
            }
            else if (objetivos != null)
            {
                objetivos.Quitar();
            }
        }

        // Quita lo del paso: interactuable y punto apagado.
        private void LimpiarPaso()
        {
            if (interactuable != null)
            {
                Registro<IInteractuable>.Quitar(interactuable);
                interactuable = null;
            }
            if (puntoActual != null)
            {
                puntoActual.Activar(false);
                puntoActual = null;
            }
        }

        private void AlTerminar(bool exito, MotivoFalloMision motivo)
        {
            DatosMision datos = maquina.Datos;
            LimpiarPaso();
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
            ResumenMision resumen = exito ? Cobrar(datos) : Castigar(datos, motivo);
            ultimoResumen = resumen;
            Terminada?.Invoke(datos, exito, resumen);
            terminadaContrato?.Invoke(datos.Tipo, exito);
            Cambio?.Invoke();
        }

        private ResumenMision Cobrar(DatosMision datos)
        {
            RecompensaMision r = ReglaMisiones.Recompensa(datos);
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            historial.Anotar(datos.Id, reloj != null ? reloj.Dia : 0);

            if (r.Pago > 0)
            {
                ICartera cartera = Registro<ICartera>.Actual;
                if (cartera != null) cartera.Abonar(r.Pago, MotivoDinero.Mision);
                else Avisar("cartera", "no hay ICartera; el pago no se abona");
            }

            int chatarra = 0;
            if (r.Chatarra != null && r.ChatarraCantidad > 0)
            {
                ServicioInventario inventario = Registro<ServicioInventario>.Actual;
                if (inventario != null) chatarra = inventario.Agregar(r.Chatarra, r.ChatarraCantidad);
                else Avisar("inventario", "no hay ServicioInventario; la chatarra no se entrega");
            }

            int dBarrio = Sumar(TipoReputacion.Barrio, r.DeltaBarrio);
            int dCorp = Sumar(TipoReputacion.Corporaciones, r.DeltaCorporaciones);

            DatosFavor favor = null;
            bool perdido = false;
            if (r.Favor != null)
            {
                ServicioFavores favores = Registro<ServicioFavores>.Actual;
                if (favores != null && favores.Dar(r.Favor.Tipo)) favor = r.Favor;
                else perdido = favores != null;
            }
            return new ResumenMision(r.Pago, chatarra > 0 ? r.Chatarra : null, chatarra, dBarrio, dCorp, favor, perdido,
                MotivoFalloMision.Ninguno, perdido ? AvisoFavorLleno : "");
        }

        private ResumenMision Castigar(DatosMision datos, MotivoFalloMision motivo)
        {
            PenalizacionMision p = ReglaMisiones.Penalizacion(datos);
            int dBarrio = Sumar(TipoReputacion.Barrio, p.Barrio);
            int dCorp = Sumar(TipoReputacion.Corporaciones, p.Corporaciones);
            return new ResumenMision(0, null, 0, dBarrio, dCorp, null, false, motivo, "");
        }

        // Suma a la reputación y devuelve lo que de verdad cambió (puede ser menos por los topes).
        private int Sumar(TipoReputacion tipo, int cantidad)
        {
            if (cantidad == 0) return 0;
            ServicioReputacion reputacion = Registro<ServicioReputacion>.Actual;
            if (reputacion == null)
            {
                Avisar("reputacion", "no hay ServicioReputacion; la reputación no se mueve");
                return 0;
            }
            int antes = reputacion.Valor(tipo);
            reputacion.Sumar(tipo, cantidad);
            return reputacion.Valor(tipo) - antes;
        }

        private void Avisar(string clave, string mensaje)
        {
            if (avisados.Add(clave)) Debug.LogWarning("[Progresion] ServicioMisiones: " + mensaje + ".", this);
        }

        // ---------- Guardado ----------

        public string Guardar()
        {
            var guardado = new DatosGuardados();
            foreach (string id in historial.Ids)
            {
                guardado.misiones.Add(new EntradaGuardada { id = id, veces = historial.Veces(id), ultimoDia = historial.UltimoDia(id) });
            }
            return JsonUtility.ToJson(guardado);
        }

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || leidos.misiones == null)
            {
                Debug.LogWarning("[Progresion] Las misiones no entendieron lo guardado; no se cambia nada.");
                return;
            }
            historial.Limpiar();
            foreach (EntradaGuardada e in leidos.misiones)
            {
                if (e == null || string.IsNullOrEmpty(e.id))
                {
                    Debug.LogWarning("[Progresion] Las misiones guardadas traen una entrada sin id; se ignora.");
                    continue;
                }
                historial.Poner(e.id, e.veces, e.ultimoDia);
            }
            CancelarSinCastigo();
            Cambio?.Invoke();
        }

        // La misión en curso no se guarda: al cargar se cancela sin castigo y sin avisar Terminada.
        private void CancelarSinCastigo()
        {
            if (!maquina.Activa) return;
            maquina.Terminada -= AlTerminar;
            maquina.Fallar(MotivoFalloMision.Abandono);
            maquina.Terminada += AlTerminar;
            LimpiarPaso();
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
        }
    }
}
