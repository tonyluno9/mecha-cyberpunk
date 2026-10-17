using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Kits;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-148: kits de reparación en plena pelea con el flujo real (arena de prueba, MechaJugador, ServiciosEconomia y FlujoPelea).
    /// El rival pierde el control tras el conteo para que la vida medida no cambie por golpes.
    /// </summary>
    public class KitReparacionPruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string RutaCaptura = "docs/capturas/T-148-1.png";
        private const string VariableCaptura = "CAPTURAR_T148";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorNumero = 0.5f;
        private const float PausaKO = 0.3f;
        private const float EsperaMaxima = 15f;
        private const float DanioMortal = 100000f;
        private const float VidaBaja = 50f;
        private const float MargenVida = 0.5f;
        private const float SegundosCuracionMinimo = 1.4f;
        private const float SegundosCuracionMaximo = 1.7f;
        private const int CuadrosMinimos = 5;
        private const int KitsIniciales = 3;
        private const float SegundosAMedioCurar = 0.5f;
        private const float MitadCentro = 0.2f;
        private const float CentroVista = 0.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);
        private static readonly float[] Aspectos = { 16f / 9f, 4f / 3f };

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ConfigFlujoPelea config;
        private DirectorPelea director;
        private ServicioInventario inventario;
        private DatosObjeto kit;
        private GameObject jugador;
        private VidaMecha vida;
        private PantallaKitsCabina pantalla;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            errores.Clear();
            Application.logMessageReceived += RegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarLog;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null) UnityEngine.Object.Destroy(objeto);
            }
            creados.Clear();
            if (config != null) UnityEngine.Object.Destroy(config);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private void RegistrarLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
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

        private GameObject Crear(string ruta, Vector3 posicion)
        {
            GameObject objeto = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), posicion, Quaternion.identity);
            creados.Add(objeto);
            return objeto;
        }

        private static void FijarFloat(UnityEngine.Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Arena, jugador, servicios y flujo con tiempos cortos, con KitsIniciales kits en el inventario.
        private IEnumerator Escenario(bool conServicios = true)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            jugador = Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            vida = jugador.GetComponent<VidaMecha>();
            pantalla = jugador.GetComponentInChildren<PantallaKitsCabina>(true);
            Assert.IsNotNull(pantalla, "MechaJugador lleva la pantalla de kits");
            kit = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            if (conServicios)
            {
                Crear(RutaServicios, Vector3.zero);
                inventario = Registro<ServicioInventario>.Actual;
                Assert.IsNotNull(inventario, "ServiciosEconomia registra el inventario");
                Assert.AreEqual(KitsIniciales, inventario.Agregar(kit, KitsIniciales));
            }

            config = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(config, "segundosPorNumero", SegundosPorNumero);
            FijarFloat(config, "pausaDespuesKO", PausaKO);
            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            creados.Add(padre);
            GameObject flujo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujo), padre.transform);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = config;
            soDirector.FindProperty("permitirRevancha").boolValue = true;
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = config;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            creados.Add(flujo);
            padre.SetActive(true);
            yield return null;
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite) yield return null;
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        // Empieza la pelea, espera ¡PELEA!, deja al rival quieto y la vida del jugador en VidaBaja.
        private IEnumerator PelearConRivalQuieto()
        {
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            director.Rival.ActivarControl(false);
            vida.FijarVida(VidaBaja);
            yield return null;
        }

        private IEnumerator Tecla(Key tecla)
        {
            Press(teclado[tecla]);
            yield return null;
            Release(teclado[tecla]);
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin) yield return null;
        }

        private static void Matar(IPeleador peleador)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        [UnityTest]
        public IEnumerator Kit_ConTresKitsYVida50_GastaUnoAlInstanteYCuraHasta70DeFormaPareja()
        {
            yield return Escenario();
            yield return PelearConRivalQuieto();
            Assert.AreEqual(VidaBaja, vida.Actual, MargenVida);

            float inicio = Time.time;
            Press(teclado[Key.G]);
            yield return null;
            Release(teclado[Key.G]);
            Assert.AreEqual(KitsIniciales - 1, inventario.Cantidad(kit), "Gasta el kit al instante");

            var cuadrosConSubida = new HashSet<int>();
            float anterior = vida.Actual;
            float llegada = -1f;
            float limite = Time.time + EsperaMaxima;
            while (llegada < 0f && Time.time < limite)
            {
                yield return null;
                if (vida.Actual > anterior + 0.0001f) cuadrosConSubida.Add(Time.frameCount);
                anterior = vida.Actual;
                if (vida.Actual >= VidaBaja + 20f - MargenVida) llegada = Time.time - inicio;
            }

            Assert.GreaterOrEqual(llegada, SegundosCuracionMinimo, "No cura de golpe");
            Assert.LessOrEqual(llegada, SegundosCuracionMaximo, "Cura en un segundo y medio");
            Assert.GreaterOrEqual(cuadrosConSubida.Count, CuadrosMinimos, "La vida sube en varios cuadros");
            yield return null;
            Assert.AreEqual(VidaBaja + 20f, vida.Actual, MargenVida);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Kit_SegundoUsoCuraYElTerceroEncuentraElLimite_ElSiguienteCombateLoReinicia()
        {
            yield return Escenario();
            yield return PelearConRivalQuieto();
            yield return Tecla(Key.G);
            yield return Esperar(SegundosCuracionMaximo);
            Assert.AreEqual(70f, vida.Actual, MargenVida);
            Assert.AreEqual("KITS 2 · G", pantalla.Texto.text);

            yield return Tecla(Key.G);
            Assert.AreEqual(KitsIniciales - 2, inventario.Cantidad(kit), "Segundo kit gastado");
            yield return Esperar(SegundosCuracionMaximo);
            Assert.AreEqual(90f, vida.Actual, MargenVida);

            yield return Tecla(Key.G);
            yield return Esperar(SegundosCuracionMinimo);
            Assert.AreEqual(KitsIniciales - 2, inventario.Cantidad(kit), "El tercero no gasta nada");
            Assert.AreEqual(90f, vida.Actual, MargenVida, "Ni cura");
            Assert.AreEqual("LÍMITE DE KITS", pantalla.Texto.text);

            // Revancha: el contador de usos se reinicia con la pelea nueva.
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return Tecla(Key.R);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            director.Rival.ActivarControl(false);
            vida.FijarVida(60f);
            yield return null;
            yield return Tecla(Key.G);
            Assert.AreEqual(0, inventario.Cantidad(kit), "En la pelea siguiente se vuelve a poder");
            yield return Esperar(SegundosCuracionMaximo);
            Assert.AreEqual(80f, vida.Actual, MargenVida);
            Assert.AreEqual("SIN KITS", pantalla.Texto.text);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Kit_MientrasSeRepara_LaPantallaDiceReparando()
        {
            yield return Escenario();
            yield return PelearConRivalQuieto();
            yield return Tecla(Key.G);
            Assert.AreEqual("REPARANDO…", pantalla.Texto.text);
            yield return Esperar(SegundosCuracionMaximo);
            Assert.AreEqual("KITS 2 · G", pantalla.Texto.text);
        }

        [UnityTest]
        public IEnumerator Kit_DuranteElConteo_NoCambiaVidaNiInventario()
        {
            yield return Escenario();
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            vida.FijarVida(VidaBaja);

            yield return Tecla(Key.G);

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "Sigue el conteo");
            Assert.AreEqual(KitsIniciales, inventario.Cantidad(kit));
            Assert.AreEqual(VidaBaja, vida.Actual, MargenVida);
        }

        [UnityTest]
        public IEnumerator Kit_SinKits_NoCambiaLaVidaYLaPantallaDiceSinKits()
        {
            yield return Escenario();
            Assert.IsTrue(inventario.Quitar(kit, KitsIniciales));
            yield return PelearConRivalQuieto();

            yield return Tecla(Key.G);
            yield return Esperar(SegundosCuracionMinimo);

            Assert.AreEqual(0, inventario.Cantidad(kit));
            Assert.AreEqual(VidaBaja, vida.Actual, MargenVida);
            Assert.AreEqual("SIN KITS", pantalla.Texto.text);
        }

        [UnityTest]
        public IEnumerator Kit_EnKO_NoCambiaVidaNiInventario()
        {
            yield return Escenario();
            yield return PelearConRivalQuieto();
            Matar(director.Jugador);
            Assert.IsTrue(vida.EstaKO);

            yield return Tecla(Key.G);
            yield return Esperar(SegundosCuracionMinimo);

            Assert.AreEqual(KitsIniciales, inventario.Cantidad(kit));
            Assert.IsTrue(vida.EstaKO, "Sigue en KO");
            Assert.AreEqual(0f, vida.Actual, MargenVida);
        }

        [UnityTest]
        public IEnumerator Kit_ConVidaCompleta_NoGastaElKit()
        {
            yield return Escenario();
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            director.Rival.ActivarControl(false);

            yield return Tecla(Key.G);

            Assert.AreEqual(KitsIniciales, inventario.Cantidad(kit));
        }

        [UnityTest]
        public IEnumerator Kit_SiElMechaCaeEnKOAMitadDeLaCuracion_LaCuracionSeCorta()
        {
            yield return Escenario();
            yield return PelearConRivalQuieto();
            yield return Tecla(Key.G);
            yield return Esperar(SegundosAMedioCurar);
            Assert.Greater(vida.Actual, VidaBaja, "Ya empezó a curar");

            Matar(director.Jugador);
            yield return Esperar(SegundosCuracionMaximo);

            Assert.IsTrue(vida.EstaKO, "Sigue en KO");
            Assert.AreEqual(0f, vida.Actual, MargenVida);
            Assert.AreEqual(KitsIniciales - 1, inventario.Cantidad(kit), "El kit ya estaba gastado");
        }

        [UnityTest]
        public IEnumerator Kit_FueraDeUnaPelea_NoHaceNada()
        {
            yield return Escenario();
            vida.FijarVida(VidaBaja);

            yield return Tecla(Key.G);

            Assert.AreEqual(KitsIniciales, inventario.Cantidad(kit));
            Assert.AreEqual(VidaBaja, vida.Actual, MargenVida);
        }

        [UnityTest]
        public IEnumerator Kit_SinServicioDeInventario_LaPantallaNoSeMuestra()
        {
            yield return Escenario(conServicios: false);
            yield return null;

            Assert.IsFalse(pantalla.Visible, "Sin inventario no hay pantalla de kits");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Pantalla_EnLaVista_QuedaFueraDelCuarentaPorCientoCentral()
        {
            yield return Escenario();
            yield return null;
            Assert.IsTrue(pantalla.Visible, "Con inventario y la cabina prendida se ve");
            Camera camara = jugador.GetComponentInChildren<Camera>();
            RectTransform rect = (RectTransform)pantalla.Lienzo.transform;
            Vector3[] esquinas = new Vector3[4];
            rect.GetWorldCorners(esquinas);
            try
            {
                foreach (float aspecto in Aspectos)
                {
                    camara.aspect = aspecto;
                    float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                    foreach (Vector3 esquina in esquinas)
                    {
                        Vector3 punto = camara.WorldToViewportPoint(esquina);
                        Assert.Greater(punto.z, 0f, "Está delante de la cámara");
                        minX = Mathf.Min(minX, punto.x);
                        maxX = Mathf.Max(maxX, punto.x);
                        minY = Mathf.Min(minY, punto.y);
                        maxY = Mathf.Max(maxY, punto.y);
                    }
                    bool tocaElCentro = maxX > CentroVista - MitadCentro && minX < CentroVista + MitadCentro
                        && maxY > CentroVista - MitadCentro && minY < CentroVista + MitadCentro;
                    Assert.IsFalse(tocaElCentro, "Aspecto " + aspecto + ": toca el 40 % central (x " + minX + " a " + maxX + ", y " + minY + " a " + maxY + ")");
                    Assert.Greater(minX, 0f, "Aspecto " + aspecto + ": entra por la izquierda");
                    Assert.Less(maxX, 1f, "Aspecto " + aspecto + ": entra por la derecha");
                    Assert.Greater(minY, 0f, "Aspecto " + aspecto + ": entra por abajo");
                }
            }
            finally
            {
                camara.ResetAspect();
            }
        }

        [UnityTest]
        public IEnumerator Captura_CabinaConKitsYMechaDaniado_GuardaT148Uno()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)))
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return Escenario();
            inventario.Quitar(kit, 1);
            yield return PelearConRivalQuieto();
            yield return Esperar(0.5f);
            Assert.AreEqual("KITS 2 · G", pantalla.Texto.text);

            Camera camara = jugador.GetComponentInChildren<Camera>();
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
            System.IO.File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            UnityEngine.Object.Destroy(imagen);
            destino.Release();
            UnityEngine.Object.Destroy(destino);
        }
    }
}
