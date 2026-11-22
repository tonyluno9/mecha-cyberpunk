using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10): lleva la historia nueva de 19 pasos (tabla 10.2 de docs/tarea-historia-en-juego.md) y reemplaza el recorrido viejo de
    /// <see cref="DirectorPandillas"/> (que queda en la escena desconectado, no borrado; sin aliados: EL CHATO, LA GÜERA y EL TOPO no entran).
    /// Se activa solo con NUEVA PARTIDA o al cargar una partida que ya traiga la clave <c>historia_cybercity</c>; cualquier otra forma de abrir la
    /// ciudad (pruebas viejas, Play directo) conserva el recorrido viejo. Reutiliza de la escena lo del viejo: el Doc, el Inventor, el Hacker, los
    /// grupos del Tianguis y de Neón Alto, la clínica, la tienda y el recinto del jefe final. Las cinemáticas salen de <see cref="CinematicaPendiente"/>
    /// y las peleas de los pasos 3, 12, 13, 15 (Nova permitida) y 17 activan <see cref="MisionProtegida"/>.
    /// Para saltar a un paso en Play: <c>DirectorRecorrido.SaltarAPaso(12)</c> (F8 pasa al siguiente fuera de la demo final).
    /// </summary>
    public class DirectorRecorrido : MonoBehaviour, IGuardable
    {
        public const string Clave = "historia_cybercity";
        private const float RadioMolestia = 14f;
        private const float RadioRanura = 4f;
        private const float RadioTorre = 35f;
        private const float DistanciaAlejarseInventor = 14f;
        private const float MetrosAtrasDeLaRanura = 7f;
        private const float SegundosEntreNaves = 50f;
        private const float SegundosPrimeraNave = 4f;
        private const int EstrellasNave = 5;
        private const float HoraNoche = 23f;
        private const float LluviaNoche = 0.7f;
        private const float SegundosFin = 5f;
        private const float SegundosEnNegroLevantarse = 1.8f;
        private const int RespetoPorSkull = 10;
        private const float AlturaRanura = 3f;
        private const float AnchoRanura = 3.2f;
        private const float GrosorRanura = 0.5f;
        private static readonly Color ColorTapia = new Color(0.12f, 0.13f, 0.15f);

        [Serializable]
        private class DatosGuardados
        {
            public string paso;
        }

        public static DirectorRecorrido Actual { get; private set; }

        /// <summary>Pruebas: simula la build Demo (con DEMO_FINAL ya lo es).</summary>
        public static bool ForzarDemo { get; set; }

        public static bool EsDemo
        {
            get
            {
#if DEMO_FINAL
                return true;
#else
                return ForzarDemo;
#endif
            }
        }

        public PasoRecorrido Paso => paso;
        public bool Iniciado => iniciado;
        public string ClaveGuardado => Clave;

        /// <summary>Se avisa con el paso nuevo cada vez que cambia (pruebas y depuración).</summary>
        public event Action<PasoRecorrido> PasoCambio;

        private PasoRecorrido paso = PasoRecorrido.Ninguno;
        private int sub;
        private bool modoNueva;
        private bool cargada;
        private bool iniciado;
        private bool saltado;
        private DatosRecorrido datos;
        private DirectorPandillas viejo;
        private ServicioHistoria historia;
        private MolestiaSkulls molestia;
        private GameObject ranuraCerrada;
        private RecintoJefe recinto;
        private bool jefeMostrado;
        private int implantesAntes;
        private float proximaNave;
        private Vector3 inicioPos;
        private Vector3 inicioAdelante = Vector3.forward;
        private bool inicioConocido;
        private bool sinMarcadorAvisado;

        // ---------- Arranque ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            ForzarDemo = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SuscribirEscena()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlArrancar() => Intentar();

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Intentar();

        // Después de Awake y antes de Start de la escena: el director viejo todavía no arrancó y se puede apagar.
        private static void Intentar()
        {
            if (Actual != null || FlujoPartida.EnTitulo || Object.FindFirstObjectByType<DirectorPandillas>() == null)
            {
                return;
            }
            AccionPartida pendiente = FlujoPartida.Pendiente;
            bool nueva = pendiente == AccionPartida.Nueva;
            bool carga = pendiente == AccionPartida.Cargar && ArchivoTieneElRecorrido(FlujoPartida.RanuraPendiente);
            if (nueva || carga)
            {
                Asegurar().modoNueva = nueva;
            }
        }

        private static bool ArchivoTieneElRecorrido(int ranura)
        {
            try
            {
                ServicioGuardado servicio = ServicioGuardado.Actual;
                if (servicio == null)
                {
                    return false;
                }
                string ruta = servicio.RutaRanura(ranura);
                return File.Exists(ruta) && File.ReadAllText(ruta).Contains(Clave);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Historia] No se pudo mirar si la partida trae el recorrido nuevo: " + e.Message);
                return false;
            }
        }

        /// <summary>Crea el director si no existe (apaga el recorrido viejo). Lo usan el arranque y la depuración.</summary>
        public static DirectorRecorrido Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            return new GameObject("DirectorRecorrido").AddComponent<DirectorRecorrido>();
        }

        /// <summary>Depuración: salta al paso <paramref name="numero"/> (1 a 19, 20 = juego libre). Crea el director si hace falta.</summary>
        public static bool SaltarAPaso(int numero)
        {
            if (!Enum.IsDefined(typeof(PasoRecorrido), numero) || numero <= 0)
            {
                Debug.LogWarning("[Historia] No existe el paso " + numero);
                return false;
            }
            Asegurar().SaltarA((PasoRecorrido)numero);
            return true;
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
            viejo = FindFirstObjectByType<DirectorPandillas>(FindObjectsInactive.Include);
            if (viejo != null)
            {
                viejo.enabled = false; // el recorrido viejo queda desconectado
            }
            DirectorPrologo prologo = FindFirstObjectByType<DirectorPrologo>(FindObjectsInactive.Include);
            if (prologo != null)
            {
                prologo.enabled = false;
            }
            datos = Resources.Load<DatosRecorrido>(DatosRecorrido.NombreRecurso);
            if (datos == null)
            {
                Debug.LogWarning("[Historia] Falta Resources/DatosRecorrido: se usan las cinemáticas de la tabla y no hay SKULLS en el paso 3 (corre CreadorRecorrido.Aplicar).");
            }
        }

        private void OnEnable() => Registro<IGuardable>.Agregar(this);

        private void OnDisable() => Registro<IGuardable>.Quitar(this);

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
            Cancelar();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }
            float limite = Time.realtimeSinceStartup + 3f;
            while (Registro<IJugador>.Actual == null && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            if (saltado)
            {
                yield break;
            }
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            historia?.Avanzar(EtapaPrologo.Libre); // el mundo ya no sigue las etapas viejas del prólogo
            iniciado = true;
            if (paso == PasoRecorrido.Ninguno)
            {
                if (modoNueva)
                {
                    IrA(PasoRecorrido.Recuerdo);
                }
                yield break;
            }
            Reanudar();
        }

        private void Reanudar()
        {
            PasoRecorrido p = paso;
            if (p == PasoRecorrido.Recuerdo)
            {
                p = PasoRecorrido.Despierta;
            }
            else if (p == PasoRecorrido.Fin)
            {
                p = PasoRecorrido.Libre;
            }
            Debug.Log("[Historia] Reanuda el recorrido en el paso " + (int)p + " (" + p + ")");
            IrA(p);
        }

        // ---------- Guardado ----------

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { paso = paso.ToString() });

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
            if (leidos == null || !Enum.TryParse(leidos.paso, false, out PasoRecorrido leido) || !Enum.IsDefined(typeof(PasoRecorrido), leido))
            {
                Debug.LogWarning("[Historia] El recorrido no entendió el paso guardado; empieza de nuevo.");
                return;
            }
            paso = leido;
            cargada = true;
            if (iniciado && !saltado)
            {
                Reanudar();
            }
        }

        private static void GuardarPartida()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio != null)
            {
                servicio.Guardar();
            }
        }

        // ---------- Cambio de paso ----------

        private void SaltarA(PasoRecorrido nuevo)
        {
            saltado = true;
            iniciado = true;
            if (historia == null)
            {
                historia = Registro<IHistoria>.Actual as ServicioHistoria;
                historia?.Avanzar(EtapaPrologo.Libre);
            }
            Cancelar();
            if (CinematicaPendiente.EnCurso)
            {
                CinematicaPendiente.SaltarActual();
            }
            IrA(nuevo);
        }

        private void Cancelar()
        {
            if (molestia != null)
            {
                molestia.Terminada -= AlTerminarMolestia;
                molestia.Cancelar();
                Destroy(molestia);
                molestia = null;
            }
            StopAllCoroutines();
            MisionProtegida.Desactivar();
        }

        private void IrA(PasoRecorrido nuevo)
        {
            paso = nuevo;
            sub = 0;
            jefeMostrado = false;
            Debug.Log("[Historia] Paso " + (int)nuevo + ": " + nuevo);
            PasoCambio?.Invoke(nuevo);
            ActualizarMundo();
            Entrar();
        }

        // Lo que queda del mundo según el paso (se repite al reanudar): la ranura cerrada y la noche.
        private void ActualizarMundo()
        {
            if (paso >= PasoRecorrido.Dinero)
            {
                CerrarRanura();
            }
            if (paso >= PasoRecorrido.Despierta && paso <= PasoRecorrido.Decision)
            {
                PonerNoche();
            }
            else if (paso > PasoRecorrido.Decision)
            {
                Reloj()?.Pausar(false);
                ClimaMundo.Lluvia = 0f;
            }
        }

        private void Entrar()
        {
            switch (paso)
            {
                case PasoRecorrido.Recuerdo: EntrarRecuerdo(); break;
                case PasoRecorrido.Despierta: EntrarDespierta(); break;
                case PasoRecorrido.Molestan: EntrarMolestan(); break;
                case PasoRecorrido.Maletin: EntrarMaletin(); break;
                case PasoRecorrido.Ranura: EntrarRanura(); break;
                case PasoRecorrido.Decision: EntrarDecision(); break;
                case PasoRecorrido.Dinero: EntrarDinero(); break;
                case PasoRecorrido.Suero: EntrarSuero(); break;
                case PasoRecorrido.Armas: EntrarArmas(); break;
                case PasoRecorrido.Inventor: EntrarInventor(); break;
                case PasoRecorrido.Chica: EntrarChica(); break;
                case PasoRecorrido.GuerraSkulls: EntrarGuerraSkulls(); break;
                case PasoRecorrido.GuerraSnakes: EntrarGuerraSnakes(); break;
                case PasoRecorrido.Hacker: EntrarHacker(); break;
                case PasoRecorrido.Robots: EntrarRobots(); break;
                case PasoRecorrido.Hermano: EntrarHermano(); break;
                case PasoRecorrido.JefeFinal: EntrarJefeFinal(); break;
                case PasoRecorrido.Puertas: EntrarPuertas(); break;
                case PasoRecorrido.Fin: EntrarFin(); break;
                case PasoRecorrido.Libre: EntrarLibre(); break;
            }
        }

        private void Cinematica(string id, Action enNegro, Action alTerminar)
        {
            DatosCinematica d = datos != null ? datos.Buscar(id) : null;
            if (d == null)
            {
                d = TablaRecorrido.CrearRespaldo(id);
            }
            CinematicaPendiente.Reproducir(d, enNegro, alTerminar);
        }

        // ---------- Pasos ----------

        // 1. C01 EL RECUERDO, antes de todo. En negro: de noche y el jugador tumbado frente a su casa.
        private void EntrarRecuerdo()
        {
            Cinematica("C01", () =>
            {
                PonerNoche();
                ColocarEnElInicio();
                VisualPersonaje visual = VisualDelJugador();
                if (visual != null)
                {
                    visual.Caido = true;
                }
                paso = PasoRecorrido.Despierta; // el paso 2 ya cuenta mientras se levanta
            }, () =>
            {
                IrA(PasoRecorrido.Despierta);
                StartCoroutine(Levantarse());
            });
        }

        // 2. Despierta en la calle, frente a su casa de Suburbios (sin cuarto, decisión de Tony).
        private void EntrarDespierta()
        {
            Fijar(TablaRecorrido.ObjetivoSalir, PosicionMolestia());
            GuardarPartida();
        }

        // 3. Dos SKULLS le salen al paso. Pelea protegida.
        private void EntrarMolestan()
        {
            MisionProtegida.Activar("historia: los SKULLS lo molestan", false);
            Quitar();
            molestia = gameObject.AddComponent<MolestiaSkulls>();
            molestia.Terminada += AlTerminarMolestia;
            float distancia = datos != null ? datos.DistanciaAparicionSkulls : 10f;
            if (datos == null || !molestia.Iniciar(datos.PrefabsSkull, distancia))
            {
                Debug.LogWarning("[Historia] No se pudo armar la escena de los SKULLS (faltan los prefabs en DatosRecorrido o el jugador): se sigue con la historia.");
                AlTerminarMolestia();
            }
        }

        private void AlTerminarMolestia()
        {
            if (paso != PasoRecorrido.Molestan)
            {
                return;
            }
            if (molestia != null)
            {
                molestia.Terminada -= AlTerminarMolestia;
                Destroy(molestia);
                molestia = null;
            }
            MisionProtegida.Desactivar();
            IrA(PasoRecorrido.Maletin);
        }

        // 4. C02 EL MALETÍN.
        private void EntrarMaletin()
        {
            Cinematica("C02", () =>
            {
                PonerNoche();
                Reloj()?.Pausar(true);
                ClimaMundo.Lluvia = LluviaNoche;
            }, () => IrA(PasoRecorrido.Ranura));
        }

        // 5. Noche, lluvia: ir a la ranura del muro.
        private void EntrarRanura()
        {
            PonerNoche();
            ClimaMundo.Lluvia = LluviaNoche;
            Fijar(TablaRecorrido.ObjetivoRanura, PosicionRanura());
            GuardarPartida();
        }

        // 6. C03 LA DECISIÓN. En negro el jugador vuelve atrás y la ranura se cierra para el resto del juego.
        private void EntrarDecision()
        {
            Cinematica("C03", () =>
            {
                Pose ranura = PoseRanura();
                Vector3 haciaDentro = (inicioConocido ? inicioPos - ranura.position : -ranura.forward);
                haciaDentro.y = 0f;
                haciaDentro = haciaDentro.sqrMagnitude > 0.01f ? haciaDentro.normalized : -ranura.forward;
                Vector3 donde = Suelo(ranura.position + haciaDentro * MetrosAtrasDeLaRanura);
                Registro<IJugador>.Actual?.Colocar(new Pose(donde, Quaternion.LookRotation(haciaDentro, Vector3.up)));
                CerrarRanura();
            }, () => IrA(PasoRecorrido.Dinero));
        }

        // 7. Ƀ20,000 del maletín y a ver al Doc.
        private void EntrarDinero()
        {
            ClimaMundo.Lluvia = 0f;
            Reloj()?.Pausar(false);
            Registro<ICartera>.Actual?.Abonar(TablaRecorrido.DineroMaletin, MotivoDinero.Mision);
            Avisar(TablaRecorrido.AvisoMaletin + Moneda.Texto(TablaRecorrido.DineroMaletin), MechaCyberpunk.UI.Hud.TipoAviso.Dinero);
            PersonajeApoyo doc = viejo != null ? viejo.Doc : null;
            if (doc == null)
            {
                Debug.LogWarning("[Historia] No hay Doc en la escena: se salta el paso 7.");
                IrA(PasoRecorrido.Suero);
                return;
            }
            Fijar(TablaRecorrido.ObjetivoDoc, doc.Posicion);
            doc.DarRegalo(() => IrA(PasoRecorrido.Suero));
            GuardarPartida();
        }

        // 8. C04 EL SUERO y luego la pantalla de mejoras.
        private void EntrarSuero()
        {
            PersonajeApoyo doc = viejo != null ? viejo.Doc : null;
            doc?.DarRegalo(null);
            IImplantes implantes = Registro<IImplantes>.Actual;
            implantesAntes = implantes != null ? implantes.Cantidad : 0;
            Cinematica("C04", () => VidaDelJugador()?.Restaurar(), () =>
            {
                sub = 1;
                if (doc != null)
                {
                    Fijar(TablaRecorrido.ObjetivoMejora, doc.Posicion);
                }
                if (Registro<IImplantes>.Actual == null)
                {
                    IrA(PasoRecorrido.Armas);
                    return;
                }
                MostradorClinica clinica = FindFirstObjectByType<MostradorClinica>();
                if (clinica != null && clinica.PuedeAbrirDesdeDoc)
                {
                    clinica.AbrirDesdeDoc();
                }
            });
        }

        // 9. Comprar un arma en el mercado y luego ir con el Inventor.
        private void EntrarArmas()
        {
            sub = TieneArma() ? 1 : 0;
            if (sub == 0)
            {
                Fijar(TablaRecorrido.ObjetivoArma, PosicionTiendaArmas());
            }
            else
            {
                PasarAlInventor();
            }
            GuardarPartida();
        }

        private void PasarAlInventor()
        {
            sub = 1;
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            if (inventor == null)
            {
                Debug.LogWarning("[Historia] No hay Inventor en la escena: se salta el paso 9.");
                IrA(PasoRecorrido.Inventor);
                return;
            }
            Fijar(TablaRecorrido.ObjetivoInventor, inventor.Posicion);
            inventor.DarRegalo(() => IrA(PasoRecorrido.Inventor));
        }

        // 10. C05 EL INVENTOR: la pistola mejorada.
        private void EntrarInventor()
        {
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            inventor?.DarRegalo(null);
            Cinematica("C05", () =>
            {
                ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
                DatosArma mejora = arsenal != null ? arsenal.DarMejora(ArmaAPie.Pistola) : null;
                Avisar(mejora != null ? mejora.Nombre.ToUpperInvariant() : TablaRecorrido.AvisoPistola, MechaCyberpunk.UI.Hud.TipoAviso.Bueno);
            }, () => IrA(PasoRecorrido.Chica));
        }

        // 11. C06 LA CHICA al salir del CIC la primera vez (al alejarse del Inventor).
        private void EntrarChica()
        {
            Quitar();
            sub = 0;
        }

        private void ReproducirChica()
        {
            sub = 1;
            Cinematica("C06", () => MisionProtegida.Activar("historia: guerra con los SKULLS", false), () => IrA(PasoRecorrido.GuerraSkulls));
        }

        // 12. Guerra con los SKULLS: liberar el Tianguis. Pelea protegida; la otra pandilla (Neón Alto) sigue apagada.
        private void EntrarGuerraSkulls()
        {
            MisionProtegida.Activar("historia: guerra con los SKULLS", false);
            Prender(viejo != null ? viejo.GruposNeon : null, false);
            Prender(viejo != null ? viejo.GruposTianguis : null, true);
            Fijar(TablaRecorrido.ObjetivoTianguis, PosicionGrupos(viejo != null ? viejo.GruposTianguis : null));
            GuardarPartida();
        }

        // 13. Guerra con los SNAKES: Neón Alto. Pelea protegida; los SKULLS ya cayeron y no se meten.
        private void EntrarGuerraSnakes()
        {
            MisionProtegida.Activar("historia: guerra con los SNAKES", false);
            GameObject[] neon = viejo != null ? viejo.GruposNeon : null;
            Prender(neon, true);
            Fijar(TablaRecorrido.ObjetivoNeon, PosicionGrupos(neon));
            GuardarPartida();
        }

        // 14. El Hacker: al hablarle sale C07. En la demo, después sale CONTINUARÁ.
        private void EntrarHacker()
        {
            PersonajeApoyo hacker = viejo != null ? viejo.Hacker : null;
            if (hacker == null)
            {
                Debug.LogWarning("[Historia] No hay Hacker en la escena: se salta a la cinemática C07.");
                ReproducirHacker();
                return;
            }
            Fijar(TablaRecorrido.ObjetivoHacker, hacker.Posicion);
            hacker.DarRegalo(() =>
            {
                hacker.DarRegalo(null);
                ReproducirHacker();
            });
            GuardarPartida();
        }

        private void ReproducirHacker()
        {
            Cinematica("C07", null, () =>
            {
                if (EsDemo)
                {
                    MostrarContinuara();
                }
                else
                {
                    IrA(PasoRecorrido.Robots);
                }
            });
        }

        private void MostrarContinuara()
        {
            Quitar();
            PantallaNegraRecorrido.Mostrar(TablaRecorrido.TituloContinuara, NombreJuego.Nombre, TablaRecorrido.TeclasContinuara, 0f, () => FlujoPartida.IrAlTitulo());
        }

        // 15. Llegan los robots de NOVA. Protegida contra la policía normal (NOVA sí ataca).
        private void EntrarRobots()
        {
            MisionProtegida.Activar("historia: los robots de NOVA", true);
            Fijar(TablaRecorrido.ObjetivoTorre, PosicionTorre());
            proximaNave = Time.time + SegundosPrimeraNave;
            GuardarPartida();
        }

        // 16. C08 EL HERMANO. En negro se pasa a la protección del jefe final (sin refuerzos de NOVA).
        private void EntrarHermano()
        {
            Cinematica("C08", () => MisionProtegida.Activar("historia: el jefe final", false), () => IrA(PasoRecorrido.JefeFinal));
        }

        // 17. Jefe final: pelea protegida contra E14.
        private void EntrarJefeFinal()
        {
            MisionProtegida.Activar("historia: el jefe final", false);
            recinto = FindFirstObjectByType<RecintoJefe>();
            Fijar(TablaRecorrido.ObjetivoJefeNova, recinto != null ? recinto.transform.position : PosicionTorre());
            if (recinto == null)
            {
                Debug.LogWarning("[Historia] No hay RecintoJefe en la escena: el paso 17 no puede terminar solo (usa SaltarAPaso(18)).");
            }
            GuardarPartida();
        }

        // 18. C09 LAS PUERTAS.
        private void EntrarPuertas()
        {
            Cinematica("C09", () => MisionProtegida.Desactivar(), () => IrA(PasoRecorrido.Fin));
        }

        // 19. Fin: negro, "CYBER CITY", "¿YA SALISTE?", créditos y vuelta al título. CONTINUAR = juego libre.
        private void EntrarFin()
        {
            MisionProtegida.Desactivar();
            Quitar();
            paso = PasoRecorrido.Libre; // lo que se guarda: al CONTINUAR la partida sigue en juego libre
            PrepararLibre();
            GuardarPartida();
            ControladorAPie control = ControlDelJugador();
            control?.ActivarControl(false);
            PantallaNegraRecorrido.Mostrar(TablaRecorrido.TituloFin, TablaRecorrido.LineaFin, string.Empty, SegundosFin, () =>
                PantallaCreditos.Abrir(() => FlujoPartida.IrAlTitulo()));
        }

        private void EntrarLibre()
        {
            PrepararLibre();
            GuardarPartida();
        }

        private void PrepararLibre()
        {
            MisionProtegida.Desactivar();
            Prender(viejo != null ? viejo.GruposTianguis : null, true);
            Prender(viejo != null ? viejo.GruposNeon : null, true);
            viejo?.Doc?.DarRegalo(null);
            viejo?.Inventor?.DarRegalo(null);
            viejo?.Hacker?.DarRegalo(null);
            Quitar();
        }

        // ---------- Cada cuadro ----------

        private void Update()
        {
#if !DEMO_FINAL
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.f8Key.wasPressedThisFrame && !Pausa.Activa && paso != PasoRecorrido.Libre && paso != PasoRecorrido.Ninguno)
            {
                SaltarA(paso + 1);
                return;
            }
