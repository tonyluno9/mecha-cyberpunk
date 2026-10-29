using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha;
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
    /// T-201: el rival que aprendió GUARDIA bloquea los puños del jugador (entrada simulada: clic izquierdo con el ratón).
    /// Atajos permitidos y anotados: niveles fijados en MemoriaRivales antes de preparar la pelea, GuardiaRival.FijarAzar, Time.timeScale,
    /// MultiplicadorEspera del rival para que no ataque mientras se mide, DatosMecha del jugador clonado con la preparación de SINAPSIS II
    /// (ReglaReaccionImplantes), vida restaurada entre golpes y Aturdir llamado como lo hace la DESCARGA.
    /// </summary>
    public class GuardiaRivalPruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabLiga11 = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga1_1.prefab";
        private const string RutaSinapsis2 = "Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_sistemanervioso_2.asset";
        private const string RutaCaptura = "docs/capturas/T-201-1.png";
        private const string IdLiga11 = "DatosRival_Liga1_1";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private const float EscalaRapida = 4f;
        private const float EsperaMaxima = 15f;
        private const float LimiteSegundos = 240f;
        private const float EsperaGolpe = 3f;
        private const float EsperaRivalCalmado = 1f;
        private const float EsperaHuge = 1000f;
        private const float Tolerancia = 1e-3f;
        private const float MitadDeVida = 0.5f;
        private const float AzarSiempre = 0f;
        private const float AzarNunca = 0.99f;
        private const float MargenFraccion = 0.2f;
        private const int GolpesNivelCero = 20;
        private const int GolpesMedidos = 10;
        private const int GolpesNaturales = 40;
        private const int GolpesSituacion = 3;
        private const float SegundosAturdir = 1f;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private class Vigia : MonoBehaviour
        {
            public RivalBasico Rival;
            public GuardiaMecha Guardia;
            public int Violaciones;

            private void Update()
            {
                if (Rival.EnAviso && (Guardia.Levantada || Guardia.Pedida))
                {
                    Violaciones++;
                }
            }
        }

        private class RegistroGolpe
        {
            public bool Conecto;
            public TipoBloqueo Bloqueo;
            public float Danio;
            public bool GuardiaArriba;
            public float ArribaDesdeClic;
            public float ImpactoDesdeClic;
        }

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ConfigFlujoPelea configPrueba;
        private DatosMecha datosClonados;
        private GameObject jugador;
        private VidaMecha vidaJugador;
        private GolpeMecha golpeJugador;
        private DirectorPelea director;
        private MemoriaRivales memoria;
        private AdaptacionRival adaptacion;
        private RivalBasico basico;
        private GuardiaMecha guardiaRival;
        private GuardiaRival cerebroGuardia;
        private VidaMecha vidaRival;
        private Vigia vigia;
        private float escalaOriginal;
        private int usos;
        private bool impactoVisto;
        private bool conectoVisto;
        private ResultadoDanio ultimoResultado;
        private bool guardiaArribaAlConectar;
        private float arribaDesdeAlConectar;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            memoria = null;
            usos = 0;
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += RegistrarMensaje;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarMensaje;
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
            if (datosClonados != null)
            {
                Object.Destroy(datosClonados);
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

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Arena, jugador, memoria con el nivel de GUARDIA pedido y FlujoPelea con TORNILLO (RivalLiga1_1); termina en Peleando.
        private IEnumerator Escenario(int evidenciaGuardia, float escala, float multiplicadorEspera)
        {
            yield return Calentar();
            GameObject servicios = new GameObject("MemoriaRivales");
            creados.Add(servicios);
            memoria = servicios.AddComponent<MemoriaRivales>();
            memoria.Obtener(IdLiga11).evidencias[(int)TipoAdaptacion.Guardia] = evidenciaGuardia;
            memoria.AvisarCambio();

            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador), inicio.position, inicio.rotation);
            creados.Add(jugador);
            vidaJugador = jugador.GetComponent<VidaMecha>();
            golpeJugador = jugador.GetComponent<GolpeMecha>();
            golpeJugador.Impacto += () => impactoVisto = true;
            golpeJugador.Conecto += AlConectar;

            configPrueba = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configPrueba, "segundosPorNumero", 0.3f);
            FijarFloat(configPrueba, "pausaDespuesKO", 0.3f);
            FijarFloat(configPrueba, "segundosTextoPelea", 0.3f);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab"), padre.transform);
            creados.Add(flujo);
            creados.Add(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configPrueba;
            soDirector.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLiga11);
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
            guardiaRival = rival.GetComponent<GuardiaMecha>();
            cerebroGuardia = rival.GetComponent<GuardiaRival>();
            vidaRival = rival.GetComponent<VidaMecha>();
            Assert.IsNotNull(guardiaRival, "El prefab del rival tiene GuardiaMecha");
            Assert.IsNotNull(cerebroGuardia, "El prefab del rival tiene GuardiaRival");
            Assert.IsFalse(guardiaRival.PermitePerfecto, "El rival nunca bloquea perfecto");
            adaptacion.Usada += tipo => { if (tipo == TipoAdaptacion.Guardia) { usos++; } };
            vigia = rival.gameObject.AddComponent<Vigia>();
            vigia.Rival = basico;
            vigia.Guardia = guardiaRival;
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

        private void AlConectar(ResultadoDanio resultado)
        {
            conectoVisto = true;
            ultimoResultado = resultado;
            guardiaArribaAlConectar = guardiaRival.EstaArriba;
            arribaDesdeAlConectar = guardiaRival.EstaArriba ? Time.time - guardiaRival.SegundosArriba : -1f;
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
        }

        // Espera a que el rival esté en Listo y lejos de su próximo golpe, para que su estado no cambie lo que se mide.
        private IEnumerator EsperarRivalCalmado()
        {
            float limite = Time.time + LimiteSegundos;
            ILecturaRival lectura = basico;
            while (Time.time < limite && !(basico.Fase == FaseGolpe.Listo && !guardiaRival.Levantada
                && lectura.SegundosParaAtacar >= EsperaRivalCalmado))
            {
                yield return null;
            }
        }

        // Un clic izquierdo con el golpe del jugador listo; espera al impacto y devuelve lo que pasó.
        private IEnumerator Golpear(RegistroGolpe registro)
        {
            float limite = Time.time + LimiteSegundos;
            while (golpeJugador.Fase != FaseGolpe.Listo && Time.time < limite)
            {
                yield return null;
            }
            MantenerVida();
            impactoVisto = false;
            conectoVisto = false;
            float clic = Time.time;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float fin = Time.time + EsperaGolpe;
            while (!impactoVisto && Time.time < fin)
            {
                yield return null;
            }
            registro.Conecto = conectoVisto;
            registro.ImpactoDesdeClic = Time.time - clic;
            if (conectoVisto)
            {
                registro.Bloqueo = ultimoResultado.Bloqueo;
                registro.Danio = ultimoResultado.DanioAplicado;
                registro.GuardiaArriba = guardiaArribaAlConectar;
                registro.ArribaDesdeClic = guardiaArribaAlConectar ? arribaDesdeAlConectar - clic : -1f;
            }
        }

        // Golpes con el rival calmado hasta juntar "cantidad" que conecten.
        private IEnumerator GolpearConectando(int cantidad, List<RegistroGolpe> resultados)
        {
            float limite = Time.time + LimiteSegundos;
            while (resultados.Count < cantidad && Time.time < limite)
            {
                yield return EsperarRivalCalmado();
                RegistroGolpe registro = new RegistroGolpe();
                yield return Golpear(registro);
                if (registro.Conecto)
                {
                    resultados.Add(registro);
                }
            }
            Assert.AreEqual(cantidad, resultados.Count, "No juntó los golpes que conectan");
        }

        private static int Bloqueados(List<RegistroGolpe> golpes)
        {
            int n = 0;
            foreach (RegistroGolpe g in golpes)
            {
                if (g.Bloqueo != TipoBloqueo.Ninguno)
                {
                    n++;
                }
            }
            return n;
        }

        private static void Imprimir(string etiqueta, List<RegistroGolpe> golpes)
        {
            foreach (RegistroGolpe g in golpes)
            {
                Debug.Log("[Prueba] " + etiqueta + ": guardia arriba " + (g.GuardiaArriba ? "a +" + g.ArribaDesdeClic.ToString("0.000") + " s del clic"
                    : "no") + ", impacto a +" + g.ImpactoDesdeClic.ToString("0.000") + " s, " + g.Bloqueo + ", daño " + g.Danio.ToString("0.0"));
            }
        }

        [UnityTest]
        public IEnumerator NivelCero_VeinteGolpesQueConectan_NingunoSeBloquea()
        {
            yield return Escenario(0, EscalaRapida, EsperaHuge);
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Guardia));
            List<RegistroGolpe> golpes = new List<RegistroGolpe>();
            yield return GolpearConectando(GolpesNivelCero, golpes);
            Assert.AreEqual(0, Bloqueados(golpes), "Sin GUARDIA nada se bloquea");
            Assert.AreEqual(0, usos);
            Assert.AreEqual(0, vigia.Violaciones);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_AzarCero_GolpeDeSerie_DiezDeDiezBloqueadosNormal()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia));
            cerebroGuardia.FijarAzar(() => AzarSiempre);
            DatosMecha datosRival = vidaRival.Datos;
            float esperado = vidaJugador.Datos.DanioGolpe * (1f - datosRival.ReduccionBloqueo);
            List<RegistroGolpe> golpes = new List<RegistroGolpe>();
            yield return GolpearConectando(GolpesMedidos, golpes);
            Imprimir("serie", golpes);
            foreach (RegistroGolpe g in golpes)
            {
                Assert.AreEqual(TipoBloqueo.Normal, g.Bloqueo, "Siempre Normal, nunca Perfecto");
                Assert.IsTrue(g.GuardiaArriba, "La guardia estaba arriba en el impacto");
                Assert.AreEqual(esperado, g.Danio, Tolerancia, "Daño por (1 - ReduccionBloqueo)");
            }
            Assert.AreEqual(GolpesMedidos, usos, "Usada(Guardia) una vez por golpe bloqueado");
            Assert.AreEqual(0, vigia.Violaciones);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_AzarCero_ConSinapsis2_NingunoBloqueado()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            cerebroGuardia.FijarAzar(() => AzarSiempre);
            DatosImplante sinapsis2 = AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaSinapsis2);
            Assert.IsNotNull(sinapsis2);
            ValoresMecha valores = ReglaReaccionImplantes.Aplicar(vidaJugador.Datos.Valores, sinapsis2.Efectos.multiplicadorReaccionMecha);
            Assert.Less(valores.preparacionGolpe, vidaJugador.Datos.PreparacionGolpe, "SINAPSIS II acorta la preparación");
            datosClonados = DatosMecha.CrearEnEjecucion(valores);
            vidaJugador.CambiarDatos(datosClonados);
            List<RegistroGolpe> golpes = new List<RegistroGolpe>();
            yield return GolpearConectando(GolpesMedidos, golpes);
            Imprimir("SINAPSIS II", golpes);
            Assert.AreEqual(0, Bloqueados(golpes), "Más rápido que su reacción: ninguno se bloquea");
            Assert.AreEqual(0, usos);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_Azar099_NingunoBloqueado()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            cerebroGuardia.FijarAzar(() => AzarNunca);
            List<RegistroGolpe> golpes = new List<RegistroGolpe>();
            yield return GolpearConectando(GolpesMedidos, golpes);
            Assert.AreEqual(0, Bloqueados(golpes));
            Assert.AreEqual(0, usos);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_AzarNatural_FraccionBloqueadaCercaDeLaProbabilidad()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            float probabilidad = adaptacion.Datos.ProbabilidadGuardia[0];
            List<RegistroGolpe> golpes = new List<RegistroGolpe>();
            yield return GolpearConectando(GolpesNaturales, golpes);
            int bloqueados = Bloqueados(golpes);
            float fraccion = (float)bloqueados / GolpesNaturales;
            Debug.Log("[Prueba] azar natural: " + bloqueados + " de " + GolpesNaturales + " bloqueados, fracción " + fraccion.ToString("0.00")
                + ", probabilidadGuardia " + probabilidad.ToString("0.00"));
            Assert.AreEqual(probabilidad, fraccion, MargenFraccion);
            Assert.AreEqual(bloqueados, usos, "Usada(Guardia) una vez por golpe bloqueado");
            foreach (RegistroGolpe g in golpes)
            {
                Assert.AreNotEqual(TipoBloqueo.Perfecto, g.Bloqueo);
            }
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_GolpeMientrasRivalPreparaORecupera_NuncaSeBloqueaYNuncaAvisaConGuardia()
        {
            yield return Escenario(1, 1f, 1f);
            cerebroGuardia.FijarAzar(() => AzarSiempre);
            int probados = 0;
            float limite = Time.time + LimiteSegundos;
            FaseGolpe[] fases = { FaseGolpe.Preparando, FaseGolpe.Recuperando };
            foreach (FaseGolpe fase in fases)
            {
                int hechos = 0;
                while (hechos < GolpesSituacion && Time.time < limite)
                {
                    MantenerVida();
                    // Con el golpe del jugador listo, espera a que el rival esté en la fase pedida y suelta el golpe en ese instante.
                    while (Time.time < limite && !(basico.Fase == fase && golpeJugador.Fase == FaseGolpe.Listo))
                    {
                        yield return null;
                    }
                    RegistroGolpe registro = new RegistroGolpe();
                    FaseGolpe faseAlClic = basico.Fase;
                    yield return Golpear(registro);
                    if (registro.Conecto && faseAlClic == fase)
                    {
                        Assert.AreEqual(TipoBloqueo.Ninguno, registro.Bloqueo, "No se cubre mientras " + fase + " su golpe");
                        hechos++;
                        probados++;
                    }
                }
                Assert.AreEqual(GolpesSituacion, hechos, "Golpes sueltos con el rival en " + fase);
            }
            Assert.AreEqual(GolpesSituacion * 2, probados);
            Assert.AreEqual(0, vigia.Violaciones, "El rival nunca entró en aviso con la guardia levantada");
            Assert.AreEqual(0, usos);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_MartilloEntraCompletoYAturdirBajaLaGuardia()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            cerebroGuardia.FijarAzar(() => AzarSiempre);
            yield return EsperarRivalCalmado();
            MantenerVida();
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float limite = Time.time + EsperaGolpe;
            while (!guardiaRival.EstaArriba && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsTrue(guardiaRival.EstaArriba, "La guardia del rival subió");

            // El MARTILLO rompe guardia: entra completo aunque esté arriba.
            float danio = vidaJugador.Datos.DanioGolpe;
            ResultadoDanio martillo = vidaRival.RecibirDanioSinGuardia(new InfoDanio(danio, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual(TipoBloqueo.Ninguno, martillo.Bloqueo);
            Assert.AreEqual(danio, martillo.DanioAplicado, Tolerancia);

            // La DESCARGA aturde: la guardia baja en el acto.
            PeleadorMecha peleador = vidaRival.GetComponent<PeleadorMecha>();
            peleador.Aturdir(SegundosAturdir);
            Assert.IsTrue(peleador.EstaAturdido);
            Assert.IsFalse(guardiaRival.Levantada, "Aturdir baja la guardia");
            Assert.IsFalse(guardiaRival.Pedida);
            Assert.AreEqual(EstadoGuardiaRival.Abajo, cerebroGuardia.Estado);
            yield return Esperar(SegundosAturdir + 0.2f);
            Assert.IsFalse(peleador.EstaAturdido);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator GuardiaI_ConGuardiaArriba_BrazoEnPoseYColorYCaptura()
        {
            yield return Escenario(1, 1f, EsperaHuge);
            cerebroGuardia.FijarAzar(() => AzarSiempre);
            ConfigPresentacionRival config = AssetDatabase.LoadAssetAtPath<ConfigPresentacionRival>("Assets/_Proyecto/IA/Datos/ConfigPresentacionRival_Base.asset");
            Quaternion reposo = basico.Brazo.localRotation;
            yield return EsperarRivalCalmado();
            MantenerVida();
            impactoVisto = false;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float limite = Time.time + EsperaGolpe;
            while (!impactoVisto && Time.time < limite)
            {
                yield return null;
            }
            // El golpe ya salió; la guardia sigue arriba unos 0.3 s más.
            yield return null;
            yield return null;
            Assert.IsTrue(guardiaRival.EstaArriba);
            Assert.AreEqual(config.AnguloBrazoGuardia, Quaternion.Angle(reposo, basico.Brazo.localRotation), 0.5f, "Pose de guardia");
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            basico.PiezasBrazo[0].GetPropertyBlock(bloque);
            Color color = bloque.GetColor(Shader.PropertyToID("_BaseColor"));
            Assert.AreEqual(config.ColorGuardia.r, color.r, 0.02f);
            Assert.AreEqual(config.ColorGuardia.g, color.g, 0.02f);
            Assert.AreEqual(config.ColorGuardia.b, color.b, 0.02f);

            // WaitForEndOfFrame no corre en batchmode: se renderiza la cámara del jugador a una textura y se guarda.
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
            Assert.IsTrue(File.Exists(RutaCaptura));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
