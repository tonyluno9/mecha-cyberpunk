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
    /// Base común del recorrido de la emboscada (T-072 y T-073): entrada simulada y los pasos de llamar, subir, encender, pelear,
    /// bajar y devolver el mecha. Del juego solo se lee estado para decidir la siguiente entrada. Cada escena pone su carga y sus puntos.
    /// </summary>
    public abstract class RecorridoEmboscadaBase : InputTestFixture
    {
        protected const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        protected const string RutaControlesMecha = "Assets/_Proyecto/Mecha/Datos/ConfigControlesMecha_Base.asset";
        protected const string RutaPiedad = "Assets/_Proyecto/IA/Datos/ConfigPiedadRival_Base.asset";
        protected virtual string VariableCaptura => "CAPTURAR_T072";
        protected const int FramesEstables = 10;
        protected const int MaxFramesCalentamiento = 1000;
        protected const float DeltaTimeEstable = 0.05f;
        protected const float MaxSegundosRecorrido = 120f;
        protected const float MaxSegundosTramo = 40f;
        protected const float MaxSegundosPelea = 70f;
        protected const float MaxSegundosEncender = 30f;
        protected const float SegundosSinPelear = 60f;
        protected const float AlturaJugador = 0.05f;
        protected const float DistanciaLlegada = 0.8f;
        protected const float GananciaGiro = 0.4f;
        protected const float AnguloParaCaminar = 60f;
        protected const float GradosMediaVuelta = 180f;
        protected const float AnguloMirarListo = 2f;
        protected const float GradosMinimosGiro = 0.5f;
        protected const float SegundosEntreClics = 0.25f;
        protected const float MetrosCapturaPelea = 5f;
        protected const float SegundosDetenido = 0.5f;
        protected const float MargenPiso = 0.001f;
        protected const int AnchoCaptura = 1280;
        protected const int AltoCaptura = 720;
        protected const int ProfundidadCaptura = 24;
        protected const int SaltoDePixeles = 97;
        protected const float VariacionMinima = 0.05f;
        protected const float SegundosCaidaCaptura = 1.5f;
        protected const float DistanciaCamaraCaptura = 4f;
        protected const float AlturaCamaraCaptura = 13f;
        protected const float DistanciaCamaraPelea = 9f;
        protected const float AlturaCamaraPelea = 6f;

        protected Keyboard teclado;
        protected Mouse raton;
        protected JugadorAPie jugador;
        protected InteraccionAPie interaccion;
        protected MontajeMecha montaje;
        protected LlamadoMecha llamado;
        protected EncendidoMecha encendido;
        protected VidaMecha vidaMecha;
        protected DirectorEmboscada director;
        protected PantallaEncendido pantallaEncendido;
        protected ICalor calor;
        protected float sensibilidadAPie;
        protected float sensibilidadMecha;
        protected float piso;
        protected bool avanzando;
        protected bool saltarMedicion;
        protected readonly MedidorFrames medidor = new MedidorFrames();
        protected float vidaMinima;
        protected float inicioJuego;
        protected float yawMechaAnterior;
        protected float giroPedidoPendiente;
        protected readonly List<string> errores = new List<string>();

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
            alFrame = null;
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

        protected void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        // ---- Preparación ----

        // Carga la escena y espera a que el tiempo por frame se estabilice.
        protected IEnumerator CargarEscena(string ruta)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        // Busca las piezas del recorrido en la escena cargada (o ya armada) y prepara las lecturas.
        protected void ResolverReferencias()
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            ConfigPiedadRival configPiedad = AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaPiedad);
            Assert.IsNotNull(configPiedad, "Existe la config de piedad");
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            director = Object.FindFirstObjectByType<DirectorEmboscada>();
            pantallaEncendido = Object.FindFirstObjectByType<PantallaEncendido>(FindObjectsInactive.Include);
            Assert.IsNotNull(jugador, "Hay un jugador a pie");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            Assert.IsNotNull(montaje, "Hay un mecha");
            Assert.IsNotNull(director, "Hay una emboscada");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor, "Hay un servicio de calor");
            piso = configPiedad.FraccionPiso * vidaMecha.Maxima;
        }

        // Empieza a medir el recorrido (tiempo, frames y vida mínima).
        protected void EmpezarMedicion()
        {
            inicioJuego = Time.time;
            medidor.Reiniciar();
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        // Puntos del recorrido de la escena: dónde espera el jugador a que baje el mecha.
        protected Vector3 juntoAlMecha;
        // Gancho opcional que corre al final de cada frame del recorrido (por ejemplo, para tomar una captura en un momento dado).
        protected Action alFrame;

        // ---- Utilidades de entrada ----

        // Un frame del juego: mide el frame más largo y la vida mínima del mecha.
        protected IEnumerator Frame()
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
            alFrame?.Invoke();
        }

        protected IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return Frame();
            }
        }

        protected IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return Frame();
            Release(tecla);
            yield return Frame();
        }

        protected void Avanzar(bool avanzar)
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

        protected static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        // Ángulo (° a la derecha) desde la cámara del jugador hacia un punto del mundo.
        protected float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugador.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        // Camina hacia un punto girando con el ratón en proporción al ángulo; termina cuando la condición se cumple.
        protected IEnumerator CaminarA(Vector3 destino, float distancia, string que, Func<bool> antes = null)
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

        protected IEnumerator MirarA(Vector3 destino, string que)
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

        protected IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return Frame();
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        // ---- Pasos del recorrido ----

        // b) y c) M llama al mecha; espera a que baje; camina junto a él y sube con E cuando la interacción lo ofrece.
        protected IEnumerator LlamarYSubir()
        {
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado, "M dentro de la zona llama al mecha");
            float inicioLlamado = Time.time;
            yield return CaminarA(juntoAlMecha, DistanciaLlegada, "junto a donde baja el mecha");
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
        protected IEnumerator Encender()
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
                        // Un cuadro entre soltar y la tecla siguiente: Press justo tras Release en el mismo cuadro vuelve a pegar la tecla.
                        yield return null;
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

        protected static bool Existe(IPeleador peleador)
        {
            return peleador is Component componente && componente != null;
        }

        protected IPeleador RivalEnPieMasCercano()
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
        protected void ApuntarYGolpear(IPeleador rival, bool clic)
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
        protected IEnumerator Pelear(string rutaCapturaPelea, string rutaCapturaKO)
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

        protected Vector3 PuntoMedioDeLosRivales()
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

        protected int RivalesEnPie()
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
        protected IEnumerator BajarYDevolver()
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
        protected void CapturarCaidosDesdeArriba(string ruta)
        {
            if (ruta == null)
            {
                return;
            }
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
        protected void CapturarPeleaDesdeUnLado(string ruta, IPeleador rival)
        {
            if (ruta == null)
            {
                return;
            }
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

        protected void Capturar(string ruta, Camera camaraElegida = null)
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

        protected void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

    }
}
