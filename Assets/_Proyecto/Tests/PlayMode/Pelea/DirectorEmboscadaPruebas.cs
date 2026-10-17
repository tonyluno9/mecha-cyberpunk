using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>
    /// T-071 en Pruebas_Calle con el jugador real: la pandilla aparece al llegar al callejón, espera mientras el jugador va a pie
    /// y solo pelea con el jugador en el mecha. El mecha se sube por API (JugadorAPie.Subir) y el encendido se hace con teclas;
    /// el recorrido completo con entrada simulada es T-072.
    /// </summary>
    public class DirectorEmboscadaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaDatos = "Assets/_Proyecto/Pelea/Datos/DatosEmboscada_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-071-1.png";
        private const string VariableCaptura = "CAPTURAR_T071";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float AlturaJugador = 0.05f;
        private const float ToleranciaPose = 0.1f;
        private const float ZCallejon = 87f;
        private const float GiroHaciaCallejon = -90f;
        private const float XBocaCallejon = -6f;
        private const float XCercaDeRivales = -18f;
        private const float SegundosSinHacerNada = 1f;
        private const float SegundosAPieCerca = 10f;
        private const float ToleranciaQuieto = 0.05f;
        private const float MaxSegundosAparicion = 5f;
        private const float MaxSegundosLlamado = 12f;
        private const float MaxSegundosEncender = 10f;
        private const float SegundosAcercarse = 2f;
        private const float AcercamientoMinimo = 3f;
        private const float MaxSegundosDetenerse = 0.2f;
        private const float SegundosDespuesDeDetenerse = 1f;
        private const float MargenRetiroCuerpo = 1.5f;
        private const float DanioMortal = 100000f;
        private const int RivalesEsperados = 2;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private DirectorEmboscada director;
        private DatosEmboscada datos;
        private readonly List<string> errores = new List<string>();

        // Solo se lee el contrato IObjetivos.
        private static IObjetivos Servicio => Registro<IObjetivos>.Actual;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            director = Object.FindFirstObjectByType<DirectorEmboscada>();
            datos = AssetDatabase.LoadAssetAtPath<DatosEmboscada>(RutaDatos);
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(director, "Pruebas_Calle trae la emboscada");
            Assert.IsNotNull(datos, "Existen los datos de la emboscada");
            yield return null;
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private void PonerJugadorEn(float x)
        {
            jugador.Colocar(new Pose(new Vector3(x, AlturaJugador, ZCallejon), Quaternion.identity));
        }

        private IEnumerator EntrarAlCallejon()
        {
            PonerJugadorEn(XBocaCallejon);
            yield return EsperarHasta(() => director.Estado != EstadoEmboscada.Esperando, MaxSegundosAparicion, "la emboscada empieza");
        }

        private IEnumerator EsperarPandillaLista()
        {
            yield return EntrarAlCallejon();
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha,
                datos.SegundosAparicion + MaxSegundosAparicion, "la pandilla termina de aparecer");
        }

        private static bool Existe(IPeleador peleador)
        {
            return peleador is Component componente && componente != null;
        }

        private List<IPeleador> RivalesVivos()
        {
            List<IPeleador> vivos = new List<IPeleador>();
            foreach (IPeleador rival in director.Rivales)
            {
                if (Existe(rival) && !rival.Vida.EstaKO)
                {
                    vivos.Add(rival);
                }
            }
            return vivos;
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        private float DistanciaMinimaARivales(Vector3 punto)
        {
            float minima = float.MaxValue;
            foreach (IPeleador rival in RivalesVivos())
            {
                minima = Mathf.Min(minima, DistanciaPlana(rival.Raiz.position, punto));
            }
            return minima;
        }

        private IEnumerator LlamarYSubir()
        {
            Assert.AreEqual(ResultadoLlamado.Llega, llamado.IntentarLlamar(), "Desde el callejón se puede llamar al mecha");
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnCalle, MaxSegundosLlamado, "el mecha llega");
            Assert.IsTrue(jugador.Subir(montaje), "El jugador sube al mecha");
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo);
        }

        private IEnumerator Encender()
        {
            EncendidoMecha encendido = montaje.GetComponent<EncendidoMecha>();
            yield return Tecla(teclado.fKey);
            Press(teclado.spaceKey);
            float fin = Time.time + MaxSegundosEncender;
            while (Time.time < fin && encendido.Paso != PasoEncendido.Paso3)
            {
                yield return null;
            }
            Release(teclado.spaceKey);
            yield return Tecla(teclado.qKey);
            while (Time.time < fin && !encendido.EstaEncendido)
            {
                yield return null;
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");
        }

        private void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Aparicion_LejosNoPasaNada_AlEntrarAlRadioAparecenDosRivalesSinControl()
        {
            yield return CargarCalle();

            yield return Esperar(SegundosSinHacerNada);
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "Lejos no pasa nada");
            Assert.AreEqual(0, director.Rivales.Count, "Lejos no hay rivales");

            yield return EntrarAlCallejon();
            Assert.AreEqual(EstadoEmboscada.Aparicion, director.Estado);
            Assert.AreEqual(RivalesEsperados, director.Rivales.Count);
            for (int i = 0; i < director.Rivales.Count; i++)
            {
                Vector3 esperada = director.transform.TransformPoint(datos.Poses[i].posicion);
                Assert.Less(Vector3.Distance(director.Rivales[i].Raiz.position, esperada), ToleranciaPose,
                    "El rival " + i + " aparece en su pose");
                Assert.IsFalse(director.Rivales[i].ControlActivo, "El rival " + i + " nace sin control");
            }

            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, MaxSegundosAparicion, "termina la aparición");
            foreach (IPeleador rival in director.Rivales)
            {
                Assert.IsFalse(rival.ControlActivo, "A pie nadie pelea");
            }
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator APie_DiezSegundosCercaDeLosRivales_NoSeMuevenNiLePegan()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            PonerJugadorEn(XCercaDeRivales);
            yield return null;

            Vector3[] inicio = new Vector3[director.Rivales.Count];
            for (int i = 0; i < inicio.Length; i++)
            {
                inicio[i] = director.Rivales[i].Raiz.position;
            }
            VidaAPie vida = jugador.GetComponentInChildren<VidaAPie>();
            Assert.IsNotNull(vida, "El jugador a pie tiene vida");
            float vidaInicial = vida.Actual;

            yield return Esperar(SegundosAPieCerca);

            for (int i = 0; i < inicio.Length; i++)
            {
                Assert.Less(Vector3.Distance(director.Rivales[i].Raiz.position, inicio[i]), ToleranciaQuieto, "El rival " + i + " se quedó quieto");
            }
            Assert.AreEqual(vidaInicial, vida.Actual, 0.001f, "Nadie le pega a la persona a pie");
            Assert.AreEqual(EstadoEmboscada.EsperandoMecha, director.Estado);
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Objetivo_ALaEspera_ApuntaAlPuntoDeLlamadoDelCallejon()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            yield return null;

            IObjetivos servicio = Servicio;
            Assert.IsNotNull(servicio, "El jugador trae el servicio de objetivos");
            Assert.IsTrue(servicio.HayObjetivo);
            Assert.AreEqual(datos.TextoLlamar, servicio.Actual.Texto);
            Assert.IsTrue(servicio.Actual.TienePosicion);
            GameObject punto = GameObject.Find("PuntoLlamado Callejón");
            Assert.IsNotNull(punto, "La escena trae el punto de llamado del callejón");
            Assert.Less(Vector3.Distance(servicio.Actual.Posicion, punto.transform.position), ToleranciaPose);

            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(hud);
            StringAssert.Contains(datos.TextoLlamar.ToUpperInvariant(), hud.TextoObjetivo.text.ToUpperInvariant(), "El HUD muestra el objetivo");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Peleando_ConElJugadorEnElMechaEncendido_LosRivalesTienenControlYSeAcercan()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            yield return LlamarYSubir();
            yield return null;
            IObjetivos servicio = Servicio;
            Assert.AreEqual(datos.TextoDerrotar, servicio.Actual.Texto, "Dentro del mecha: derrota a la pandilla");
            Vector3 mecha = montaje.transform.position;
            float antes = DistanciaMinimaARivales(mecha);
            yield return Encender();

            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado);
            yield return null;
            foreach (IPeleador rival in director.Rivales)
            {
                Assert.IsTrue(rival.ControlActivo, "Con el jugador en el mecha, la pandilla pelea");
            }

            float despues = DistanciaMinimaARivales(mecha);
            Assert.Less(despues, antes - AcercamientoMinimo, "Los rivales se acercan al mecha");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Bajar_AMediaPelea_LosRivalesSeDetienenYAlSubirReanudan()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            yield return LlamarYSubir();
            yield return Encender();
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.Peleando, MaxSegundosAparicion, "peleando");
            yield return Esperar(SegundosAcercarse);

            float inicioBajada = Time.time;
            Assert.IsTrue(jugador.Bajar(), "El jugador baja del mecha");
            yield return EsperarHasta(() => RivalesVivos().TrueForAll(r => !r.ControlActivo), MaxSegundosDetenerse, "los rivales pierden el control");
            Assert.LessOrEqual(Time.time - inicioBajada, MaxSegundosDetenerse + Time.deltaTime, "Se detienen en menos de 0.2 s");
            Assert.AreEqual(EstadoEmboscada.Pausada, director.Estado);

            Vector3[] posiciones = new Vector3[director.Rivales.Count];
            for (int i = 0; i < posiciones.Length; i++)
            {
                posiciones[i] = director.Rivales[i].Raiz.position;
            }
            VidaAPie vida = jugador.GetComponentInChildren<VidaAPie>();
            float vidaInicial = vida.Actual;
            yield return Esperar(SegundosDespuesDeDetenerse);
            for (int i = 0; i < posiciones.Length; i++)
            {
                Assert.Less(DistanciaPlana(director.Rivales[i].Raiz.position, posiciones[i]), ToleranciaQuieto, "El rival " + i + " sigue quieto");
            }
            Assert.AreEqual(vidaInicial, vida.Actual, 0.001f, "A pie nadie le pega");

            Assert.IsTrue(jugador.Subir(montaje), "Vuelve a subir");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.Peleando, MaxSegundosAparicion, "reanuda");
            yield return null;
            Assert.IsTrue(RivalesVivos().TrueForAll(r => r.ControlActivo), "Al subir otra vez recuperan el control");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Terminar_TodosEnKO_AvisaUnaVezQuitaElObjetivoYRetiraLosCuerpos()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            Assert.IsTrue(jugador.Subir(montaje), "El jugador sube al mecha (por API, desde el taller)");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.Peleando, MaxSegundosAparicion, "peleando");

            int terminadas = 0;
            director.Terminada += () => terminadas++;
            List<IPeleador> rivales = new List<IPeleador>(director.Rivales);
            foreach (IPeleador rival in rivales)
            {
                IRecibeDanio recibe = ((Component)rival).GetComponent<IRecibeDanio>();
                Assert.IsNotNull(recibe, "El rival recibe daño");
                recibe.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, rival.Raiz.position, Vector3.forward, null));
                Assert.IsTrue(rival.Vida.EstaKO, "El daño directo lo deja en KO");
            }
            yield return Esperar(SegundosSinHacerNada);

            Assert.AreEqual(EstadoEmboscada.Superada, director.Estado);
            Assert.AreEqual(1, terminadas, "Terminada avisa una sola vez");
            Assert.IsFalse(Servicio.HayObjetivo, "La emboscada quita su objetivo");

            yield return Esperar(datos.SegundosRetiroCuerpo + MargenRetiroCuerpo);
            foreach (IPeleador rival in rivales)
            {
                Assert.IsFalse(Existe(rival), "El cuerpo caído se retira");
            }
            Assert.AreEqual(1, terminadas, "Sigue siendo una sola vez");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Empezar_SinActivarAlAcercarse_SoloEmpiezaConEmpezar()
        {
            yield return CargarCalle();
            FieldInfo campo = typeof(DirectorEmboscada).GetField("activarAlAcercarse", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(campo, "El director tiene activarAlAcercarse");
            campo.SetValue(director, false);

            PonerJugadorEn(XBocaCallejon);
            yield return Esperar(datos.SegundosAparicion + SegundosSinHacerNada);
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "Cerca, pero sin activar al acercarse, no empieza");
            Assert.AreEqual(0, director.Rivales.Count);

            director.Empezar();
            director.Empezar();
            yield return null;
            Assert.AreEqual(EstadoEmboscada.Aparicion, director.Estado);
            Assert.AreEqual(RivalesEsperados, director.Rivales.Count, "Empezar dos veces no duplica rivales");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Habilitada_PorDefectoEsVerdadera()
        {
            yield return CargarCalle();
            Assert.IsTrue(director.Habilitada, "Sin director del prólogo la emboscada nace habilitada");
        }

        [UnityTest]
        public IEnumerator Habilitada_Falsa_AcercarseNoLaActivaYAlVolverAHabilitarlaSi()
        {
            yield return CargarCalle();
            director.Habilitada = false;

            PonerJugadorEn(XBocaCallejon);
            yield return Esperar(datos.SegundosAparicion + SegundosSinHacerNada);
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "Deshabilitada, acercarse no la activa");
            Assert.AreEqual(0, director.Rivales.Count);

            director.Habilitada = true;
            yield return EsperarHasta(() => director.Estado != EstadoEmboscada.Esperando, MaxSegundosAparicion,
                "Habilitada otra vez, con el jugador ya cerca, empieza");
            Assert.AreEqual(RivalesEsperados, director.Rivales.Count);
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Habilitada_Falsa_EmpezarManualSigueFuncionando()
        {
            yield return CargarCalle();
            director.Habilitada = false;
            director.Empezar();
            yield return null;
            Assert.AreEqual(EstadoEmboscada.Aparicion, director.Estado);
            Assert.AreEqual(RivalesEsperados, director.Rivales.Count);
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Captura_PandillaRecienAparecida_SeVeALoLejosConElObjetivoEnElHud()
        {
            yield return CargarCalle();
            yield return EsperarPandillaLista();
            jugador.Colocar(new Pose(new Vector3(XBocaCallejon, AlturaJugador, ZCallejon), Quaternion.Euler(0f, GiroHaciaCallejon, 0f)));
            yield return Esperar(SegundosSinHacerNada);

            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Camera camara = jugador.Camara;
            Canvas lienzo = hud.GetComponentInParent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = camara;
            lienzo.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            yield return null;

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
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            }
            Object.Destroy(rt);
            Object.Destroy(imagen);

            Assert.Greater(maximo - minimo, VariacionMinima, "La imagen es plana: no se ve nada");
            AssertSinErrores();
        }
    }
}
