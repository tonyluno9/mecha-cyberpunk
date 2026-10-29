using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-202: FINTAS (el rival alarga su aviso y amaga) y RETIRADA (da un paso atrás tras su golpe). Arena + flujo + MechaJugador, entrada
    /// simulada con el ratón. Atajos permitidos y anotados: niveles fijados en MemoriaRivales antes de preparar la pelea, FijarAzar de
    /// FintasRival y RetiradaRival (a veces con una semilla fija), Time.timeScale en las pruebas que no miden tiempos finos, y vida
    /// restaurada entre golpes.
    /// </summary>
    public class FintasRetiradaPruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaRival = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga{0}_{1}.prefab";
        private const string IdRival = "DatosRival_Liga{0}_{1}";
        private const string RutaCaptura = "docs/capturas/T-202-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EsperaMaxima = 15f;
        private const float LimiteSegundos = 240f;
        private const float MitadDeVida = 0.5f;
        private const float AzarSiempre = 0f;
        private const float AzarNunca = 0.99f;
        private const float AzarUno = 1f;
        private const float EsperaRapida = 0.3f;
        private const float EsperaNormal = 1f;
        private const float Escala1 = 1f;
        private const float EscalaMedia = 2f;
        private const float EscalaRapida = 3f;
        private const float EscalaMuyRapida = 4f;
        private const int GolpesMedidos = 10;
        private const int GolpesDeBloqueo = 20;
        private const int GolpesSeguidos = 20;
        private const int GolpesNaturales = 40;
        private const int SemillaNatural = 202;
        private const int SemillaBloqueo = 2020;
        private const float TiempoPulsoGuardia = 0.35f;
        private const float TiempoSoltarGuardia = 0.15f;
        private const float MargenCuadros = 1.2f;
        private const float HolguraAviso = 0.01f;
        private const float AnguloMinimoAmago = 10f;
        private const float AnguloReposo = 1f;
        private const float MargenDistancia = 0.1f;
        private const float ToleranciaVelocidad = 1e-3f;
        private const float ToleranciaAvisoHabilidad = 0.15f;
        private const float ToleranciaEnfriamiento = 1f;
        private const float ContragolpeTemprano = 0.3f;
        private const float ContragolpeTardio = 0.9f;
        private const int MaximoTardioQueConecta = 3;
        private const float VelocidadDeCaptura = 1.5f;
        private const float CuadroDeTironMaximo = 0.15f;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private class Vigia : MonoBehaviour
        {
            public RivalBasico Rival;
            public Quaternion Reposo;
            public int CuadrosRetirandose;
            public float DtVentana;
            public float MaxAnguloVentana;
            public float AnguloTrasCancelar = -1f;
            private bool cancelado;

            public void ReiniciarVentana()
            {
                DtVentana = 0f;
                MaxAnguloVentana = 0f;
            }

            public void AvisarCancelado()
            {
                cancelado = true;
            }

            private void Update()
            {
                DtVentana = Mathf.Max(DtVentana, Time.deltaTime);
                if (Rival.Retirandose)
                {
                    CuadrosRetirandose++;
                }
            }

            private void LateUpdate()
            {
                float angulo = Quaternion.Angle(Reposo, Rival.Brazo.localRotation);
                if (Rival.EnAviso)
                {
                    MaxAnguloVentana = Mathf.Max(MaxAnguloVentana, angulo);
                }
                if (cancelado)
                {
                    AnguloTrasCancelar = angulo;
                    cancelado = false;
                }
            }
        }

        private class Aviso
        {
            public float Inicio;
            public float Fin;
            public bool Amago;
            public float DtMax;
            public float MaxAngulo;
            public float AnguloTras = -1f;
        }

        private class Ensayo
        {
            public float Retraso;
            public float DistanciaInicial;
            public float DistanciaMaxima;
            public float SegundosHastaMeta = -1f;
            public bool LlegoALaMeta;
            public float VelocidadMaxima;
            public bool Conecto;
        }

        private struct Recibido
        {
            public float Tiempo;
            public TipoBloqueo Bloqueo;
        }

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private readonly List<Aviso> avisos = new List<Aviso>();
        private readonly List<Recibido> recibidos = new List<Recibido>();
        private ConfigFlujoPelea configPrueba;
        private GameObject jugador;
        private VidaMecha vidaJugador;
        private GolpeMecha golpeJugador;
        private IPeleador peleadorJugador;
        private DirectorPelea director;
        private MemoriaRivales memoria;
        private AdaptacionRival adaptacion;
        private RivalBasico basico;
        private FintasRival fintas;
        private RetiradaRival retirada;
        private VidaMecha vidaRival;
        private Vigia vigia;
        private Aviso actual;
        private float escalaOriginal;
        private int usosFintas;
        private int usosRetirada;
        private bool impactoVisto;
        private bool conectoVisto;
        private bool impactoRivalNuevo;
        private float ultimoImpactoRival;
        private bool capturado;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            avisos.Clear();
            recibidos.Clear();
            memoria = null;
            usosFintas = 0;
            usosRetirada = 0;
            actual = null;
            capturado = false;
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += RegistrarMensaje;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarMensaje;
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            if (configPrueba != null)
            {
                Object.Destroy(configPrueba);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarMensaje(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string RutaDe(int liga, int n) => string.Format(RutaRival, liga, n);

        // Arena, jugador, memoria con los niveles pedidos y FlujoPelea con el rival de liga dado; termina en Peleando.
        private IEnumerator Escenario(int liga, int n, int evidenciaFintas, int evidenciaRetirada, float escala, float multiplicadorEspera)
        {
            yield return Calentar();
            GameObject servicios = new GameObject("MemoriaRivales");
            creados.Add(servicios);
            memoria = servicios.AddComponent<MemoriaRivales>();
            MemoriaRival entrada = memoria.Obtener(string.Format(IdRival, liga, n));
            entrada.evidencias[(int)TipoAdaptacion.Fintas] = evidenciaFintas;
            entrada.evidencias[(int)TipoAdaptacion.Retirada] = evidenciaRetirada;
            memoria.AvisarCambio();

            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador), inicio.position, inicio.rotation);
            creados.Add(jugador);
            vidaJugador = jugador.GetComponent<VidaMecha>();
            golpeJugador = jugador.GetComponent<GolpeMecha>();
            peleadorJugador = jugador.GetComponent<IPeleador>();
            golpeJugador.Impacto += () => impactoVisto = true;
            golpeJugador.Conecto += resultado => conectoVisto = true;
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;

            configPrueba = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configPrueba, "segundosPorNumero", 0.3f);
            FijarFloat(configPrueba, "pausaDespuesKO", 0.3f);
            FijarFloat(configPrueba, "segundosTextoPelea", 0.3f);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlujo), padre.transform);
            creados.Add(flujo);
            creados.Add(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configPrueba;
            soDirector.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaDe(liga, n));
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configPrueba;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            yield return null;
            yield return null;

            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            Component rival = (Component)director.Rival;
            adaptacion = rival.GetComponent<AdaptacionRival>();
            basico = rival.GetComponent<RivalBasico>();
            fintas = rival.GetComponent<FintasRival>();
            retirada = rival.GetComponent<RetiradaRival>();
            vidaRival = rival.GetComponent<VidaMecha>();
            Assert.IsNotNull(fintas, "El prefab del rival tiene FintasRival");
            Assert.IsNotNull(retirada, "El prefab del rival tiene RetiradaRival");
            adaptacion.Usada += tipo =>
            {
                if (tipo == TipoAdaptacion.Fintas)
                {
                    usosFintas++;
                }
                else if (tipo == TipoAdaptacion.Retirada)
                {
                    usosRetirada++;
                }
            };
            vigia = rival.gameObject.AddComponent<Vigia>();
            vigia.Rival = basico;
            vigia.Reposo = basico.Brazo.localRotation;
            basico.GolpeIniciado += AlIniciarGolpe;
            basico.Impacto += AlImpactarRival;
            basico.GolpeCancelado += AlCancelarGolpe;
            basico.MultiplicadorEspera = multiplicadorEspera;

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = escala;
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        private void AlIniciarGolpe()
        {
            actual = new Aviso { Inicio = Time.time };
            vigia.ReiniciarVentana();
        }

        private void AlImpactarRival()
        {
            impactoRivalNuevo = true;
            ultimoImpactoRival = Time.time;
            CerrarAviso(false);
        }

        private void AlCancelarGolpe()
        {
            vigia.AvisarCancelado();
            CerrarAviso(true);
        }

        private void CerrarAviso(bool amago)
        {
            if (actual == null)
            {
                return;
            }
            actual.Fin = Time.time;
            actual.Amago = amago;
            actual.DtMax = vigia.DtVentana;
            actual.MaxAngulo = vigia.MaxAnguloVentana;
            avisos.Add(actual);
            if (amago)
            {
                // El ángulo tras cancelar se completa en el LateUpdate de este mismo cuadro.
                Aviso cerrado = actual;
                PendienteAngulo(cerrado);
            }
            actual = null;
        }

        private readonly List<Aviso> porCompletar = new List<Aviso>();

        private void PendienteAngulo(Aviso aviso)
        {
            porCompletar.Add(aviso);
        }

        private void CompletarAngulos()
        {
            if (porCompletar.Count > 0 && vigia.AnguloTrasCancelar >= 0f)
            {
                foreach (Aviso a in porCompletar)
                {
                    a.AnguloTras = vigia.AnguloTrasCancelar;
                }
                porCompletar.Clear();
            }
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (ReferenceEquals(objetivo, peleadorJugador) && info.Tipo == TipoDanio.Golpe)
            {
                recibidos.Add(new Recibido { Tiempo = Time.time, Bloqueo = resultado.Bloqueo });
            }
        }

        private void MantenerVida()
        {
            if (vidaJugador.Actual < vidaJugador.Maxima * MitadDeVida)
            {
                vidaJugador.Restaurar();
            }
            if (vidaRival.Actual < vidaRival.Maxima * MitadDeVida)
            {
                vidaRival.Restaurar();
            }
            CompletarAngulos();
        }

        private IEnumerator HastaAvisos(int cantidad)
        {
            float limite = Time.time + LimiteSegundos;
            while (avisos.Count < cantidad && Time.time < limite)
            {
                MantenerVida();
                yield return null;
            }
            yield return null;
            CompletarAngulos();
            Assert.GreaterOrEqual(avisos.Count, cantidad, "No juntó los avisos pedidos");
        }

        private float Serie => basico.Datos.Mecha.PreparacionGolpe;

        private static float Tolerancia(Aviso aviso) => aviso.DtMax * MargenCuadros + HolguraAviso;

        private static string Linea(int i, Aviso a)
        {
            return "golpe " + i + ": " + (a.Amago ? "AMAGO" : "real ") + " aviso " + (a.Fin - a.Inicio).ToString("0.000") + " s (cuadro máx "
                + a.DtMax.ToString("0.000") + ")" + (a.Amago ? ", brazo atrás máx " + a.MaxAngulo.ToString("0.0") + " grados, tras cancelar "
                + a.AnguloTras.ToString("0.0") : "");
        }

        [UnityTest]
        public IEnumerator NivelCero_DiezGolpes_AvisoDeSerieYNuncaSeRetira()
        {
            yield return Escenario(1, 1, 0, 0, Escala1, EsperaRapida);
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Fintas));
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Retirada));
            yield return HastaAvisos(GolpesMedidos);
            for (int i = 0; i < GolpesMedidos; i++)
            {
                Aviso a = avisos[i];
                Debug.Log("[Prueba] nivel 0, " + Linea(i, a));
                Assert.IsFalse(a.Amago, "Sin FINTAS nunca amaga");
                Assert.AreEqual(Serie, a.Fin - a.Inicio, Tolerancia(a), "Aviso de serie en el golpe " + i);
            }
            Assert.AreEqual(0, vigia.CuadrosRetirandose, "Sin RETIRADA nunca se retira");
            Assert.AreEqual(0, usosFintas + usosRetirada);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator FintasI_AzarUno_AvisoEsLaSeriePorElExtraMaximo()
        {
            yield return Escenario(1, 1, 1, 0, Escala1, EsperaRapida);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Fintas));
            fintas.FijarAzar(() => AzarUno);
            float extra = adaptacion.Datos.ExtraAvisoMaximo[0];
            yield return HastaAvisos(GolpesMedidos / 2);
            for (int i = 0; i < GolpesMedidos / 2; i++)
            {
                Aviso a = avisos[i];
                Debug.Log("[Prueba] FINTAS I azar 1, " + Linea(i, a));
                Assert.IsFalse(a.Amago);
                Assert.AreEqual(Serie + extra, a.Fin - a.Inicio, Tolerancia(a), "Aviso = serie + extra máximo en el golpe " + i);
            }
            Assert.AreEqual(0, usosFintas, "Sin amagos no avisa uso");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator FintasI_AzarCero_AmagaYElGolpeSiguienteEsRealYNuncaDosSeguidos()
        {
            yield return Escenario(1, 1, 1, 0, Escala1, EsperaRapida);
            fintas.FijarAzar(() => AzarSiempre);
            yield return HastaAvisos(GolpesSeguidos);
            float fraccion = adaptacion.Datos.FraccionAmago;
            float esperaTras = adaptacion.Datos.EsperaTrasAmago;
            int amagos = 0;
            for (int i = 0; i < GolpesSeguidos; i++)
            {
                Aviso a = avisos[i];
                Debug.Log("[Prueba] FINTAS I azar 0, " + Linea(i, a));
                if (a.Amago)
                {
                    amagos++;
                    Assert.AreEqual(Serie * fraccion, a.Fin - a.Inicio, Tolerancia(a), "El amago se cancela al 60 % del aviso de serie");
                    Assert.Greater(a.MaxAngulo, AnguloMinimoAmago, "El brazo se echa atrás en el amago");
                    Assert.Less(a.AnguloTras, AnguloReposo, "Y vuelve al reposo al cancelar");
                    if (i + 1 < avisos.Count)
                    {
                        Assert.IsFalse(avisos[i + 1].Amago, "Nunca dos amagos seguidos");
                        Assert.GreaterOrEqual(avisos[i + 1].Fin - avisos[i + 1].Inicio, Serie - Tolerancia(avisos[i + 1]),
                            "El golpe que sigue a un amago tiene aviso de serie o más");
                        Assert.GreaterOrEqual(avisos[i + 1].Inicio - a.Fin, esperaTras - Tolerancia(a), "Espera tras el amago");
                        foreach (Recibido r in recibidos)
                        {
                            Assert.IsFalse(r.Tiempo >= a.Inicio && r.Tiempo < avisos[i + 1].Inicio, "Un amago no pega");
                        }
                    }
                }
            }
            Assert.GreaterOrEqual(amagos, GolpesSeguidos / 2 - 1, "Con azar 0 amaga uno de cada dos");
            Assert.AreEqual(amagos, usosFintas, "Cada amago avisa AvisarUso(Fintas)");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator FintasI_AzarNatural_NuncaDosAmagosSeguidosEnCuarentaGolpes()
        {
            yield return Escenario(1, 1, 1, 0, EscalaRapida, EsperaRapida);
            System.Random generador = new System.Random(SemillaNatural);
            fintas.FijarAzar(() => (float)generador.NextDouble());
            yield return HastaAvisos(GolpesNaturales);
            int amagos = 0;
            for (int i = 0; i < GolpesNaturales; i++)
            {
                if (avisos[i].Amago)
                {
                    amagos++;
                    Assert.IsFalse(i + 1 < avisos.Count && avisos[i + 1].Amago, "Dos amagos seguidos en el golpe " + i);
                }
                else
                {
                    Assert.GreaterOrEqual(avisos[i].Fin - avisos[i].Inicio, Serie - Tolerancia(avisos[i]), "El aviso real nunca baja de la serie");
                }
            }
            Debug.Log("[Prueba] FINTAS I azar natural (semilla " + SemillaNatural + "): " + amagos + " amagos en " + GolpesNaturales + " golpes");
            Assert.Greater(amagos, 0, "Con azar natural amaga alguna vez");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // Un jugador que sube la guardia a tiempo para el aviso de serie (llega 0.1 s antes del impacto) y la suelta después.
        private IEnumerator BloquearGolpes(int cuantos, List<Recibido> resultado)
        {
            float limite = Time.time + LimiteSegundos;
            float inicioGolpe = -1f;
            float soltarEn = -1f;
            bool sosteniendo = false;
            int vistos = recibidos.Count;
            int avisosVistos = avisos.Count;
            while (resultado.Count < cuantos && Time.time < limite)
            {
                MantenerVida();
                if (actual != null && inicioGolpe < 0f)
                {
                    inicioGolpe = actual.Inicio;
                }
                if (actual == null && avisos.Count > avisosVistos)
                {
                    avisosVistos = avisos.Count;
                    inicioGolpe = -1f;
                    soltarEn = Time.time + TiempoSoltarGuardia;
                }
                if (inicioGolpe >= 0f && !sosteniendo && Time.time >= inicioGolpe + TiempoPulsoGuardia)
                {
                    Press(raton.rightButton);
                    sosteniendo = true;
                }
                if (sosteniendo && inicioGolpe < 0f && Time.time >= soltarEn)
                {
                    Release(raton.rightButton);
                    sosteniendo = false;
                }
                while (vistos < recibidos.Count)
                {
                    resultado.Add(recibidos[vistos]);
                    vistos++;
                }
                yield return null;
            }
            if (sosteniendo)
            {
                Release(raton.rightButton);
            }
            yield return null;
            Assert.GreaterOrEqual(resultado.Count, cuantos, "No juntó los golpes recibidos");
        }

        private static int Perfectos(List<Recibido> golpes, int cuantos)
        {
            int n = 0;
            for (int i = 0; i < cuantos; i++)
            {
                if (golpes[i].Bloqueo == TipoBloqueo.Perfecto)
                {
                    n++;
                }
            }
            return n;
        }

        [UnityTest]
        public IEnumerator Fintas_ElJugadorQueCubreConElTimingDeSerie_BloqueaMenosPerfectosQueSinFintas()
        {
            // Primero sin FINTAS.
            yield return Escenario(1, 1, 0, 0, Escala1, EsperaRapida);
            List<Recibido> base0 = new List<Recibido>();
            yield return BloquearGolpes(GolpesDeBloqueo, base0);
            int perfectosBase = Perfectos(base0, GolpesDeBloqueo);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
            ReiniciarEscenario();

            yield return Escenario(1, 1, 1, 0, Escala1, EsperaRapida);
            System.Random generador = new System.Random(SemillaBloqueo);
            fintas.FijarAzar(() => (float)generador.NextDouble());
            List<Recibido> conFintas = new List<Recibido>();
            yield return BloquearGolpes(GolpesDeBloqueo, conFintas);
            int perfectosFintas = Perfectos(conFintas, GolpesDeBloqueo);
            float fraccionBase = perfectosBase / (float)GolpesDeBloqueo;
            float fraccionFintas = perfectosFintas / (float)GolpesDeBloqueo;
            Debug.Log("[Prueba] bloqueos perfectos en " + GolpesDeBloqueo + " golpes: nivel 0 " + perfectosBase + " (" + fraccionBase.ToString("0.00")
                + "), FINTAS I " + perfectosFintas + " (" + fraccionFintas.ToString("0.00") + "), amagos " + usosFintas);
            Assert.Less(fraccionFintas, fraccionBase, "Con FINTAS el timing de serie ya no basta");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // Para el segundo escenario de una misma prueba: destruye lo creado y limpia el estado.
        private void ReiniciarEscenario()
        {
            Time.timeScale = escalaOriginal;
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }
            creados.Clear();
            if (configPrueba != null)
            {
                Object.DestroyImmediate(configPrueba);
            }
            EventosPelea.LimpiarSuscriptores();
            avisos.Clear();
            recibidos.Clear();
            porCompletar.Clear();
            actual = null;
            usosFintas = 0;
            usosRetirada = 0;
        }

        // Un golpe de serie del jugador soltado "retraso" s después de cada impacto del rival; mide la retirada en el camino.
        private IEnumerator Ensayos(float retraso, int cantidad, List<Ensayo> salida, string etiqueta, bool tomarCaptura)
        {
            float limite = Time.time + LimiteSegundos;
            float recarga = basico.Datos.Mecha.RecargaGolpe;
            float meta = vidaJugador.Datos.AlcanceGolpe + adaptacion.Datos.MargenRetirada - MargenDistancia;
            float maximo = basico.Datos.Mecha.VelocidadMaxima * adaptacion.Datos.FactorVelocidadRetirada[0];
            while (salida.Count < cantidad && Time.time < limite)
            {
                impactoRivalNuevo = false;
                while (!impactoRivalNuevo && Time.time < limite)
                {
                    MantenerVida();
                    yield return null;
                }
                float t0 = ultimoImpactoRival;
                if (golpeJugador.Fase != FaseGolpe.Listo)
                {
                    continue;
                }
                Ensayo e = new Ensayo { Retraso = retraso };
                System.Text.StringBuilder traza = new System.Text.StringBuilder();
                int ultimoDecimo = -1;
                float cuadroMaximo = 0f;
                e.DistanciaInicial = Distancia();
                bool clic = false;
                bool soltar = false;
                impactoVisto = false;
                conectoVisto = false;
                float fin = t0 + recarga + 0.5f;
                while (Time.time < fin || (clic && !impactoVisto && Time.time < fin + 1f))
                {
                    MantenerVida();
                    if (soltar)
                    {
                        Release(raton.leftButton);
                        soltar = false;
                    }
                    if (!clic && Time.time >= t0 + retraso)
                    {
                        Press(raton.leftButton);
                        clic = true;
                        soltar = true;
                    }
                    cuadroMaximo = Mathf.Max(cuadroMaximo, Time.deltaTime);
                    float d = Distancia();
                    if (Mathf.FloorToInt((Time.time - t0) * 10f) != ultimoDecimo)
                    {
                        ultimoDecimo = Mathf.FloorToInt((Time.time - t0) * 10f);
                        traza.Append((Time.time - t0).ToString("0.00")).Append(':').Append(d.ToString("0.00")).Append('/').Append(basico.Velocidad.ToString("0.0"))
                            .Append(basico.Retirandose ? "R " : " ");
                    }
                    e.DistanciaMaxima = Mathf.Max(e.DistanciaMaxima, d);
                    if (Time.time - t0 <= recarga && d >= meta && !e.LlegoALaMeta)
                    {
                        e.LlegoALaMeta = true;
                        e.SegundosHastaMeta = Time.time - t0;
                    }
                    if (basico.Retirandose)
                    {
                        e.VelocidadMaxima = Mathf.Max(e.VelocidadMaxima, Mathf.Abs(basico.Velocidad));
                        if (tomarCaptura && !capturado && basico.Velocidad < -VelocidadDeCaptura)
                        {
                            Capturar();
                        }
                    }
                    yield return null;
                }
                if (soltar)
                {
                    Release(raton.leftButton);
                }
                e.Conecto = conectoVisto;
                if (cuadroMaximo > CuadroDeTironMaximo)
                {
                    // Un tirón del editor (primer golpe, compilación de un shader) deja pasar medio segundo en un cuadro: no mide la regla.
                    Debug.Log("[Prueba] " + etiqueta + ": ensayo descartado por un tirón de " + cuadroMaximo.ToString("0.00") + " s en un cuadro");
                    continue;
                }
                if (!e.LlegoALaMeta)
                {
                    Debug.Log("[Prueba] traza de un ensayo sin llegar a la meta: " + traza);
                }
                Debug.Log("[Prueba] " + etiqueta + " contragolpe a +" + retraso.ToString("0.0") + " s: " + (e.Conecto ? "CONECTA" : "falla")
                    + ", distancia al impacto " + e.DistanciaInicial.ToString("0.00") + " m, máxima " + e.DistanciaMaxima.ToString("0.00")
                    + " m, meta " + meta.ToString("0.00") + " m " + (e.LlegoALaMeta ? "alcanzada a +" + e.SegundosHastaMeta.ToString("0.00") + " s" : "NO alcanzada")
                    + ", velocidad máx " + e.VelocidadMaxima.ToString("0.00") + " m/s (tope " + maximo.ToString("0.00") + ")");
                salida.Add(e);
                yield return null;
            }
            Assert.AreEqual(cantidad, salida.Count, "No juntó los ensayos");
        }

        private float Distancia()
        {
            Vector3 d = basico.transform.position - jugador.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private void Capturar()
        {
            capturado = true;
            Camera camara = Camera.main;
            Assert.IsNotNull(camara, "Falta la cámara de la cabina");
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, 24);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = destino;
            camara.Render();
            camara.targetTexture = anterior;
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            Object.Destroy(destino);
        }

        private static int Conectaron(List<Ensayo> ensayos)
        {
            int n = 0;
            foreach (Ensayo e in ensayos)
            {
                if (e.Conecto)
                {
                    n++;
                }
            }
            return n;
        }

        private IEnumerator RetiradaContra(int liga, int n, bool tomarCaptura)
        {
            yield return Escenario(liga, n, 0, 1, EscalaMedia, EsperaNormal);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Retirada));
            retirada.FijarAzar(() => AzarSiempre);
            // Se mide solo el golpe: la habilidad del jefe lo detiene (Detenido) y se prueba aparte en DonFierro_...
            HabilidadRival habilidadJefe = basico.GetComponent<HabilidadRival>();
            if (habilidadJefe != null)
            {
                habilidadJefe.enabled = false;
            }
            string nombre = basico.Datos.Alias;
            float recarga = basico.Datos.Mecha.RecargaGolpe;
            float maximo = basico.Datos.Mecha.VelocidadMaxima * adaptacion.Datos.FactorVelocidadRetirada[0];

            List<Ensayo> tempranos = new List<Ensayo>();
            yield return Ensayos(ContragolpeTemprano, GolpesMedidos, tempranos, nombre, tomarCaptura);
            List<Ensayo> tardios = new List<Ensayo>();
            yield return Ensayos(ContragolpeTardio, GolpesMedidos, tardios, nombre, false);

            foreach (Ensayo e in tempranos)
            {
                Assert.IsTrue(e.LlegoALaMeta, nombre + ": llega a alcance + margen - 0.1 m antes de acabar su recarga (máxima " + e.DistanciaMaxima.ToString("0.00") + ")");
                Assert.LessOrEqual(e.VelocidadMaxima, maximo + ToleranciaVelocidad, nombre + ": nunca pasa de velocidad máxima x factor");
            }
            foreach (Ensayo e in tardios)
            {
                Assert.LessOrEqual(e.VelocidadMaxima, maximo + ToleranciaVelocidad, nombre + ": nunca pasa de velocidad máxima x factor");
            }
            int temprano = Conectaron(tempranos);
            int tardio = Conectaron(tardios);
            Debug.Log("[Prueba] " + nombre + " RETIRADA I azar 0: contragolpe a +0.3 s conectó " + temprano + " de " + GolpesMedidos + ", a +0.9 s " + tardio + " de "
                + GolpesMedidos + ", retiradas " + usosRetirada + ", recarga " + recarga.ToString("0.0") + " s");
            Assert.AreEqual(GolpesMedidos, temprano, nombre + ": castigar rápido conecta 10 de 10");
            Assert.LessOrEqual(tardio, MaximoTardioQueConecta, nombre + ": castigar tarde conecta 3 de 10 o menos");
            Assert.Greater(usosRetirada, 0);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator RetiradaI_Tornillo_ElContragolpeTardioSeQuedaCortoYTemprano_Conecta()
        {
            yield return RetiradaContra(1, 1, true);
            Assert.IsTrue(File.Exists(RutaCaptura), "Se tomó la captura T-202-1");
        }

        [UnityTest]
        public IEnumerator RetiradaI_ElCacique_ElContragolpeTardioSeQuedaCortoYTemprano_Conecta()
        {
            yield return RetiradaContra(4, 5, false);
        }

        [UnityTest]
        public IEnumerator DonFierro_ConFintasIYRetiradaI_SuHabilidadNoCambia()
        {
            yield return Escenario(1, 5, 1, 1, EscalaMuyRapida, EsperaNormal);
            fintas.FijarAzar(() => AzarNunca);
            retirada.FijarAzar(() => AzarSiempre);
            HabilidadRival habilidad = basico.GetComponent<HabilidadRival>();
            Assert.IsNotNull(habilidad, "DON FIERRO lleva HabilidadRival");
            List<float> inicios = new List<float>();
            List<float> lanzamientos = new List<float>();
            habilidad.AvisoIniciado += () => inicios.Add(Time.time);
            habilidad.Lanzada += () => lanzamientos.Add(Time.time);
            float inicioPelea = Time.time;
            float enfriamiento = habilidad.Datos.Habilidad.Enfriamiento * habilidad.Datos.FactorEnfriamiento;
            float aviso = habilidad.Datos.SegundosAviso;
            float limite = Time.time + LimiteSegundos;
            while ((inicios.Count < 2 || lanzamientos.Count < 1) && Time.time < limite)
            {
                MantenerVida();
                yield return null;
            }
            Assert.GreaterOrEqual(inicios.Count, 2);
            Assert.GreaterOrEqual(lanzamientos.Count, 1);
            Debug.Log("[Prueba] DON FIERRO: aviso " + (lanzamientos[0] - inicios[0]).ToString("0.000") + " s (datos " + aviso.ToString("0.00")
                + "), primer uso a +" + (inicios[0] - inicioPelea).ToString("0.00") + " s, segundo " + (inicios[1] - lanzamientos[0]).ToString("0.00")
                + " s tras el lanzamiento (datos " + enfriamiento.ToString("0.00") + "), amagos " + usosFintas + ", retiradas " + usosRetirada);
            Assert.AreEqual(aviso, lanzamientos[0] - inicios[0], ToleranciaAvisoHabilidad, "El aviso de la habilidad sigue en 0.6 s");
            Assert.AreEqual(enfriamiento, inicios[0] - inicioPelea, ToleranciaEnfriamiento, "Primer uso tras el enfriamiento de siempre");
            Assert.AreEqual(enfriamiento, inicios[1] - lanzamientos[0], ToleranciaEnfriamiento, "El siguiente uso tarda el enfriamiento de siempre");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
