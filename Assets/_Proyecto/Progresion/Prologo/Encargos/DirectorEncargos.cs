using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Encargos
{
    /// <summary>
    /// Los encargos repetibles del juego libre (T-426, D-tony-294): COBRAR CUOTA (3 locales de tu zona; a veces uno se resiste), DEFENDER ZONA (una oleada de
    /// 4 rivales en un punto de SUBURBIOS) y ROBAR CARRO (un carro marcado, a CANEPA). Se piden con E en el TABLERO junto a tu casa, solo con la historia
    /// en el juego libre. Cada uno pone el objetivo (y con eso la marca del minimapa y del mapa de T-408/T-409), paga en Biits y respeto y se enfría
    /// antes de repetirse. Guarda sus enfriamientos y contadores con la clave <c>encargos</c>. Las reglas puras están en <see cref="ReglaEncargos"/>.
    /// </summary>
    public class DirectorEncargos : MonoBehaviour, IGuardable
    {
        public const string Clave = "encargos";
        public const string TextoTablero = "TABLERO DE ENCARGOS";
        public const string TextoCancelar = "E: CANCELAR ENCARGO";
        public const string TextoEspera = "SIN ENCARGOS · VUELVE EN ";
        public const string TextoHistoria = "TABLERO · TERMINA LA HISTORIA";
        public const string PrefijoCuota = "COBRA LA CUOTA ";
        public const string TextoMoroso = "VENCE AL MOROSO";
        public const string TextoIrDefender = "VE A DEFENDER SUBURBIOS";
        public const string PrefijoDefender = "DEFIENDE SUBURBIOS ";
        public const string TextoRobar = "ROBA EL CARRO MARCADO";
        public const string TextoEntregar = "LLEVA EL CARRO A CANEPA";
        public const string SubtituloMoroso = "—¡Hoy no pago!";
        public const string SubtituloCuota = "—Aquí está la cuota.";
        public const string SubtituloOleada = "—¡Los SNAKES vienen por la zona!";
        public const string SubtituloCarro = "—Ese es. Que nadie te vea.";
        public const string AyudaSinCarro = "NO HAY CARRO PARA ROBAR";
        public const int LargoMaximoObjetivo = 32;
        public const int RivalesOleada = 4;

        private const float RadioLocal = 3.5f;
        private const float RadioArranqueOleada = 25f;
        private const float RadioAbandono = 120f;
        private const float SegundosOleada = 240f;
        private const float RadioEntrega = 8f;
        private const float SegundosAviso = 5f;
        private const float RadioAnillo = 7f;

        private enum Fase { Ninguna, Cuota, Moroso, DefensaCamino, DefensaOleada, RobarCarro, EntregarCarro }

        [SerializeField] private DirectorPandillas historia;
        [Tooltip("Los locales de tu zona donde se cobra la cuota.")]
        [SerializeField] private Transform[] locales = System.Array.Empty<Transform>();
        [SerializeField] private Transform puntoDefensa;
        [Tooltip("El tablero (el punto desde donde se mide el respeto de tu zona).")]
        [SerializeField] private Transform tablero;
        [SerializeField] private PersonajeApoyo mecanico;
        [Tooltip("Rivales a puños, apagados: se prenden donde hacen falta (el moroso y la oleada).")]
        [SerializeField] private PandilleroAPie[] rivales = System.Array.Empty<PandilleroAPie>();

        private readonly ReglaEncargos regla = new ReglaEncargos();
        private Fase fase;
        private TipoEncargo tipo;
        private int indiceLocal;
        private bool resistio;
        private int vencidosOleada;
        private float finOleada;
        private Component carro;
        private IMontable montable;

        public ReglaEncargos Regla => regla;
        public bool HayEncargo => fase != Fase.Ninguna;
        public TipoEncargo Tipo => tipo;
        public string ClaveGuardado => Clave;

        /// <summary>Todos los objetivos de los encargos, para revisar que quepan en <see cref="LargoMaximoObjetivo"/> (D-tony-240).</summary>
        public static string[] TextosObjetivo => new[]
        {
            TextoCuota(3, 3), TextoMoroso, TextoIrDefender, TextoDefender(RivalesOleada, RivalesOleada), TextoRobar, TextoEntregar
        };

        public static string TextoCuota(int cobrados, int total) => PrefijoCuota + cobrados + "/" + total;

        public static string TextoDefender(int vencidos, int total) => PrefijoDefender + vencidos + "/" + total;

        private bool HistoriaLibre => historia != null && historia.Iniciado && historia.Paso == PasoHistoria.Libre;

        private PantallaHistoriaPandillas Pantalla => historia != null ? historia.Pantalla : null;

        /// <summary>Lo que dice el tablero ahora.</summary>
        public string TextoDelTablero
        {
            get
            {
                if (HayEncargo)
                {
                    return TextoCancelar;
                }
                if (!HistoriaLibre)
                {
                    return TextoHistoria;
                }
                if (!regla.TryProximo(out TipoEncargo proximo))
                {
                    return TextoEspera + Mathf.CeilToInt(regla.EsperaMinima()) + " S";
                }
                return "E: ENCARGO · " + ReglaEncargos.Nombre(proximo);
            }
        }

        public bool PuedeUsarTablero => HayEncargo || (ReglaEncargos.Permitido(HistoriaLibre, HayEncargo) && regla.TryProximo(out _));

        private void OnEnable() => Registro<IGuardable>.Agregar(this);

        private void OnDisable() => Registro<IGuardable>.Quitar(this);

        private void Awake()
        {
            foreach (PandilleroAPie r in rivales)
            {
                if (r != null)
                {
                    r.gameObject.SetActive(false);
                }
            }
        }

        // ---------- El tablero ----------

        /// <summary>E en el tablero: acepta el encargo que toca, o cancela el que está en curso.</summary>
        public void UsarTablero()
        {
            if (HayEncargo)
            {
                Terminar(false, "ENCARGO CANCELADO");
                return;
            }
            if (ReglaEncargos.Permitido(HistoriaLibre, HayEncargo) && regla.TryProximo(out TipoEncargo proximo))
            {
                Iniciar(proximo);
            }
        }

        /// <summary>Empieza un encargo de un tipo. False si no se pudo (otro en curso, historia sin terminar, en espera o sin lo necesario).</summary>
        public bool Iniciar(TipoEncargo nuevo)
        {
            if (!ReglaEncargos.Permitido(HistoriaLibre, HayEncargo) || !regla.Disponible(nuevo))
            {
                return false;
            }
            tipo = nuevo;
            vencidosOleada = 0;
            indiceLocal = 0;
            resistio = false;
            switch (nuevo)
            {
                case TipoEncargo.Cuota:
                    if (locales.Length == 0)
                    {
                        return false;
                    }
                    fase = Fase.Cuota;
                    Objetivo(TextoCuota(0, locales.Length), locales[0].position);
                    break;
                case TipoEncargo.Defender:
                    if (puntoDefensa == null)
                    {
                        return false;
                    }
                    fase = Fase.DefensaCamino;
                    Objetivo(TextoIrDefender, puntoDefensa.position);
                    break;
                default:
                    carro = BuscarCarro();
                    if (carro == null)
                    {
                        Pantalla?.MostrarAyuda(AyudaSinCarro, SegundosAviso);
                        return false;
                    }
                    montable = carro as IMontable;
                    fase = Fase.RobarCarro;
                    Objetivo(TextoRobar, carro.transform.position);
                    Pantalla?.MostrarSubtitulo(SubtituloCarro, SegundosAviso);
                    break;
            }
            Pantalla?.MostrarAyuda("ENCARGO: " + ReglaEncargos.Nombre(nuevo), SegundosAviso);
            return true;
        }

        // Entre los carros marcados "CarroRobable_N" (T-401) que nadie usa, uno distinto cada vez.
        private Component BuscarCarro()
        {
            List<Component> libres = new List<Component>();
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                Component c = m as Component;
                if (c != null && c.gameObject.activeInHierarchy && !m.Ocupado && c.name.StartsWith("CarroRobable"))
                {
                    libres.Add(c);
                }
            }
            if (libres.Count == 0)
            {
                return null;
            }
            libres.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return libres[regla.Hechos(TipoEncargo.Robar) % libres.Count];
        }

        // ---------- Cada cuadro ----------

        private void Update()
        {
            regla.Avanzar(Time.deltaTime);
            if (!HayEncargo)
            {
                return;
            }
            if (!HistoriaLibre)
            {
                // No chocan con la historia: si vuelve a un paso (cargar una partida), el encargo se deja sin castigo.
                Terminar(false, null, false);
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Vector3 yo = jugador.Raiz.position;
            switch (fase)
            {
                case Fase.Cuota:
                    ActualizarCuota(yo);
                    break;
                case Fase.Moroso:
                    if (RivalesVencidos() >= 1)
                    {
                        Cobrar();
                    }
                    break;
                case Fase.DefensaCamino:
                    if (Distancia(yo, puntoDefensa.position) <= RadioArranqueOleada)
                    {
                        EmpezarOleada();
                    }
                    break;
                case Fase.DefensaOleada:
                    ActualizarOleada(yo);
                    break;
                case Fase.RobarCarro:
                    ActualizarRobo(jugador);
                    break;
                case Fase.EntregarCarro:
                    ActualizarEntrega();
                    break;
            }
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).magnitude;
        }

        // ---------- Cobrar cuota ----------

        private void ActualizarCuota(Vector3 yo)
        {
            Transform local = locales[indiceLocal];
            if (Distancia(yo, local.position) > RadioLocal)
            {
                return;
            }
            if (!resistio && ReglaEncargos.SeResiste(regla.Hechos(TipoEncargo.Cuota), indiceLocal))
            {
                resistio = true;
                PonerRival(0, local.position + local.right * 2.5f);
                fase = Fase.Moroso;
                Objetivo(TextoMoroso, local.position);
                Pantalla?.MostrarSubtitulo(SubtituloMoroso, SegundosAviso);
                return;
            }
            Cobrar();
        }

        private void Cobrar()
        {
            Registro<ICartera>.Actual?.Abonar(ReglaEncargos.CuotaPorLocal, MotivoDinero.Mision);
            Pantalla?.MostrarSubtitulo(SubtituloCuota, 3f);
            indiceLocal++;
            resistio = false;
            if (indiceLocal >= locales.Length)
            {
                Terminar(true, null);
                return;
            }
            fase = Fase.Cuota;
            Objetivo(TextoCuota(indiceLocal, locales.Length), locales[indiceLocal].position);
        }

        // ---------- Defender zona ----------

        private void EmpezarOleada()
        {
            fase = Fase.DefensaOleada;
            finOleada = Time.time + SegundosOleada;
            int total = Mathf.Min(RivalesOleada, rivales.Length);
            for (int i = 0; i < total; i++)
            {
                float angulo = i * Mathf.PI * 2f / total;
                PonerRival(i, puntoDefensa.position + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * RadioAnillo);
            }
            Objetivo(TextoDefender(0, total), puntoDefensa.position);
            Pantalla?.MostrarSubtitulo(SubtituloOleada, SegundosAviso);
        }

        private void ActualizarOleada(Vector3 yo)
        {
            int total = Mathf.Min(RivalesOleada, rivales.Length);
            int vencidos = RivalesVencidos();
            if (vencidos > vencidosOleada)
            {
                for (int i = vencidosOleada; i < vencidos; i++)
                {
                    Registro<ITerritorio>.Actual?.RegistrarVencido(puntoDefensa.position, IdPandilla.Viboras, false);
                }
                vencidosOleada = vencidos;
                Objetivo(TextoDefender(vencidos, total), puntoDefensa.position);
            }
            if (vencidos >= total)
            {
                Terminar(true, null);
            }
            else if (Time.time >= finOleada || Distancia(yo, puntoDefensa.position) > RadioAbandono)
            {
                Terminar(false, "PERDISTE LA DEFENSA");
            }
        }

        // ---------- Robar y entregar ----------

        private void ActualizarRobo(IJugador jugador)
        {
            if (carro == null || !carro.gameObject.activeInHierarchy)
            {
                Terminar(false, "EL CARRO YA NO ESTÁ");
                return;
            }
            Objetivo(TextoRobar, carro.transform.position);
            if (jugador.Modo == ModoJugador.Carro && montable != null && montable.Ocupado)
            {
                fase = Fase.EntregarCarro;
                Objetivo(TextoEntregar, PuntoTaller());
                Pantalla?.MostrarAyuda("LLÉVALO CON CANEPA", SegundosAviso);
            }
        }

        private void ActualizarEntrega()
        {
            if (carro == null || !carro.gameObject.activeInHierarchy)
            {
                Terminar(false, "EL CARRO YA NO ESTÁ");
                return;
            }
            if (Distancia(carro.transform.position, PuntoTaller()) <= RadioEntrega)
            {
                Terminar(true, null);
            }
        }

        private Vector3 PuntoTaller() => mecanico != null ? mecanico.Posicion : (tablero != null ? tablero.position : Vector3.zero);

        // ---------- Rivales ----------

        private void PonerRival(int indice, Vector3 aprox)
        {
            if (indice < 0 || indice >= rivales.Length || rivales[indice] == null)
            {
                return;
            }
            Vector3 punto = aprox;
            if (Physics.Raycast(aprox + Vector3.up * 20f, Vector3.down, out RaycastHit piso, 40f))
            {
                punto = piso.point;
            }
            punto += Vector3.up * 0.1f;
            PandilleroAPie r = rivales[indice];
            CharacterController cc = r.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            r.transform.position = punto;
            r.Configurar(punto, null);
            r.gameObject.SetActive(true);
            r.Reaparecer();
        }

        private int RivalesVencidos()
        {
            int cuenta = 0;
            int hasta = fase == Fase.Moroso ? 1 : Mathf.Min(RivalesOleada, rivales.Length);
            for (int i = 0; i < hasta; i++)
            {
                if (rivales[i] != null && rivales[i].EstaKO)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private void ApagarRivales()
        {
            foreach (PandilleroAPie r in rivales)
            {
                if (r != null && r.gameObject.activeSelf)
                {
                    r.gameObject.SetActive(false);
                }
            }
        }

        // ---------- Fin ----------

        private void Terminar(bool exito, string aviso, bool avisar = true)
        {
            TipoEncargo hecho = tipo;
            fase = Fase.Ninguna;
            carro = null;
            montable = null;
            ApagarRivales();
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
            regla.Terminar(hecho, exito);
            if (exito)
            {
                int premio = ReglaEncargos.Premio(hecho);
                Registro<ICartera>.Actual?.Abonar(premio, MotivoDinero.Mision);
                ITerritorio territorio = Registro<ITerritorio>.Actual;
                Vector3 punto = tablero != null ? tablero.position : Vector3.zero;
                for (int i = 0; i < ReglaEncargos.RespetoUnidades(hecho); i++)
                {
                    territorio?.RegistrarVencido(punto, IdPandilla.Viboras, false);
                }
                Pantalla?.MostrarAyuda("ENCARGO LISTO · +" + Moneda.Texto(premio) + " · +RESPETO", SegundosAviso);
            }
            else if (avisar && aviso != null)
            {
                Pantalla?.MostrarAyuda(aviso, SegundosAviso);
            }
        }

        private static void Objetivo(string texto, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        // ---------- Guardado ----------

        public string Guardar() => regla.Guardar();

        public void Cargar(string datos)
        {
            if (!regla.Cargar(datos))
            {
                Debug.LogWarning("[Progresion] DirectorEncargos: el guardado no se entiende; no se cambia nada.");
            }
        }
    }
}