#endif
            if (!iniciado || CinematicaEnCurso.Activa || FlujoPartida.EnTitulo)
            {
                return;
            }
            Transform jugador = JugadorRaiz();
            if (jugador == null)
            {
                return;
            }
            switch (paso)
            {
                case PasoRecorrido.Despierta:
                    if (Plano(jugador.position, PosicionMolestia()) <= RadioMolestia
                        || (inicioConocido && Plano(jugador.position, inicioPos) >= MetrosParaMolestia()))
                    {
                        IrA(PasoRecorrido.Molestan);
                    }
                    break;
                case PasoRecorrido.Ranura:
                    if (Plano(jugador.position, PosicionRanura()) <= RadioRanura)
                    {
                        IrA(PasoRecorrido.Decision);
                    }
                    break;
                case PasoRecorrido.Suero:
                    ActualizarSuero();
                    break;
                case PasoRecorrido.Armas:
                    if (sub == 0 && TieneArma())
                    {
                        PasarAlInventor();
                    }
                    break;
                case PasoRecorrido.Chica:
                    ActualizarChica(jugador);
                    break;
                case PasoRecorrido.GuerraSkulls:
                    if (ActualizarConquista(viejo != null ? viejo.ZonaTianguis : "tianguis", viejo != null ? viejo.GruposTianguis : null,
                        TablaRecorrido.ObjetivoJefeSkulls, IdPandilla.Calaveras))
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.GuerraSnakes);
                    }
                    break;
                case PasoRecorrido.GuerraSnakes:
                    if (ActualizarConquista(TablaRecorrido.ZonaNeon, viejo != null ? viejo.GruposNeon : null,
                        TablaRecorrido.ObjetivoJefeSnakes, IdPandilla.Viboras))
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.Hacker);
                    }
                    break;
                case PasoRecorrido.Robots:
                    ActualizarRobots(jugador);
                    break;
                case PasoRecorrido.JefeFinal:
                    if (recinto != null && recinto.Jefe != null && recinto.Jefe.Vencido)
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.Puertas);
                    }
                    break;
            }
        }

        private void ActualizarSuero()
        {
            if (sub != 1)
            {
                return;
            }
            IImplantes implantes = Registro<IImplantes>.Actual;
            if (implantes != null && implantes.Cantidad > implantesAntes)
            {
                IrA(PasoRecorrido.Armas);
            }
        }

        private void ActualizarChica(Transform jugador)
        {
            if (sub != 0)
            {
                return;
            }
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            if (inventor == null || Plano(jugador.position, inventor.Posicion) >= DistanciaAlejarseInventor)
            {
                ReproducirChica();
            }
        }

        private void ActualizarRobots(Transform jugador)
        {
            if (Plano(jugador.position, PosicionTorre()) <= RadioTorre)
            {
                IrA(PasoRecorrido.Hermano);
                return;
            }
            if (Time.time >= proximaNave)
            {
                proximaNave = Time.time + SegundosEntreNaves;
                // Lo que hoy hace llegar a NOVA: una nave que baja soldados cerca del jugador (RespuestaNova, T-1498).
                RespuestaNova nova = RespuestaNova.Actual;
                if (nova != null)
                {
                    nova.LlamarNave(jugador.position, EstrellasNave);
                }
            }
        }

        // Cuenta de la conquista de una zona; true cuando la zona ya es del jugador. Como el director viejo, no se puede trabar por falta de SKULLS.
        private bool ActualizarConquista(string zona, GameObject[] grupos, string textoJefe, IdPandilla rival)
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (territorio == null)
            {
                return false;
            }
            if (territorio.Dueno(zona) == IdPandilla.Jugador)
            {
                return true;
            }
            CompletarSiCayeronTodos(territorio, zona, grupos, rival);
            if (!jefeMostrado && territorio.JefeDisponible(zona))
            {
                jefeMostrado = true;
                Vector3 donde = PosicionJefe(zona, grupos);
                Fijar(textoJefe, donde);
            }
            return false;
        }

        private void CompletarSiCayeronTodos(ITerritorio territorio, string zona, GameObject[] grupos, IdPandilla rival)
        {
            if (grupos == null || grupos.Length == 0)
            {
                return;
            }
            foreach (GameObject g in grupos)
            {
                PandillaAPie pandilla = g != null ? g.GetComponent<PandillaAPie>() : null;
                if (pandilla == null || !pandilla.ModoMision || !pandilla.TodosVencidos)
                {
                    return;
                }
            }
            Vector3 punto = PosicionGrupos(grupos);
            int seguridad = 20;
            while (!territorio.JefeDisponible(zona) && territorio.Dueno(zona) != IdPandilla.Jugador && seguridad-- > 0)
            {
                territorio.RegistrarVencido(punto, rival, false);
            }
        }

        // ---------- Mundo ----------

        private static void Prender(GameObject[] grupos, bool activo)
        {
            if (grupos == null)
            {
                return;
            }
            foreach (GameObject g in grupos)
            {
                if (g == null || g.activeSelf == activo)
                {
                    continue;
                }
                if (activo)
                {
                    PandillaAPie pandilla = g.GetComponent<PandillaAPie>();
                    if (pandilla != null)
                    {
                        pandilla.EsDeGuion = true;
                        pandilla.ModoMision = true;
                    }
                }
                g.SetActive(activo);
            }
        }

        private void PonerNoche()
        {
            IRelojMundo reloj = Reloj();
            if (reloj != null && (!reloj.EsDeNoche || reloj.Hora < HoraNoche - 1f))
            {
                reloj.AvanzarHasta(HoraNoche);
            }
            if (paso >= PasoRecorrido.Despierta && paso <= PasoRecorrido.Decision)
            {
                reloj?.Pausar(true);
            }
        }

        private static IRelojMundo Reloj() => Registro<IRelojMundo>.Actual;

        private void CerrarRanura()
        {
            if (ranuraCerrada != null)
            {
                return;
            }
            Pose p = PoseRanura();
            ranuraCerrada = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ranuraCerrada.name = "RanuraCerrada";
            Transform t = ranuraCerrada.transform;
            t.SetPositionAndRotation(p.position + Vector3.up * (AlturaRanura * 0.5f), Quaternion.Euler(0f, p.rotation.eulerAngles.y, 0f));
            t.localScale = new Vector3(AnchoRanura, AlturaRanura, GrosorRanura);
            Renderer r = ranuraCerrada.GetComponent<Renderer>();
            Shader sombreador = Shader.Find("Universal Render Pipeline/Lit");
            if (r != null && sombreador != null)
            {
                Material m = new Material(sombreador) { name = "TapiaRanura" };
                m.SetColor("_BaseColor", ColorTapia);
                m.SetFloat("_Smoothness", 0.15f);
                r.sharedMaterial = m;
            }
        }

        private IEnumerator Levantarse()
        {
            ControladorAPie control = ControlDelJugador();
            VisualPersonaje visual = VisualDelJugador();
            if (visual == null)
            {
                yield break;
            }
            control?.ActivarControl(false);
            visual.Caido = true;
            yield return new WaitForSecondsRealtime(ReglasAnimacion.RetardoLevantarse);
            visual.Caido = false;
            yield return new WaitForSecondsRealtime(SegundosEnNegroLevantarse);
            control?.ActivarControl(true);
        }

        private bool TieneArma()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return false;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && arsenal.Tiene(arma))
                {
                    return true;
                }
            }
            return false;
        }

        // ---------- Posiciones ----------

        private float MetrosParaMolestia() => datos != null ? datos.MetrosParaMolestia : 22f;

        private void ConocerInicio()
        {
            if (inicioConocido)
            {
                return;
            }
            IPuntoAparicion punto = BuscarAparicion(viejo != null ? viejo.IdPuntoInicio : "Inicio_Calle") ?? BuscarAparicion(DirectorPandillas.IdInicioRespaldo);
            if (punto != null)
            {
                inicioPos = punto.Pose.position;
                inicioAdelante = Vector3.ProjectOnPlane(punto.Pose.rotation * Vector3.forward, Vector3.up).normalized;
                inicioConocido = true;
                return;
            }
            Transform j = JugadorRaiz();
            if (j != null)
            {
                inicioPos = j.position;
                inicioAdelante = Vector3.ProjectOnPlane(j.forward, Vector3.up).normalized;
                inicioConocido = true;
            }
        }

        private void ColocarEnElInicio()
        {
            ConocerInicio();
            IPuntoAparicion punto = BuscarAparicion(viejo != null ? viejo.IdPuntoInicio : "Inicio_Calle") ?? BuscarAparicion(DirectorPandillas.IdInicioRespaldo);
            if (punto == null)
            {
                Debug.LogWarning("[Historia] No hay punto de aparición Inicio_Calle: el jugador empieza donde está.");
                return;
            }
            Registro<IJugador>.Actual?.Colocar(punto.Pose);
        }

        private Vector3 PosicionMolestia()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdMolestia, out Pose pose))
            {
                return pose.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdMolestia);
            ConocerInicio();
            return Ajustar(inicioPos + inicioAdelante * MetrosParaMolestia());
        }

        private Pose PoseRanura()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdRanura, out Pose pose))
            {
                return pose;
            }
            AvisarSinMarcador(MarcadorHistoria.IdRanura);
            ConocerInicio();
            float metros = datos != null ? datos.MetrosHastaRanura : 45f;
            Vector3 donde = Ajustar(inicioPos + inicioAdelante * metros);
            return new Pose(donde, Quaternion.LookRotation(inicioAdelante, Vector3.up));
        }

        private Vector3 PosicionRanura() => PoseRanura().position;

        private Vector3 PosicionTorre()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdTorre, out Pose pose))
            {
                return pose.position;
            }
            RecintoJefe r = FindFirstObjectByType<RecintoJefe>();
            if (r != null)
            {
                return r.transform.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdTorre);
            Transform j = JugadorRaiz();
            return j != null ? j.position + j.forward * 200f : Vector3.zero;
        }

        private Vector3 PosicionTiendaArmas()
        {
            foreach (MostradorTienda m in FindObjectsByType<MostradorTienda>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                foreach (DatosObjeto objeto in m.EnVenta)
                {
                    if (objeto is DatosArma)
                    {
                        return m.Posicion;
                    }
                }
            }
            return PosicionGrupos(viejo != null ? viejo.GruposTianguis : null);
        }

        private Vector3 PosicionJefe(string zona, GameObject[] grupos)
        {
            foreach (JefePandilla j in FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    return j.transform.position;
                }
            }
            return PosicionGrupos(grupos);
        }

        private static Vector3 PosicionGrupos(GameObject[] grupos)
        {
            if (grupos != null)
            {
                foreach (GameObject g in grupos)
                {
                    if (g != null)
                    {
                        return g.transform.position;
                    }
                }
            }
            return Vector3.zero;
        }

        private void AvisarSinMarcador(string id)
        {
            if (!sinMarcadorAvisado)
            {
                sinMarcadorAvisado = true;
                Debug.LogWarning("[Historia] Falta el marcador de historia '" + id + "' en la escena: se usa una posición de respaldo (ver docs).");
            }
        }

        private static Vector3 Ajustar(Vector3 punto)
        {
            if (NavMesh.SamplePosition(punto, out NavMeshHit golpe, 30f, NavMesh.AllAreas))
            {
                return golpe.position;
            }
            return Suelo(punto);
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 8f, Vector3.down, out RaycastHit golpe, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point;
            }
            return punto;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static IPuntoAparicion BuscarAparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto != null && punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        // ---------- Jugador, objetivos y avisos ----------

        private static Transform JugadorRaiz()
        {
            IJugador j = Registro<IJugador>.Actual;
            return j != null ? j.Raiz : null;
        }

        private static ControladorAPie ControlDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<ControladorAPie>(true) : null;
        }

        private static VisualPersonaje VisualDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<VisualPersonaje>(true) : null;
        }

        private static VidaAPie VidaDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<VidaAPie>(true) : null;
        }

        private static void Fijar(string texto, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        private static void Quitar()
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
        }

        private static void Avisar(string texto, MechaCyberpunk.UI.Hud.TipoAviso tipo)
        {
            MechaCyberpunk.UI.Hud.ColaAvisos.Publicar(texto, MechaCyberpunk.UI.Hud.PrioridadAviso.Normal, tipo, 4f);
        }
    }
}
