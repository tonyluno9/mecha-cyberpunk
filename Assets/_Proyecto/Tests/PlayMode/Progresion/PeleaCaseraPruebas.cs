using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-129: la pelea casera y el mostrador del clandestino en Pruebas_Prologo. Se juega con entrada simulada (E y clics del ratón);
    /// lo único que no es entrada es rematar al rival con daño directo cuando el jugador ya recibió golpes reales.
    /// </summary>
    public class PeleaCaseraPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string RutaRivalCasero = "Assets/_Proyecto/IA/Prefabs/RivalCasero.prefab";
        private const string IdSalida = "Clandestino_Salida";
        private const string AliasRival = "EL CHATARRAS";
        private const string TextoOfrecido = "E · Pelea casera · premio $600";
        private const string TextoDeDia = "Peleas caseras de noche · abre 20:00";
        private const string TextoSinPelea = "Hoy no hay peleas caseras";
        private const int PremioCasero = 600;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosGolpes = 60f;
        private const float MaxSegundosSalida = 25f;
        private const float SegundosEntreClics = 0.25f;
        private const float SegundosSinEfecto = 1.5f;
        private const float DanioMortal = 100000f;
        private const float ToleranciaJugador = 0.5f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaFregada = 0.2f;
        private const float HoraDeDia = 10f;
        private const float DistanciaAlMostrador = 2f;
        private const float AlturaInicio = 0.05f;
        private const string VariableCaptura = "CAPTURAR_T129";
        private const string RutaCaptura = "docs/capturas/T-129-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 1f;

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private VidaMecha vidaMecha;
        private HudAPie hud;
        private readonly List<string> errores = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            carpeta = Path.Combine(Application.temporaryCachePath, "pelea_casera_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            terminadas.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            EventosPelea.LimpiarSuscriptores();
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

        // ---- Preparación y utilidades ----

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(mostrador, "Pruebas_Prologo trae el mostrador de peleas");
            Assert.IsNotNull(montaje, "Pruebas_Prologo trae el mecha de la historia");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            Assert.IsNotNull(director, "El mostrador trae el flujo casero");
            llamado = montaje.GetComponent<LlamadoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            EventosPelea.PeleaTerminada += terminadas.Add;
            yield return null;
        }

        // La etapa de la historia sale del ServicioHistoria real de la escena (empieza en Ninguna).
        private static void PonerEtapa(EtapaPrologo etapa)
        {
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            Assert.IsNotNull(historia, "La escena trae el ServicioHistoria");
            Assert.IsTrue(historia.Avanzar(etapa) || historia.Etapa == etapa, "La historia llega a " + etapa);
        }

        // Pone al jugador a 2 m al sur del mostrador, mirando hacia él, y deja pasar unos cuadros para que el HUD se actualice.
        private IEnumerator IrAlMostrador()
        {
            Vector3 posicion = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(posicion, Quaternion.identity));
            for (int i = 0; i < FramesParaElHud; i++)
            {
                yield return null;
            }
        }

        // Captura de lo que ve el jugador (solo con CAPTURAR_T129=1). Los lienzos en Overlay no salen en una cámara a textura:
        // se pasan un instante a ScreenSpaceCamera con esa cámara y se devuelven como estaban.
        private void Capturar()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = jugador.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = DistanciaPlanoCanvas;
                    movidos.Add(lienzo);
                }
            }
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
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        private static int OyentesActivos()
        {
            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    oyentes++;
                }
            }
            return oyentes;
        }

        private static int RivalesVivos()
        {
            int cuenta = 0;
            foreach (MonoBehaviour candidato in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Rival)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // Espera una condición revisando en cada cuadro que no haya más de un rival ni más de un oído activo.
        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo mientras se espera: " + que);
                Assert.LessOrEqual(RivalesVivos(), 1, "A lo más un rival en escena mientras se espera: " + que);
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private IEnumerator PulsarE()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
        }

        // Clics al aire hasta que se cumpla la condición (o se acabe el tiempo).
        private IEnumerator ClicsHasta(Func<bool> condicion)
        {
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosGolpes;
            while (!condicion() && Time.time < fin)
            {
                if (Time.time >= proximoClic)
                {
                    clic = !clic;
                    proximoClic = Time.time + SegundosEntreClics / 2f;
                    if (clic) Press(raton.leftButton); else Release(raton.leftButton);
                }
                yield return null;
            }
            Release(raton.leftButton);
            yield return null;
        }

        private IEnumerator EsperarPelea()
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Presentacion || director.Estado == EstadoFlujoPelea.Conteo,
                MaxSegundosEstado, "hay presentación o conteo");
            Assert.IsNotNull(director.Rival, "Hay rival");
            Assert.AreEqual(AliasRival, director.Rival.Nombre, "El rival es EL CHATARRAS");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.AreEqual(1, RivalesVivos(), "Un solo rival en escena");
        }

        // ---- Pruebas ----

        [Test]
        public void RivalCasero_Prefab_NoTienePiedadYSeLlamaElChatarras()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalCasero);
            Assert.IsNotNull(prefab, "Existe RivalCasero.prefab");
            Assert.IsNull(prefab.GetComponent<PiedadRival>(), "La casera se puede perder: sin PiedadRival (D-064)");

            GameObject instancia = Object.Instantiate(prefab);
            try
            {
                Assert.AreEqual(AliasRival, instancia.GetComponent<IPeleador>().Nombre, "IPeleador.Nombre");
                Assert.AreEqual(LadoPelea.Rival, instancia.GetComponent<IPeleador>().Lado);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Mostrador_DeNoche_OfreceLaPeleaYElJugadorGanaYCobra()
        {
            yield return CargarEscena();
            PonerEtapa(EtapaPrologo.PeleaCasera);
            yield return IrAlMostrador();
            ICartera cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "Hay cartera");
            int dineroAntes = cartera.Dinero;

            Assert.IsTrue(mostrador.PuedeUsar, "De noche se puede usar: " + mostrador.Texto);
            Assert.AreEqual(TextoOfrecido, hud.TextoAvisoInteraccion.text, "El HUD ofrece la pelea con el premio");
            Capturar();

            yield return PulsarE();
            Assert.IsTrue(entrada.EnCurso, "E empieza la entrada");
            yield return EsperarPelea();

            // Golpes reales al jugador y luego remate con daño directo (el rival casero no tiene piedad: puede ganar).
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            Assert.AreEqual(string.Empty, director.Pantalla.Revancha, "No hay revancha");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador, "Ganó el jugador");
            Assert.AreEqual(dineroAntes + PremioCasero, cartera.Dinero, "Cobró $600");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador quedó a pie");
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.Less(DistanciaHorizontal(jugador.Raiz.position, salida.Pose.position), ToleranciaJugador, "A menos de 0.5 m de la salida");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha volvió al taller");
            Assert.AreEqual(0, RivalesVivos(), "El rival ya no existe");
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado, "El flujo quedó listo");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo al final");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Mostrador_ConElMechaFregado_ElRivalLoDejaEnKOYPierde()
        {
            yield return CargarEscena();
            ICartera cartera = Registro<ICartera>.Actual;
            int dineroAntes = cartera.Dinero;
            PonerEtapa(EtapaPrologo.PeleaCasera);
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            yield return IrAlMostrador();
            Assert.IsTrue(mostrador.PuedeUsar, "Con el mecha fregado pero en pie se ofrece la pelea");

            yield return PulsarE();
            yield return EsperarPelea();

            // Solo clics al aire: el rival golpea de verdad hasta dejarlo en KO.
            yield return ClicsHasta(() => vidaMecha.EstaKO);
            Assert.IsTrue(vidaMecha.EstaKO, "El rival lo dejó en KO con golpes reales");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Rival, terminadas[0].Ganador, "Ganó el rival");
            Assert.AreEqual(dineroAntes, cartera.Dinero, "Perder no paga: el dinero no cambia");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador quedó a pie");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha volvió al taller");
            Assert.IsTrue(vidaMecha.EstaKO, "El mecha sigue en KO hasta repararlo");
            Assert.AreEqual(0, RivalesVivos(), "El rival ya no existe");

            // Con el mecha en KO el mostrador no ofrece otra pelea (la entrada fallaría): hay que repararlo primero.
            yield return IrAlMostrador();
            Assert.IsFalse(mostrador.PuedeUsar, "Con el mecha en KO no se puede pelear");
            Assert.IsNotEmpty(mostrador.Texto, "Y avisa por qué");
            yield return PulsarE();
            yield return new WaitForSeconds(SegundosSinEfecto);
            Assert.IsFalse(entrada.EnCurso, "E no hace nada");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Mostrador_DeDiaOSinEtapa_AvisaYNoHaceNada()
        {
            yield return CargarEscena();
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(reloj, "Hay reloj");
            reloj.Pausar(true);

            // De noche pero con la historia en otra etapa: hoy no hay peleas caseras.
            PonerEtapa(EtapaPrologo.PeleaTutorial);
            Assert.IsTrue(reloj.EsDeNoche, "Es de noche");
            yield return IrAlMostrador();
            Assert.IsFalse(mostrador.PuedeUsar, "En PeleaTutorial no se usa");
            Assert.AreEqual(TextoSinPelea, mostrador.Texto);
            Assert.AreEqual(TextoSinPelea, hud.TextoAvisoInteraccion.text, "El HUD dice que hoy no hay peleas");
            yield return PulsarE();
            yield return new WaitForSeconds(SegundosSinEfecto);
            Assert.IsFalse(entrada.EnCurso, "E con otra etapa no hace nada");

            // En la etapa de la casera y de noche sí se ofrece.
            PonerEtapa(EtapaPrologo.PeleaCasera);
            yield return IrAlMostrador();
            Assert.IsTrue(mostrador.PuedeUsar, "En PeleaCasera y de noche se usa");
            Assert.AreEqual(TextoOfrecido, hud.TextoAvisoInteraccion.text, "El HUD ofrece la pelea");

            // De día: abre a las 20:00 y E no hace nada.
            reloj.AvanzarHasta(HoraDeDia);
            yield return null;
            Assert.IsFalse(reloj.EsDeNoche, "Ahora es de día");
            yield return IrAlMostrador();
            Assert.IsFalse(mostrador.PuedeUsar, "De día no se usa");
            Assert.AreEqual(TextoDeDia, mostrador.Texto);
            Assert.AreEqual(TextoDeDia, hud.TextoAvisoInteraccion.text, "El HUD avisa que abre a las 20:00");
            yield return PulsarE();
            yield return new WaitForSeconds(SegundosSinEfecto);
            Assert.IsFalse(entrada.EnCurso, "E de día no hace nada");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Sigue a pie");
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado, "El flujo no arrancó");

            Assert.AreEqual(0, RivalesVivos(), "Ningún rival en escena");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
