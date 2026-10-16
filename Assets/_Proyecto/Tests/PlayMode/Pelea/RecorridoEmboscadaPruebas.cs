using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Cabina;
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
    /// T-072: la emboscada de Pruebas_Calle jugada de principio a fin con teclado y ratón simulados, como lo haría una persona:
    /// camina al callejón, llama al mecha (M), sube (E), hace el encendido (F, Espacio, Q), pelea mirando y golpeando, baja y
    /// lo devuelve. Del juego solo se lee estado para decidir la siguiente entrada; ningún método del juego avanza el recorrido.
    /// </summary>
    public class RecorridoEmboscadaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string RutaControlesMecha = "Assets/_Proyecto/Mecha/Datos/ConfigControlesMecha_Base.asset";
        private const string RutaPiedad = "Assets/_Proyecto/IA/Datos/ConfigPiedadRival_Base.asset";
        private const string RutaCaptura1 = "docs/capturas/T-101-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-101-2.png";
        private const string VariableCaptura = "CAPTURAR_T072";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosRecorrido = 120f;
        private const float MaxSegundosTramo = 40f;
        private const float MaxSegundosPelea = 70f;
        private const float MaxSegundosEncender = 30f;
        private const float SegundosSinPelear = 60f;
        private const float AlturaJugador = 0.05f;
        private const float ZCallejon = 87f;
        private const float XBocaCallejon = -6f;
        private const float XEsperaLlamado = -7.5f;
        private const float ZEsperaLlamado = 84.5f;
        private const float XJuntoAlMecha = -9.3f;
        private const float ZJuntoAlMecha = 85f;
        private const float DistanciaLlegada = 0.8f;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float AnguloMirarListo = 2f;
        private const float GradosMinimosGiro = 0.5f;
        private const float SegundosEntreClics = 0.25f;
        private const float MetrosCapturaPelea = 5f;
        private const float SegundosDetenido = 0.5f;
        private const float MargenPiso = 0.001f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float SegundosCaidaCaptura = 1.5f;
        private const float DistanciaCamaraCaptura = 4f;
        private const float AlturaCamaraCaptura = 13f;
        private const float DistanciaCamaraPelea = 9f;
        private const float AlturaCamaraPelea = 6f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private EncendidoMecha encendido;
        private VidaMecha vidaMecha;
        private DirectorEmboscada director;
        private PantallaEncendido pantallaEncendido;
        private ICalor calor;
        private float sensibilidadAPie;
        private float sensibilidadMecha;
        private float piso;
        private bool avanzando;
        private bool saltarMedicion;
        private readonly MedidorFrames medidor = new MedidorFrames();
        private float vidaMinima;
        private float inicioJuego;
        private float yawMechaAnterior;
        private float giroPedidoPendiente;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            avanzando = false;
            saltarMedicion = false;
            medidor.Reiniciar();
            giroPedidoPendiente = 0f;
            vidaMinima = float.MaxValue;
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

        // ---- Preparación ----

        private IEnumerator CargarCalle()
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            ConfigPiedadRival configPiedad = AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaPiedad);
            Assert.IsNotNull(configPiedad, "Existe la config de piedad");

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
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            director = Object.FindFirstObjectByType<DirectorEmboscada>();
            pantallaEncendido = Object.FindFirstObjectByType<PantallaEncendido>(FindObjectsInactive.Include);
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            Assert.IsNotNull(montaje, "Pruebas_Calle trae el mecha");
            Assert.IsNotNull(director, "Pruebas_Calle trae la emboscada");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor, "Hay un servicio de calor");
            piso = configPiedad.FraccionPiso * vidaMecha.Maxima;
            yield return null;

            Assert.Less(Vector3.Distance(new Vector3(0f, AlturaJugador, 0f), jugador.transform.position), 0.1f, "Aparece en Calle_Inicio");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha empieza en el taller");
            inicioJuego = Time.time;
            medidor.Reiniciar();
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        // ---- Utilidades de entrada ----

        // Un frame del juego: mide el frame más largo y la vida mínima del mecha.
        private IEnumerator Frame()
        {
            yield return null;
            if (saltarMedicion)
            {
                saltarMedicion = false;
            }
            else
            {
                medidor.Registrar(Time.unscaledDeltaTime);
            }
            if (vidaMecha != null)
            {
                vidaMinima = Mathf.Min(vidaMinima, vidaMecha.Actual);
            }
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return Frame();
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return Frame();
            Release(tecla);
            yield return Frame();
        }

        private void Avanzar(bool avanzar)
        {
            if (avanzar == avanzando)
            {
                return;
            }
            avanzando = avanzar;
            if (avanzar)
            {
                Press(teclado.wKey);
            }
            else
            {
                Release(teclado.wKey);
            }
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        // Ángulo (° a la derecha) desde la cámara del jugador hacia un punto del mundo.
        private float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugador.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        // Camina hacia un punto girando con el ratón en proporción al ángulo; termina cuando la condición se cumple.
        private IEnumerator CaminarA(Vector3 destino, float distancia, string que, Func<bool> antes = null)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (DistanciaPlana(jugador.transform.position, destino) > distancia && (antes == null || !antes()))
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (posición " + jugador.transform.position + ")");
                }
                float angulo = AnguloHacia(destino);
                float delta = Mathf.Clamp(angulo * GananciaGiro, -GradosMediaVuelta, GradosMediaVuelta) / sensibilidadAPie;
                Set(raton.delta, new Vector2(delta, 0f));
                Avanzar(Mathf.Abs(angulo) < AnguloParaCaminar);
                yield return Frame();
            }
            Avanzar(false);
            yield return Frame();
        }

        private IEnumerator MirarA(Vector3 destino, string que)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (Mathf.Abs(AnguloHacia(destino)) > AnguloMirarListo)
            {
                if (Time.time > fin)
                {
                    Assert.Fail("No pudo mirar a: " + que);
                }
                Set(raton.delta, new Vector2(AnguloHacia(destino) * GananciaGiro / sensibilidadAPie, 0f));
                yield return Frame();
            }
            yield return Frame();
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return Frame();
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        // ---- Pasos del recorrido ----

        // a) Camina al callejón hasta que la pandilla aparece, y sigue hasta la zona del punto de llamado.
        private IEnumerator CaminarAlCallejon()
        {
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "Lejos del callejón no pasa nada");
            yield return CaminarA(new Vector3(XBocaCallejon, AlturaJugador, ZCallejon), DistanciaLlegada, "la boca del callejón",
                () => director.Estado != EstadoEmboscada.Esperando);
            Assert.AreNotEqual(EstadoEmboscada.Esperando, director.Estado, "Al entrar al radio la pandilla aparece");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "la pandilla termina de aparecer");
            Assert.AreEqual(2, director.Rivales.Count, "Aparecen dos pandilleros");
            yield return CaminarA(new Vector3(XEsperaLlamado, AlturaJugador, ZEsperaLlamado), DistanciaLlegada, "la zona de llamado");
        }

        // b) y c) M llama al mecha; espera a que baje; camina junto a él y sube con E cuando la interacción lo ofrece.
        private IEnumerator LlamarYSubir()
        {
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado, "M dentro de la zona llama al mecha");
            float inicioLlamado = Time.time;
            yield return CaminarA(new Vector3(XJuntoAlMecha, AlturaJugador, ZJuntoAlMecha), DistanciaLlegada, "junto a donde baja el mecha");
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnCalle, 10f, "el mecha llega");
            float tardo = Time.time - inicioLlamado;
            Debug.Log("[Prueba T-072] El mecha llegó en " + tardo.ToString("F1") + " s");
            Assert.AreEqual(llamado.Config.SegundosLlegada, tardo, 1f, "El mecha llega en unos 6 s");

            yield return MirarA(montaje.PuntoSubida, "la escotilla del mecha");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Montable, 3f, "la interacción ofrece el mecha");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "E sube al mecha");
            yield return Frame();
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        // d) El encendido: lee qué paso pide y hace la tecla de ese paso.
        private IEnumerator Encender()
        {
            PasoEncendido ultimoTocado = PasoEncendido.Apagado;
            bool espacioPulsado = false;
            float fin = Time.time + MaxSegundosEncender;
            while (!encendido.EstaEncendido && Time.time < fin)
            {
                PasoEncendido paso = encendido.Paso;
                if (paso == PasoEncendido.Paso1 && ultimoTocado != PasoEncendido.Paso1)
                {
                    ultimoTocado = PasoEncendido.Paso1;
                    yield return Tecla(teclado.fKey);
                    continue;
                }
                if (paso == PasoEncendido.Paso2 && !espacioPulsado)
                {
                    espacioPulsado = true;
                    Press(teclado.spaceKey);
                }
                if (paso == PasoEncendido.Paso3)
                {
                    if (espacioPulsado)
                    {
                        espacioPulsado = false;
                        Release(teclado.spaceKey);
                    }
                    if (ultimoTocado != PasoEncendido.Paso3)
                    {
                        ultimoTocado = PasoEncendido.Paso3;
                        yield return Tecla(teclado.qKey);
                        continue;
                    }
                }
                yield return Frame();
            }
            if (espacioPulsado)
            {
                Release(teclado.spaceKey);
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");
            yield return Frame();
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        private static bool Existe(IPeleador peleador)
        {
            return peleador is Component componente && componente != null;
        }

        private IPeleador RivalEnPieMasCercano()
        {
            IPeleador mejor = null;
            float distanciaMejor = float.MaxValue;
            foreach (IPeleador rival in director.Rivales)
            {
                if (!Existe(rival) || rival.Vida.EstaKO)
                {
                    continue;
                }
                float distancia = DistanciaPlana(rival.Raiz.position, montaje.transform.position);
                if (distancia < distanciaMejor)
                {
                    distanciaMejor = distancia;
                    mejor = rival;
                }
            }
            return mejor;
        }

        // Un frame de pelea: el mecha gira hacia el rival en pie más cercano con delta de ratón proporcional al ángulo que falta
        // (descontando lo que el giro pesado todavía no hizo) y hace clic seguido.
        private void ApuntarYGolpear(IPeleador rival, bool clic)
        {
            float yaw = montaje.transform.eulerAngles.y;
            giroPedidoPendiente -= Mathf.DeltaAngle(yawMechaAnterior, yaw);
            yawMechaAnterior = yaw;

            Vector3 hacia = rival.Raiz.position - montaje.transform.position;
            hacia.y = 0f;
            float angulo = Vector3.SignedAngle(montaje.transform.forward, hacia, Vector3.up);
            float falta = angulo - giroPedidoPendiente;
            if (Mathf.Abs(falta) > GradosMinimosGiro)
            {
                Set(raton.delta, new Vector2(falta / sensibilidadMecha, 0f));
                giroPedidoPendiente += falta;
            }
            if (clic)
            {
                Press(raton.leftButton);
            }
            else
            {
                Release(raton.leftButton);
            }
        }

        // e) La pelea: mira y golpea hasta que la emboscada termina.
        private IEnumerator Pelear(string rutaCapturaPelea, string rutaCapturaKO)
        {
            int terminadas = 0;
            director.Terminada += () => terminadas++;
            giroPedidoPendiente = 0f;
            yawMechaAnterior = montaje.transform.eulerAngles.y;
            bool capturadaPelea = false;
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosPelea;
            while (director.Estado != EstadoEmboscada.Superada && Time.time < fin)
            {
                IPeleador rival = RivalEnPieMasCercano();
                if (rival != null)
                {
                    if (Time.time >= proximoClic)
                    {
                        clic = !clic;
                        proximoClic = Time.time + SegundosEntreClics / 2f;
                    }
                    ApuntarYGolpear(rival, clic);
                    if (!capturadaPelea && DistanciaPlana(rival.Raiz.position, montaje.transform.position) < MetrosCapturaPelea
                        && rival.Vida.Actual < rival.Vida.Maxima && RivalesEnPie() == director.Rivales.Count
                        && (pantallaEncendido == null || !pantallaEncendido.Visible))
                    {
                        capturadaPelea = true;
                        CapturarPeleaDesdeUnLado(rutaCapturaPelea, rival);
                    }
                }
                yield return Frame();
            }
            Release(raton.leftButton);
            yield return Frame();
            Assert.AreEqual(EstadoEmboscada.Superada, director.Estado, "La pandilla cayó antes de " + MaxSegundosPelea + " s de pelea");
            // La captura de la pandilla caída se toma después de dejar pasar su caída (duracionCaida 1 s en
            // ConfigPresentacionRival_Base) con un margen.
            yield return Esperar(SegundosCaidaCaptura);
            CapturarCaidosDesdeArriba(rutaCapturaKO);
            yield return Esperar(SegundosDetenido);
            Assert.AreEqual(1, terminadas, "Terminada llega una sola vez");
        }

        private Vector3 PuntoMedioDeLosRivales()
        {
            Vector3 suma = Vector3.zero;
            int cuenta = 0;
            foreach (IPeleador rival in director.Rivales)
            {
                if (Existe(rival))
                {
                    suma += rival.Raiz.position;
                    cuenta++;
                }
            }
            return cuenta > 0 ? suma / cuenta : montaje.transform.position + montaje.transform.forward;
        }

        private int RivalesEnPie()
        {
            int cuenta = 0;
            foreach (IPeleador rival in director.Rivales)
            {
                if (Existe(rival) && !rival.Vida.EstaKO)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // f) E baja (con el mecha detenido) y M lo devuelve al taller.
        private IEnumerator BajarYDevolver()
        {
            yield return EsperarHasta(() => montaje.PuedeBajar, 5f, "el mecha puede bajar (detenido)");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return Frame();
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Regresando, llamado.Estado, "M con el mecha vacío en la calle lo manda de vuelta");
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnTaller, 10f, "el mecha vuelve al taller");
        }

        // La cabina no alcanza a ver el piso cercano: la captura de los caídos sale de una cámara temporal en alto, mirando al
        // punto medio de los dos pandilleros.
        private void CapturarCaidosDesdeArriba(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Vector3 centro = PuntoMedioDeLosRivales();
            Vector3 haciaMecha = montaje.transform.position - centro;
            haciaMecha.y = 0f;
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            objeto.transform.position = centro + haciaMecha.normalized * DistanciaCamaraCaptura + Vector3.up * AlturaCamaraCaptura;
            objeto.transform.LookAt(centro);
            Capturar(ruta, camara);
            Object.Destroy(objeto);
        }

        // Desde la cabina la pelea queda tapada por el cuerpo del rival (una masa blanca a un paso de la cámara): la captura
        // sale de una cámara temporal al costado y en alto, mirando al punto medio entre el mecha y el rival golpeado.
        private void CapturarPeleaDesdeUnLado(string ruta, IPeleador rival)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Vector3 centro = (montaje.transform.position + rival.Raiz.position) / 2f;
            Vector3 haciaRival = rival.Raiz.position - montaje.transform.position;
            haciaRival.y = 0f;
            Vector3 costado = Vector3.Cross(Vector3.up, haciaRival.normalized);
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            objeto.transform.position = centro + costado * DistanciaCamaraPelea + Vector3.up * AlturaCamaraPelea;
            objeto.transform.LookAt(centro + Vector3.up);
            Capturar(ruta, camara);
            Object.Destroy(objeto);
        }

        private void Capturar(string ruta, Camera camaraElegida = null)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = camaraElegida != null ? camaraElegida : jugador.Camara;
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
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            saltarMedicion = true;
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        private void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Recorrido_DeLaCalleAlCallejonYLaPandillaEnKO_JugandoComoUnaPersona()
        {
            yield return CargarCalle();

            yield return CaminarAlCallejon();
            yield return LlamarYSubir();
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Con el mecha encendido la pandilla pelea");
            yield return Pelear(RutaCaptura1, RutaCaptura2);
            yield return BajarYDevolver();

            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-072] Recorrido a-f: " + segundos.ToString("F1") + " s de juego, vida mínima del mecha "
                + vidaMinima.ToString("F1") + " de " + vidaMecha.Maxima.ToString("F0") + " (piso " + piso.ToString("F1")
                + "), frame más largo " + (medidor.Maximo * 1000f).ToString("F0") + " ms, calor final " + calor.Valor.ToString("F1")
                + " (nivel " + calor.Nivel + ")");

            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de 120 s de juego");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador termina a pie");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha termina en el taller");
            Assert.Greater(vidaMinima, 0f, "El jugador no pudo perder: la vida del mecha nunca llegó a 0");
            Assert.GreaterOrEqual(vidaMinima, piso - MargenPiso, "Nunca bajó del piso de piedad");
            Assert.Greater(calor.Valor, 0f, "Al final hay calor");
            Assert.GreaterOrEqual(calor.Nivel, 1, "Y es de nivel 1 o más");
            Assert.IsTrue(medidor.Cumple, "Frames del recorrido: " + medidor.Resumen());
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator SinPelear_SesentaSegundosEnElMechaEncendido_NoSePuedePerder()
        {
            yield return CargarCalle();

            yield return CaminarAlCallejon();
            yield return LlamarYSubir();
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Con el mecha encendido la pandilla pelea");

            vidaMinima = vidaMecha.Actual;
            yield return Esperar(SegundosSinPelear);
            Debug.Log("[Prueba T-072] Sin pelear " + SegundosSinPelear + " s: vida mínima del mecha " + vidaMinima.ToString("F1")
                + " de " + vidaMecha.Maxima.ToString("F0") + " (piso " + piso.ToString("F1") + "), vida final "
                + vidaMecha.Actual.ToString("F1") + ", frame más largo " + (medidor.Maximo * 1000f).ToString("F0") + " ms");

            Assert.IsFalse(vidaMecha.EstaKO, "El mecha no cayó en KO");
            Assert.Greater(vidaMinima, 0f, "La vida del mecha nunca llegó a 0");
            Assert.GreaterOrEqual(vidaMinima, piso - MargenPiso, "Nunca bajó del piso de piedad");
            Assert.AreEqual(2, RivalesEnPie(), "Los rivales siguen en pie");
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Y la emboscada sigue");
            AssertSinErrores();
        }
    }
}
