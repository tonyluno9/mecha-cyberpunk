using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// Pruebas jugables de T-069: piso, MechaJugador real y 3 RivalPandilla con control, sobre el jugador en el origen
    /// mirando a +Z. Los pandilleros nacen en un arco a 8 m.
    /// </summary>
    public class PandillaPruebas : InputTestFixture
    {
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabPandilla = "Assets/_Proyecto/IA/Prefabs/RivalPandilla.prefab";
        private const string RutaCaptura = "docs/capturas/T-069-1.png";
        private const string VariableCaptura = "CAPTURAR_T069";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float AlturaPiso = 0.05f;
        private const float RadioNacimiento = 8f;
        private const float EscalaTiempo = 4f;
        private const float SegundosDePelea = 60f;
        private const float SegundosParaVerTurnos = 30f;
        private const float GraciaSinTurno = 0.8f;
        private const float EsperaBusqueda = 0.6f;
        private const float LimiteGanar = 90f;
        private const float SegundosEntreClics = 0.1f;
        private const float MargenVida = 1e-3f;
        private const float SegundosAntesDeCaptura = 4f;
        private const float LimiteEsperaCaptura = 5f;
        private const float MargenCapturaAnillo = 1.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.1f;
        private static readonly float[] AngulosArco = { -35f, 0f, 35f };

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private VidaMecha vidaJugador;
        private float escalaOriginal;
        private readonly List<GameObject> pandilleros = new List<GameObject>();
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            TurnosAtaque.Compartido.Limpiar();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += RegistrarError;
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarError;
            foreach (GameObject pandillero in pandilleros)
            {
                if (pandillero != null)
                {
                    Object.Destroy(pandillero);
                }
            }
            pandilleros.Clear();
            Object.Destroy(jugador);
            Object.Destroy(piso);
            TurnosAtaque.Compartido.Limpiar();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
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

        private void CrearJugador()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
            jugador = Object.Instantiate(prefab, new Vector3(0f, AlturaPiso, 0f), Quaternion.identity);
            vidaJugador = jugador.GetComponent<VidaMecha>();
        }

        private void CrearPandilleros()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPandilla);
            foreach (float angulo in AngulosArco)
            {
                Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
                GameObject pandillero = Object.Instantiate(prefab, direccion * RadioNacimiento + Vector3.up * AlturaPiso,
                    Quaternion.LookRotation(-direccion));
                pandilleros.Add(pandillero);
            }
        }

        // Tras un frame ya corrió el Start de cada pandillero (que les quita el control): se lo devolvemos.
        private IEnumerator DarControl()
        {
            yield return null;
            foreach (GameObject pandillero in pandilleros)
            {
                pandillero.GetComponent<PeleadorMecha>().ActivarControl(true);
            }
            yield return null;
        }

        private IEnumerator Escenario()
        {
            yield return Calentar();
            CrearJugador();
            CrearPandilleros();
            yield return DarControl();
        }

        private static float DistanciaPlana(Transform a, Transform b)
        {
            Vector3 d = a.position - b.position;
            d.y = 0f;
            return d.magnitude;
        }

        private int EnPreparacion()
        {
            int cuenta = 0;
            foreach (GameObject pandillero in pandilleros)
            {
                if (pandillero.GetComponent<RivalBasico>().EnAviso)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Turnos_JugadorQuieto60s_NuncaDosEnPreparacionNiMuereNiSeHierenEntreEllos()
        {
            yield return Escenario();
            Time.timeScale = EscalaTiempo;
            int danoEntreEllos = 0;
            int golpesAlJugador = 0;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                if (objetivo != null && objetivo.Lado == LadoPelea.Rival && info.Atacante != null
                    && info.Atacante.Lado == LadoPelea.Rival)
                {
                    danoEntreEllos++;
                }
                if (objetivo != null && objetivo.Lado == LadoPelea.Jugador && resultado.DanioAplicado > 0f)
                {
                    golpesAlJugador++;
                }
            };

            int maximoEnPreparacion = 0;
            int golpesPreparados = 0;
            bool habiaAviso = false;
            float minima = vidaJugador.Maxima;
            float fin = Time.time + SegundosDePelea;
            while (Time.time < fin)
            {
                yield return null;
                int ahora = EnPreparacion();
                maximoEnPreparacion = Mathf.Max(maximoEnPreparacion, ahora);
                if (ahora > 0 && !habiaAviso)
                {
                    golpesPreparados++;
                }
                habiaAviso = ahora > 0;
                minima = Mathf.Min(minima, vidaJugador.Actual);
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó en KO");
            }

            Assert.LessOrEqual(maximoEnPreparacion, 1, "Hubo dos pandilleros preparando golpe a la vez");
            Assert.Greater(golpesPreparados, 3, "Sí hubo pelea: se prepararon varios golpes");
            Assert.Greater(golpesAlJugador, 3, "Sí hubo pelea: golpearon al jugador");
            Assert.Greater(minima, 0f);
            Assert.AreEqual(0, danoEntreEllos, "Un pandillero hirió a otro");
            foreach (GameObject pandillero in pandilleros)
            {
                VidaMecha vida = pandillero.GetComponent<VidaMecha>();
                Assert.AreEqual(vida.Maxima, vida.Actual, MargenVida, "Un pandillero recibió daño");
            }
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Turnos_LosQueNoTienenTurno_SeQuedanMasLejosQueSuDistanciaDeAtaque()
        {
            yield return Escenario();
            Time.timeScale = EscalaTiempo;
            float ataque = pandilleros[0].GetComponent<RivalBasico>().Datos.DistanciaAtaque;
            float[] sinTurnoDesde = { -1f, -1f, -1f };
            int medidos = 0;
            HashSet<int> conTurno = new HashSet<int>();

            float fin = Time.time + SegundosParaVerTurnos;
            while (Time.time < fin)
            {
                yield return null;
                for (int i = 0; i < pandilleros.Count; i++)
                {
                    IPeleador peleador = pandilleros[i].GetComponent<PeleadorMecha>();
                    if (TurnosAtaque.Compartido.Tiene(peleador))
                    {
                        sinTurnoDesde[i] = -1f;
                        conTurno.Add(i);
                        continue;
                    }
                    if (sinTurnoDesde[i] < 0f)
                    {
                        sinTurnoDesde[i] = Time.time;
                    }
                    // Un recién soltado todavía está retrocediendo hacia su anillo: se le da un momento.
                    if (Time.time - sinTurnoDesde[i] > GraciaSinTurno)
                    {
                        float distancia = DistanciaPlana(pandilleros[i].transform, jugador.transform);
                        Assert.Greater(distancia, ataque, "El pandillero " + i + " sin turno está dentro de su distancia de ataque");
                        medidos++;
                    }
                }
            }

            Assert.AreEqual(pandilleros.Count, conTurno.Count, "Los 3 pandilleros tuvieron su turno");
            Assert.Greater(medidos, 0, "Sí se midió a quienes esperaban");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Busqueda_PandillerosCreadosAntesQueElJugador_LoEncuentranAlAparecer()
        {
            yield return Calentar();
            CrearPandilleros();
            yield return DarControl();
            yield return Esperar(1f);
            foreach (GameObject pandillero in pandilleros)
            {
                Assert.IsNull(pandillero.GetComponent<RivalBasico>().Objetivo, "Todavía no hay a quién perseguir");
            }

            CrearJugador();
            yield return Esperar(EsperaBusqueda);

            foreach (GameObject pandillero in pandilleros)
            {
                Assert.AreSame(jugador, pandillero.GetComponent<RivalBasico>().Objetivo, "No encontró al jugador a tiempo");
            }
        }

        [UnityTest]
        public IEnumerator Jugador_MiraAlMasCercanoYHaceClicSeguido_DejaALos3EnKOEnMenosDe90s()
        {
            yield return Escenario();
            Time.timeScale = EscalaTiempo;

            float fin = Time.time + LimiteGanar;
            while (Time.time < fin && !TodosEnKO())
            {
                GameObject masCercano = MasCercanoEnPie();
                if (masCercano != null)
                {
                    Vector3 hacia = masCercano.transform.position - jugador.transform.position;
                    hacia.y = 0f;
                    jugador.transform.rotation = Quaternion.LookRotation(hacia);
                }
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return Esperar(SegundosEntreClics);
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó en KO");
            }

            Assert.IsTrue(TodosEnKO(), "No quedaron los 3 pandilleros en KO a tiempo");
            Assert.IsFalse(vidaJugador.EstaKO);
            Assert.IsNull(TurnosAtaque.Compartido.Dueno(LadoPelea.Rival), "Con todos en KO el turno queda libre");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        private bool TodosEnKO()
        {
            foreach (GameObject pandillero in pandilleros)
            {
                if (!pandillero.GetComponent<VidaMecha>().EstaKO)
                {
                    return false;
                }
            }
            return true;
        }

        private GameObject MasCercanoEnPie()
        {
            GameObject mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (GameObject pandillero in pandilleros)
            {
                if (pandillero.GetComponent<VidaMecha>().EstaKO)
                {
                    continue;
                }
                float distancia = DistanciaPlana(pandillero.transform, jugador.transform);
                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejor = pandillero;
                }
            }
            return mejor;
        }

        private bool TodosEnElAnillo()
        {
            foreach (GameObject pandillero in pandilleros)
            {
                RivalBasico rival = pandillero.GetComponent<RivalBasico>();
                if (DistanciaPlana(pandillero.transform, jugador.transform) > rival.Datos.DistanciaAtaque + MargenCapturaAnillo * 2f)
                {
                    return false;
                }
            }
            return true;
        }

        // El jugador gira hacia el promedio de las posiciones de los pandilleros, para que los tres queden en pantalla.
        private void MirarAlCentroDelGrupo()
        {
            Vector3 suma = Vector3.zero;
            foreach (GameObject pandillero in pandilleros)
            {
                Vector3 hacia = pandillero.transform.position - jugador.transform.position;
                hacia.y = 0f;
                suma += hacia.normalized;
            }
            if (suma.sqrMagnitude > 0.01f)
            {
                jugador.transform.rotation = Quaternion.LookRotation(suma);
            }
        }

        // Alguno está más cerca que el anillo de espera (va a golpear o acaba de hacerlo).
        private bool HayAlgunoCerca()
        {
            foreach (GameObject pandillero in pandilleros)
            {
                RivalBasico rival = pandillero.GetComponent<RivalBasico>();
                if (DistanciaPlana(pandillero.transform, jugador.transform) < rival.Datos.DistanciaAtaque + MargenCapturaAnillo)
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator Captura_TresPandillerosRodeandoAlJugador_SeVeDesdeLaCabina()
        {
            yield return Escenario();
            // Se captura en cuanto los tres llegan a su anillo y ninguno prepara golpe (el brazo echado atrás taparía la vista).
            float limite = Time.time + SegundosAntesDeCaptura * LimiteEsperaCaptura;
            while (Time.time < limite && (EnPreparacion() > 0 || HayAlgunoCerca() || !TodosEnElAnillo()))
            {
                yield return null;
            }
            MirarAlCentroDelGrupo();
            yield return null;

            Camera camara = jugador.GetComponent<ControladorMecha>().Camara.GetComponent<Camera>();
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            if (System.Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            }
            Object.Destroy(rt);
            Object.Destroy(imagen);

            Assert.Greater(maximo - minimo, VariacionMinima, "La imagen de la cabina es plana: no se ve nada");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
