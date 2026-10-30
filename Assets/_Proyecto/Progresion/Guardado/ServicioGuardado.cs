using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Guardado
{
    /// <summary>
    /// Guarda y carga la partida (T-111, D-tony-50). Junta todo lo que está en <c>Registro&lt;IGuardable&gt;</c> sin conocer a
    /// nadie. No guarda ni carga solo: lo decide quien lo llame (menú y dormir, T-F6). Va en ServiciosProgresion.prefab.
    /// Las piezas leídas cuyo dueño aún no está en la escena quedan pendientes: se entregan cuando se registra y
    /// <see cref="Guardar()"/> las vuelve a escribir tal cual, así guardar en una escena sin mecha no borra el mecha (T-164).
    /// Un registrado siempre gana sobre una pendiente de su misma clave.
    /// T-412 (D-tony-244): 3 ranuras manuales y el AUTOGUARDADO (<see cref="RanuraAuto"/>), cada una con su resumen; el AUTOGUARDADO es
    /// <see cref="RutaArchivo"/>. La posición del jugador (clave <c>jugador</c>) solo se entrega al cargar una ranura, no en un cambio de
    /// escena; con un cambio de escena se conserva tal cual.
    /// </summary>
    public class ServicioGuardado : MonoBehaviour
    {
        public const int RanuraAuto = 0;
        public const int PrimeraRanura = 1;
        public const int UltimaRanura = 3;
        public const string ClaveJugador = "jugador";
        private const string NombreAuto = "partida_auto";
        private const string NombreRanura = "partida_";
        private const string Extension = ".json";
        private const string ExtensionTemporal = ".tmp";
        private const string FormatoFechaLocal = "yyyy-MM-ddTHH:mm:ss";

        // Claves que solo se entregan al cargar una ranura (la posición no viaja entre escenas).
        private static readonly string[] ClavesSoloRanura = { ClaveJugador };

        private string ruta;
        private readonly Dictionary<string, string> pendientes = new Dictionary<string, string>();
        private readonly Dictionary<string, string> retenidas = new Dictionary<string, string>();
        private readonly HashSet<string> avisadas = new HashSet<string>();

        /// <summary>
        /// Si está puesta, gana sobre la ruta por defecto en todas las instancias (pruebas que cruzan escenas sin tocar la partida
        /// real). Una ruta puesta con <see cref="FijarRuta"/> en esa instancia gana sobre esta. Se limpia al entrar a Play.
        /// </summary>
        public static string RutaForzada { get; set; }

        /// <summary>Se avisa al escribir una ranura con <see cref="Guardar(int)"/> (el aviso "GUARDADO" de la UI la escucha).</summary>
        public static event Action<int> RanuraEscrita;

        /// <summary>
        /// Dónde está el AUTOGUARDADO. Por defecto persistentDataPath/partida_auto.json; con <see cref="RutaForzada"/> o
        /// <see cref="FijarRuta"/> es exactamente esa ruta.
        /// </summary>
        public string RutaArchivo => ruta ?? (!string.IsNullOrEmpty(RutaForzada)
            ? RutaForzada
            : Path.Combine(Application.persistentDataPath, NombreAuto + Extension));

        /// <summary>El servicio de guardado activo de la escena, o null (para otras áreas sin Find).</summary>
        public static ServicioGuardado Actual => Registro<ServicioGuardado>.Actual;

        /// <summary>La hora local de ahora (las pruebas la cambian para escribir ranuras con fechas distintas).</summary>
        public Func<DateTime> Reloj { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            RutaForzada = null;
            RanuraEscrita = null;
        }

        public bool HayPartida => File.Exists(RutaArchivo);

        public event Action Guardada;
        public event Action Cargada;

        /// <summary>Cambia la ruta del archivo (pruebas).</summary>
        public void FijarRuta(string nuevaRuta)
        {
            ruta = nuevaRuta;
        }

        /// <summary>True si es 0 (AUTO) o de 1 a 3.</summary>
        public static bool EsRanura(int ranura) => ranura >= RanuraAuto && ranura <= UltimaRanura;

        /// <summary>
        /// El archivo de la ranura. AUTO es <see cref="RutaArchivo"/>; las manuales van en persistentDataPath/partida_N.json o, con una ruta
        /// forzada o fijada, a su lado (&lt;ruta sin extensión&gt;_N.json), para que las pruebas no toquen las partidas reales.
        /// </summary>
        public string RutaRanura(int ranura)
        {
            if (ranura == RanuraAuto)
            {
                return RutaArchivo;
            }
            string forzada = ruta ?? RutaForzada;
            if (string.IsNullOrEmpty(forzada))
            {
                return Path.Combine(Application.persistentDataPath, NombreRanura + ranura + Extension);
            }
            string carpeta = Path.GetDirectoryName(forzada) ?? string.Empty;
            return Path.Combine(carpeta, Path.GetFileNameWithoutExtension(forzada) + "_" + ranura + Extension);
        }

        private void OnEnable()
        {
            Registro<IGuardable>.Agregado += AlRegistrarse;
            Registro<ServicioGuardado>.Agregar(this);
        }

        private void Start()
        {
            // Solo tras un cambio de escena o un FlujoPartida se lee la partida: dar Play directo no lee la del humano.
            bool transicion = CambioEscena.ConsumirTransicion();
            if (FlujoPartida.ConsumirPendiente(out AccionPartida accion, out int ranura))
            {
                if (accion == AccionPartida.Cargar)
                {
                    Cargar(ranura);
                }
                return;
            }
#if DEMO_FINAL
            // La demo empieza nueva salvo que haya una carga pendiente.
            transicion = false;
#endif
            if (transicion) Cargar();
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Agregado -= AlRegistrarse;
            Registro<ServicioGuardado>.Quitar(this);
        }

        /// <summary>Escribe el AUTOGUARDADO (lo usa el cambio de escena). False (con error en consola) si no se pudo; el archivo anterior queda intacto.</summary>
        public bool Guardar() => Escribir(RutaArchivo);

        /// <summary>Escribe la ranura (0 = AUTO, 1 a 3) y avisa <see cref="RanuraEscrita"/>. False si la ranura no existe o no se pudo.</summary>
        public bool Guardar(int ranura)
        {
            if (!EsRanura(ranura))
            {
                Debug.LogWarning("[Progresion] No existe la ranura " + ranura + ".");
                return false;
            }
            if (!Escribir(RutaRanura(ranura)))
            {
                return false;
            }
            RanuraEscrita?.Invoke(ranura);
            return true;
        }

        /// <summary>Lee el AUTOGUARDADO y se lo devuelve a cada registrado, salvo la posición del jugador. False (con warning) si no existe o no se entiende.</summary>
        public bool Cargar() => Leer(RutaArchivo, false);

        /// <summary>Lee la ranura (0 = AUTO, 1 a 3) con todo, también la posición del jugador. False (con warning) si no existe o no se entiende.</summary>
        public bool Cargar(int ranura)
        {
            if (!EsRanura(ranura))
            {
                Debug.LogWarning("[Progresion] No existe la ranura " + ranura + ".");
                return false;
            }
            return Leer(RutaRanura(ranura), true);
        }

        /// <summary>
        /// Fecha, zona, dinero y objetivo de la ranura, sin cargarla. Sin archivo, roto o de otra versión sale vacía; nunca lanza
        /// excepciones y avisa una sola vez por archivo roto.
        /// </summary>
        public ResumenRanura ResumenDe(int ranura)
        {
            if (!EsRanura(ranura))
            {
                return ResumenRanura.SinPartida(ranura);
            }
            string archivo = RutaRanura(ranura);
            string json;
            try
            {
                if (!File.Exists(archivo))
                {
                    return ResumenRanura.SinPartida(ranura);
                }
                json = File.ReadAllText(archivo);
            }
            catch (Exception e)
            {
                AvisarUnaVez(archivo, "no se pudo leer (" + e.Message + ")");
                return ResumenRanura.SinPartida(ranura);
            }
            if (!ArchivoPartida.TryLeerResumen(json, out ResumenPartida resumen, out string fechaIso)
                || !DateTime.TryParse(fechaIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime fecha))
            {
                AvisarUnaVez(archivo, "no se entiende");
                return ResumenRanura.SinPartida(ranura);
            }
            DateTime utc = fecha.ToUniversalTime();
            DateTime local = utc.ToLocalTime();
            if (resumen != null && DateTime.TryParseExact(resumen.fecha, FormatoFechaLocal, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime guardadaLocal))
            {
                local = guardadaLocal;
            }
            return resumen != null
                ? new ResumenRanura(ranura, local, utc, resumen.zona, resumen.dinero, resumen.objetivo)
                : new ResumenRanura(ranura, local, utc, string.Empty, 0, string.Empty);
        }

        /// <summary>La ranura escrita más recientemente (AUTO incluida), o -1 si no hay ninguna.</summary>
        public int RanuraMasReciente()
        {
            int mejor = -1;
            DateTime fechaMejor = DateTime.MinValue;
            for (int r = RanuraAuto; r <= UltimaRanura; r++)
            {
                ResumenRanura resumen = ResumenDe(r);
                if (!resumen.Vacia && (mejor < 0 || resumen.FechaUtc > fechaMejor))
                {
                    mejor = r;
                    fechaMejor = resumen.FechaUtc;
                }
            }
            return mejor;
        }

        /// <summary>Borra el archivo del AUTOGUARDADO.</summary>
        public void Borrar()
        {
            try
            {
                if (File.Exists(RutaArchivo)) File.Delete(RutaArchivo);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Progresion] No se pudo borrar la partida: " + e.Message);
            }
            pendientes.Clear();
            retenidas.Clear();
        }

        private void AvisarUnaVez(string archivo, string motivo)
        {
            if (avisadas.Add(archivo))
            {
                Debug.LogWarning("[Progresion] La partida " + Path.GetFileName(archivo) + " " + motivo + "; sale VACÍA.");
            }
        }

        private bool Escribir(string destino)
        {
            var piezas = new List<PiezaGuardada>();
            var claves = new HashSet<string>();
            foreach (IGuardable guardable in Registro<IGuardable>.Todos)
            {
                string clave = guardable.ClaveGuardado;
                if (!claves.Add(clave))
                {
                    Debug.LogWarning("[Progresion] Clave de guardado repetida '" + clave + "'; solo se guarda la primera.");
                    continue;
                }
                piezas.Add(new PiezaGuardada { clave = clave, datos = guardable.Guardar() ?? string.Empty });
            }
            // Lo leído cuyo dueño no está en esta escena se escribe igual (T-164): no se pierde al guardar.
            foreach (KeyValuePair<string, string> pendiente in pendientes)
            {
                if (claves.Add(pendiente.Key)) piezas.Add(new PiezaGuardada { clave = pendiente.Key, datos = pendiente.Value });
            }
            foreach (KeyValuePair<string, string> retenida in retenidas)
            {
                if (claves.Add(retenida.Key)) piezas.Add(new PiezaGuardada { clave = retenida.Key, datos = retenida.Value });
            }
            DateTime ahora = Reloj != null ? Reloj() : DateTime.Now;
            string json = ArchivoPartida.Escribir(piezas, ahora.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), CrearResumen(ahora));
            string temporal = destino + ExtensionTemporal;
            try
            {
                string carpeta = Path.GetDirectoryName(destino);
                if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
                File.WriteAllText(temporal, json);
                if (File.Exists(destino)) File.Replace(temporal, destino, null);
                else File.Move(temporal, destino);
            }
            catch (Exception e)
            {
                Debug.LogError("[Progresion] No se pudo guardar la partida: " + e.Message);
                try
                {
                    if (File.Exists(temporal)) File.Delete(temporal);
                }
                catch (Exception)
                {
                    // Sin más: el error ya se avisó.
                }
                return false;
            }
            avisadas.Remove(destino);
            Guardada?.Invoke();
            return true;
        }

        // Fecha local, zona donde está el jugador, dinero y objetivo actual.
        private static ResumenPartida CrearResumen(DateTime ahora)
        {
            var resumen = new ResumenPartida { fecha = ahora.ToString(FormatoFechaLocal, CultureInfo.InvariantCulture), zona = string.Empty, objetivo = string.Empty };
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (jugador != null && jugador.Raiz != null && territorio != null)
            {
                resumen.zona = territorio.Nombre(territorio.ZonaEn(jugador.Raiz.position)) ?? string.Empty;
            }
            ICartera cartera = Registro<ICartera>.Actual;
            resumen.dinero = cartera != null ? cartera.Dinero : 0;
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            if (objetivos != null && objetivos.HayObjetivo)
            {
                resumen.objetivo = objetivos.Actual.Texto ?? string.Empty;
            }
            return resumen;
        }

        private bool Leer(string origen, bool conPosicion)
        {
            if (!File.Exists(origen))
            {
                Debug.LogWarning("[Progresion] No hay partida guardada en " + origen);
                return false;
            }
            string json;
            try
            {
                json = File.ReadAllText(origen);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Progresion] No se pudo leer la partida: " + e.Message);
                return false;
            }
            if (!ArchivoPartida.TryLeer(json, out List<PiezaGuardada> piezas, out _))
            {
                Debug.LogWarning("[Progresion] La partida guardada no se entiende; no se cambia nada.");
                return false;
            }
            pendientes.Clear();
            retenidas.Clear();
            foreach (PiezaGuardada pieza in piezas)
            {
                if (!conPosicion && Array.IndexOf(ClavesSoloRanura, pieza.clave) >= 0)
                {
                    retenidas[pieza.clave] = pieza.datos;
                }
                else
                {
                    pendientes[pieza.clave] = pieza.datos;
                }
            }
            // Copia: Cargar de un guardable podría registrar otros.
            foreach (IGuardable guardable in new List<IGuardable>(Registro<IGuardable>.Todos))
            {
                Entregar(guardable);
            }
            Cargada?.Invoke();
            return true;
        }

        private void AlRegistrarse(IGuardable guardable)
        {
            if (guardable != null) Entregar(guardable);
        }

        private void Entregar(IGuardable guardable)
        {
            if (pendientes.TryGetValue(guardable.ClaveGuardado, out string datos))
            {
                pendientes.Remove(guardable.ClaveGuardado);
                guardable.Cargar(datos);
            }
        }
    }
}
