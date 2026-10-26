using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-177: la pantalla de habilidades del tablero. Prefab real MechaEstacionado.prefab (arranque Directo, con piloto), un
    /// ServicioInventario y entrada simulada (teclas 1 y 2). La pelea se marca con EventosPelea.PublicarInicio y PublicarFin.
    /// </summary>
    public class PantallaHabilidadesPruebas : InputTestFixture
    {
        private const string RutaEstacionado = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaHabilidades_Base.asset";
        private const string VariableCaptura = "CAPTURAR_T177";
        private const string RutaCaptura1 = "docs/capturas/T-177-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-177-2.png";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EnfriamientoMisil = 8f;
        private const float EsperaMaxima = 12f;
        private const float ToleranciaBarra = 0.15f;
        private const float SegundosMedicion = 3f;
        private const float SegundosCalentamientoMedidor = 0.5f;
        private const float MargenEnfriamiento = 0.2f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private GameObject raizInventario;
        private ServicioInventario inventario;
        private CatalogoPiezas catalogo;
        private ConfigPantallaHabilidades config;
        private EquipoMecha equipo;
        private UsoHabilidades uso;
        private LanzadorHabilidades lanzador;
        private PantallaHabilidadesCabina pantalla;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaHabilidades>(RutaConfig);
            Assert.IsNotNull(catalogo, "Existe CatalogoPiezas_Base");
            Assert.IsNotNull(config, "Existe ConfigPantallaHabilidades_Base");
            errores.Clear();
            Application.logMessageReceived += AlRegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlRegistrarLog;
            if (mecha != null) Object.DestroyImmediate(mecha);
            if (piso != null) Object.Destroy(piso);
            if (raizInventario != null) Object.Destroy(raizInventario);
            foreach (ProyectilHabilidad p in Object.FindObjectsByType<ProyectilHabilidad>(FindObjectsSortMode.None)) Object.DestroyImmediate(p.gameObject);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private void AlRegistrarLog(string texto, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(texto);
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
            while (Time.time < fin) yield return null;
        }

        private IEnumerator Escenario()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            var padre = new GameObject("PadreApagado");
            padre.SetActive(false);
            mecha = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaEstacionado), new Vector3(0f, 0.05f, 0f), Quaternion.identity, padre.transform);
            var so = new SerializedObject(mecha.GetComponent<MontajeMecha>());
            so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha.transform.SetParent(null);
            Object.Destroy(padre);
            raizInventario = new GameObject("InventarioPrueba");
            inventario = raizInventario.AddComponent<ServicioInventario>();
            equipo = mecha.GetComponent<EquipoMecha>();
            uso = mecha.GetComponent<UsoHabilidades>();
            lanzador = mecha.GetComponent<LanzadorHabilidades>();
            pantalla = mecha.GetComponentInChildren<PantallaHabilidadesCabina>(true);
            Assert.IsNotNull(uso, "El mecha lleva UsoHabilidades");
            Assert.IsNotNull(pantalla, "El mecha lleva la pantalla de habilidades");
            yield return Calentar();
        }

        private DatosPieza Pieza(string id)
        {
            DatosPieza p = catalogo.Buscar(id);
            Assert.IsNotNull(p, "Existe la pieza " + id);
            return p;
        }

        private IEnumerator Tecla(Key tecla)
        {
            KeyControl control = teclado[tecla];
            Press(control);
            yield return null;
            Release(control);
            yield return null;
        }

        private static int Segundos(string texto)
        {
            return int.Parse(texto.Replace(" s", string.Empty));
        }

        [UnityTest]
        public IEnumerator PiezasDeSerie_LaPantallaEstaApagada()
        {
            yield return Escenario();
            EventosPelea.PublicarInicio();
            yield return null;
            yield return null;

            Assert.IsFalse(pantalla.Visible, "Sin habilidades la pantalla no se ve");
            Assert.IsFalse(pantalla.FranjaVisible);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator SinUsoHabilidades_LaPantallaSeQuedaApagadaSinErrores()
        {
            var objeto = new GameObject("PantallaSola");
            objeto.SetActive(false);
            PantallaHabilidadesCabina sola = objeto.AddComponent<PantallaHabilidadesCabina>();
            objeto.SetActive(true);
            yield return null;
            yield return null;

            Assert.IsFalse(sola.enabled, "Se apaga sola");
            Assert.IsFalse(sola.Visible);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
            Object.Destroy(objeto);
        }

        [UnityTest]
        public IEnumerator MisilMilitarI_MuestraLaFila_ElDisparoBajaLaMunicion_YLaBarraBajaDeOchoACero()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 2);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return null;

            Assert.IsTrue(pantalla.Visible, "Con una habilidad la pantalla se ve");
            Assert.AreEqual("1 · MISIL I · ×2", pantalla.TextoFila(0));
            Assert.AreEqual("LISTO", pantalla.TextoEstado(0));
            Assert.AreEqual(config.ColorListo, pantalla.ColorEstado(0));
            Assert.AreEqual(brazo.Habilidad.ColorRama, pantalla.ColorFila(0), "El nombre va en el color de la rama");
            Assert.AreEqual("2 ·", pantalla.TextoFila(1), "La ranura sin habilidad queda vacía");
            Assert.AreEqual("—", pantalla.TextoEstado(1));
            Assert.AreEqual(string.Empty, pantalla.TextoPie);

            float t0 = Time.time;
            yield return Tecla(Key.Digit1);
            Assert.AreEqual("1 · MISIL I · ×1", pantalla.TextoFila(0), "Gastó una munición");
            Assert.AreEqual(EnfriamientoMisil, Segundos(pantalla.TextoEstado(0)), 1f, "Arranca en 8 s");
            Assert.AreEqual(1f, pantalla.Barra(0), ToleranciaBarra, "La barra arranca llena");

            yield return Esperar(4f - (Time.time - t0));
            int mitad = Segundos(pantalla.TextoEstado(0));
            Assert.AreEqual(4, mitad, 1, "A los 4 s faltan unos 4 s");
            Assert.AreEqual(0.5f, pantalla.Barra(0), ToleranciaBarra, "La barra va por la mitad");

            yield return Esperar(EnfriamientoMisil + MargenEnfriamiento - (Time.time - t0));
            Assert.AreEqual("LISTO", pantalla.TextoEstado(0), "Terminó el enfriamiento");
            Assert.AreEqual(0f, pantalla.Barra(0), 0.001f, "La barra quedó vacía");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator SinMunicion_MuestraSinMunicion_YConLasDosRanurasVacias_SoloBrazos()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            DatosPieza torso = Pieza("pieza_torso_tech_1");
            equipo.Equipar(brazo);
            inventario.Agregar(torso.Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return null;

            Assert.AreEqual("1 · MISIL I · ×0", pantalla.TextoFila(0));
            Assert.AreEqual("SIN MUNICIÓN", pantalla.TextoEstado(0));
            Assert.AreEqual(config.ColorSinMunicion, pantalla.ColorEstado(0), "Naranja suave");
            Assert.AreEqual(string.Empty, pantalla.TextoPie, "Con una sola ranura con habilidad no avisa");

            equipo.Equipar(torso);
            yield return null;
            yield return null;
            Assert.AreEqual("2 · ESCUDO I · ×3", pantalla.TextoFila(1));
            Assert.AreEqual("LISTO", pantalla.TextoEstado(1));
            Assert.AreEqual(string.Empty, pantalla.TextoPie, "Una ranura todavía tiene munición");

            inventario.Quitar(torso.Municion, 3);
            yield return null;
            yield return null;
            Assert.AreEqual("SIN MUNICIÓN", pantalla.TextoEstado(1));
            Assert.AreEqual("SOLO BRAZOS", pantalla.TextoPie, "Las dos ranuras sin munición");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator FueraDePelea_LasFilasSalenEnGris_YEnPeleaVuelvenAlColor()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 2);
            yield return null;
            yield return null;

            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(config.ColorGris, pantalla.ColorFila(0), "Fila gris fuera de pelea");
            Assert.AreEqual(config.ColorGris, pantalla.ColorEstado(0), "Estado gris fuera de pelea");

            EventosPelea.PublicarInicio();
            yield return null;
            yield return null;
            Assert.AreEqual(brazo.Habilidad.ColorRama, pantalla.ColorFila(0));
            Assert.AreEqual(config.ColorListo, pantalla.ColorEstado(0));

            EventosPelea.PublicarFin(new ResultadoPelea(LadoPelea.Jugador, 1f));
            yield return null;
            yield return null;
            Assert.AreEqual(config.ColorGris, pantalla.ColorFila(0), "Al terminar la pelea vuelve a gris");
        }

        [UnityTest]
        public IEnumerator EscudoActivo_PoneTurquesaElMarcoYLaFranja_YAlTerminarVuelveAlColorNormal()
        {
            yield return Escenario();
            DatosPieza torso = Pieza("pieza_torso_tech_1");
            equipo.Equipar(torso);
            inventario.Agregar(torso.Municion, 2);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return null;
            Assert.AreEqual(config.ColorMarco, pantalla.ColorMarco, "Marco normal sin escudo");
            Assert.IsFalse(pantalla.FranjaVisible);

            yield return Tecla(Key.Digit2);
            yield return null;
            Assert.IsTrue(lanzador.EscudoActivo, "El escudo se activó");
            Assert.AreEqual(config.ColorEscudo, pantalla.ColorMarco, "Marco turquesa con el escudo");
            Assert.AreEqual(config.ColorEscudo, pantalla.ColorFranja);
            Assert.IsTrue(pantalla.FranjaVisible, "La franja del cristal se ve");

            float limite = Time.time + EsperaMaxima;
            while (lanzador.EscudoActivo && Time.time < limite) yield return null;
            yield return null;
            Assert.IsFalse(lanzador.EscudoActivo, "El escudo terminó");
            Assert.AreEqual(config.ColorMarco, pantalla.ColorMarco, "Vuelve al color normal");
            Assert.IsFalse(pantalla.FranjaVisible, "La franja se apaga");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator BarraCorriendo_SinTironesYSinBasuraPorCuadro()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 2);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return Tecla(Key.Digit1);
            yield return Calentar();
            Assert.Greater(uso.EnfriamientoRestante(RanuraPieza.BrazoIzquierdo), SegundosMedicion + 1f, "El enfriamiento sigue corriendo");

            var medidor = new MedidorFrames(SegundosCalentamientoMedidor);
            long bytes = 0;
            int cuadros = 0;
            int cambiosDeSegundo = 0;
            string anterior = pantalla.TextoEstado(0);
            float fin = Time.time + SegundosMedicion;
            while (Time.time < fin)
            {
                yield return null;
                medidor.Registrar(Time.unscaledDeltaTime);
                long antes = GC.GetAllocatedBytesForCurrentThread();
                pantalla.Refrescar();
                bytes += GC.GetAllocatedBytesForCurrentThread() - antes;
                cuadros++;
                if (pantalla.TextoEstado(0) != anterior)
                {
                    cambiosDeSegundo++;
                    anterior = pantalla.TextoEstado(0);
                }
            }

            Assert.Greater(cuadros, 10, "Se midieron cuadros");
            Assert.GreaterOrEqual(cambiosDeSegundo, 2, "Los segundos cambiaron durante la medición");
            Assert.AreEqual(0L, bytes, "Refrescar con la barra corriendo no crea objetos");
            Assert.IsTrue(medidor.Cumple, medidor.Resumen());
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        private IEnumerator Capturar(string ruta)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)) || Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return null;
            Camera camara = mecha.GetComponentInChildren<Camera>();
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = destino;
            camara.Render();
            camara.targetTexture = anterior;
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            destino.Release();
            Object.Destroy(destino);
        }

        [UnityTest]
        public IEnumerator Captura_MisilEnfriandoYEscudoListo_GuardaT177Uno()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)) || Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_2");
            DatosPieza torso = Pieza("pieza_torso_tech_2");
            equipo.Equipar(brazo);
            equipo.Equipar(torso);
            inventario.Agregar(brazo.Municion, 3);
            inventario.Agregar(torso.Municion, 2);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return Tecla(Key.Digit1);
            yield return Esperar(2f);
            yield return Capturar(RutaCaptura1);
        }

        [UnityTest]
        public IEnumerator Captura_SinMunicionYSoloBrazos_GuardaT177Dos()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)) || Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return Escenario();
            equipo.Equipar(Pieza("pieza_brazoizq_militar_2"));
            equipo.Equipar(Pieza("pieza_torso_tech_2"));
            EventosPelea.PublicarInicio();
            yield return Esperar(0.5f);
            Assert.AreEqual("SOLO BRAZOS", pantalla.TextoPie);
            yield return Capturar(RutaCaptura2);
        }
    }
}
