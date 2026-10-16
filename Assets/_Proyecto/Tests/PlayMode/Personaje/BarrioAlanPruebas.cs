using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// Integración T-060: el jugador a pie de Tony en el barrio de Alan (Pruebas_Alan.unity), sin editar ni guardar nada de Alan.
    /// La escena se carga en la prueba, el prefab se instancia en tiempo de juego y todo se encuentra por los registros de Core.
    /// </summary>
    public class BarrioAlanPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaConfigControles = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string IdAparicionCasa = "Casa_Puerta";
        private const float HoraNoche = 22f;
        private const float HoraMediodia = 12f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosCaminata = 4f;
        private const float MetrosMinimosCaminata = 12f;
        private const float AlturaMinimaPiso = -0.5f;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float MaxSegundosPorTramo = 30f;
        private const float RadioLlegadaObjetivo = 4f;
        private const float MargenLlegadaEntrada = 0.5f;
        private const float MaxFrameSegundos = 0.1f;
        private const float MetrosFrenteAPuerta = 1.2f;
        private const float AlturaPies = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugadorAPie;
        private GameObject jugador;
        private InteraccionAPie interaccion;
        private HudAPie hud;
        private ServicioObjetivos objetivos;
        private IRelojMundo reloj;
        private float sensibilidadRaton;
        private bool avanzando;
        private float maxFrame;
        private readonly List<ResultadoPuerta> resultados = new List<ResultadoPuerta>();
        private readonly List<IdLugar> entradas = new List<IdLugar>();
        private readonly List<IdLugar> salidas = new List<IdLugar>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            resultados.Clear();
            entradas.Clear();
            salidas.Clear();
            avanzando = false;
            maxFrame = 0f;
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        // La escena se cargó en Single: se destruyen sus raíces para que no contamine otras pruebas.
        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        // ---- Preparación ----

        private IEnumerator CargarBarrio(string idAparicion, float hora)
        {
            AsyncOperation carga = EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return carga;

            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }

            // La cámara libre de Alan también es IJugador: se apaga su GameObject sin conocer su clase.
            foreach (IJugador otro in new List<IJugador>(Registro<IJugador>.Todos))
            {
                if (otro is Component componente)
                {
                    componente.gameObject.SetActive(false);
                }
            }

            reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(reloj, "El barrio trae su reloj");
            reloj.Pausar(true);
            reloj.AvanzarHasta(hora);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            jugador = UnityEngine.Object.Instantiate(prefab);
            jugador.GetComponent<AparicionJugador>().IdAparicion = idAparicion;
            yield return null;
            yield return null;

            jugadorAPie = jugador.GetComponent<JugadorAPie>();
            interaccion = jugador.GetComponent<InteraccionAPie>();
            hud = jugador.GetComponentInChildren<HudAPie>(true);
            objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            sensibilidadRaton = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>(RutaConfigControles).SensibilidadRaton;
            Assert.IsNotNull(hud, "El HUD viene con el jugador");
            Assert.IsNotNull(objetivos, "El prefab trae el servicio de objetivos");

            interaccion.PuertaIntentada += (puerta, resultado) => resultados.Add(resultado);
            EventosLugares.JugadorEntro += id => entradas.Add(id);
            EventosLugares.JugadorSalio += id => salidas.Add(id);

            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo IJugador registrado");
            Assert.AreSame(jugadorAPie, Registro<IJugador>.Actual, "Y es el de Tony");
            Assert.AreEqual(1, SceneManager.sceneCount, "Una sola escena");
        }

        // ---- Utilidades ----

        private IEnumerator Frame()
        {
            yield return null;
            maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime);
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

        private void Conducir(float angulo)
        {
            float limite = GradosMediaVuelta / sensibilidadRaton;
            float delta = Mathf.Clamp(angulo * GananciaGiro / sensibilidadRaton, -limite, limite);
            Set(raton.delta, new Vector2(delta, 0f));
            Avanzar(Mathf.Abs(angulo) < AnguloParaCaminar);
        }

        private float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugadorAPie.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        private float DistanciaHorizontal(Vector3 destino)
        {
            Vector3 d = destino - jugador.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private IEnumerator SeguirFlecha(Func<bool> listo, string que)
        {
            float fin = Time.time + MaxSegundosPorTramo;
            while (!listo())
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (objetivo '" + hud.TextoObjetivo.text + "', distancia " + hud.TextoDistancia.text
                        + ", posición " + jugador.transform.position + ")");
                }
                Conducir(hud.AnguloFlecha);
                yield return Frame();
            }
            Avanzar(false);
            yield return Frame();
        }

        private IEnumerator IrA(Vector3 destino, float distancia, string que)
        {
            float fin = Time.time + MaxSegundosPorTramo;
            while (DistanciaHorizontal(destino) > distancia)
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (posición " + jugador.transform.position + ")");
                }
                Conducir(AnguloHacia(destino));
                yield return Frame();
            }
            Avanzar(false);
            yield return Frame();
        }

        private IEnumerator MirarA(Vector3 destino, string que)
        {
            float fin = Time.time + MaxSegundosPorTramo;
            while (Mathf.Abs(AnguloHacia(destino)) > 2f)
            {
                if (Time.time > fin)
                {
                    Assert.Fail("No pudo mirar a: " + que);
                }
                Set(raton.delta, new Vector2(AnguloHacia(destino) * GananciaGiro / sensibilidadRaton, 0f));
                yield return Frame();
            }
            yield return Frame();
        }

        private static ILugar LugarDe(IdLugar id)
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == id)
                {
                    return lugar;
                }
            }
            return null;
        }

        private static IPuerta PuertaDe(IdLugar id)
        {
            foreach (IPuerta puerta in Registro<IPuerta>.Todos)
            {
                if (puerta.Lugar == id)
                {
                    return puerta;
                }
            }
            return null;
        }

        // Coloca al jugador en la entrada del lugar, mirando a su puerta.
        private IEnumerator ColocarFrenteA(IdLugar id)
        {
            ILugar lugar = LugarDe(id);
            IPuerta puerta = PuertaDe(id);
            Assert.IsNotNull(lugar, "Hay un ILugar " + id);
            Assert.IsNotNull(puerta, "Hay una IPuerta " + id);
            Vector3 haciaPuerta = puerta.Posicion - lugar.PosicionEntrada;
            haciaPuerta.y = 0f;
            Vector3 posicion = lugar.PosicionEntrada;
            posicion.y += AlturaPies;
            jugadorAPie.Colocar(new Pose(posicion, Quaternion.LookRotation(haciaPuerta.normalized, Vector3.up)));
            yield return Esperar(0.3f);
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Barrio_AparecEnLaCasaYCaminaCuatroSegundosPorLaBanqueta()
        {
            yield return CargarBarrio(IdAparicionCasa, HoraNoche);
            IPuntoAparicion punto = null;
            foreach (IPuntoAparicion candidato in Registro<IPuntoAparicion>.Todos)
            {
                if (candidato.Id == IdAparicionCasa)
                {
                    punto = candidato;
                }
            }
            Assert.IsNotNull(punto, "El barrio tiene el punto " + IdAparicionCasa);
            Assert.Less(DistanciaHorizontal(punto.Pose.position), 0.5f, "Aparece en la puerta de la casa");

            Vector3 inicio = jugador.transform.position;
            Avanzar(true);
            yield return Esperar(SegundosCaminata);
            Avanzar(false);
            yield return Frame();

            Assert.Greater(DistanciaHorizontal(inicio), MetrosMinimosCaminata, "Avanzó más de 12 m en 4 s");
            Assert.Greater(jugador.transform.position.y, AlturaMinimaPiso, "No cayó del piso");
            Assert.Less(maxFrame, MaxFrameSegundos);
        }

        [UnityTest]
        public IEnumerator Barrio_DelClandestinoAlBar_SigueLaFlechaAbreEntraYSale()
        {
            yield return CargarBarrio(IdAparicionCasa, HoraNoche);
            yield return ColocarFrenteA(IdLugar.Clandestino);

            ILugar bar = LugarDe(IdLugar.Bar);
            IPuerta puerta = PuertaDe(IdLugar.Bar);
            Assert.IsNotNull(bar);
            Assert.IsNotNull(puerta);
            Assert.IsTrue(bar.EstaAbierto, "A las 22:00 el Bar está abierto");
            Assert.Less(Vector3.Distance(LugarDe(IdLugar.Clandestino).PosicionEntrada, bar.PosicionEntrada), 30f, "Están frente a frente");

            // b) Objetivo al Bar y sigue solo la flecha hasta la llegada, luego los últimos metros a la entrada.
            objetivos.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Frame();
            StringAssert.Contains("bar", hud.TextoObjetivo.text.ToLowerInvariant());
            yield return SeguirFlecha(() => DistanciaHorizontal(bar.PosicionEntrada) < RadioLlegadaObjetivo, "la zona del Bar");
            yield return IrA(bar.PosicionEntrada, MargenLlegadaEntrada, "la entrada del Bar");
            yield return MirarA(puerta.Posicion, "la puerta del Bar");
            Assert.IsFalse(puerta.EstaAbierta);

            // c) E abre, W entra, media vuelta y sale. Una sola escena todo el tiempo.
            yield return Tecla(teclado.eKey);
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.Abierta }, resultados, "E abre la puerta del Bar");
            Assert.IsTrue(puerta.EstaAbierta);
            yield return Esperar(0.5f);

            Press(teclado.wKey);
            float limite = Time.time + MaxSegundosPorTramo;
            while (entradas.Count == 0 && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.AreEqual(1, SceneManager.sceneCount, "Sigue una sola escena al cruzar la puerta");
            yield return Esperar(1f);
            Release(teclado.wKey);
            yield return Frame();
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "JugadorEntro(Bar) una vez");
            Assert.AreEqual(0, salidas.Count);
            Assert.IsTrue(bar.JugadorDentro);

            Set(raton.delta, new Vector2(GradosMediaVuelta / sensibilidadRaton, 0f));
            yield return Frame();
            yield return Frame();
            Press(teclado.wKey);
            limite = Time.time + MaxSegundosPorTramo;
            while (salidas.Count == 0 && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.AreEqual(1, SceneManager.sceneCount, "Sigue una sola escena al salir");
            Release(teclado.wKey);
            yield return Esperar(0.3f);
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, salidas, "JugadorSalio(Bar) una vez");
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "Y no hubo otra entrada");
            Assert.IsFalse(bar.JugadorDentro);
            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Siempre un solo IJugador");
            Assert.Less(maxFrame, MaxFrameSegundos, "Ningún frame dura más de 100 ms");
        }

        [UnityTest]
        public IEnumerator Barrio_ClandestinoAMediodia_EDaCerradaPorHorarioYElHudDiceElHorario()
        {
            yield return CargarBarrio(IdAparicionCasa, HoraMediodia);
            yield return ColocarFrenteA(IdLugar.Clandestino);

            ILugar clandestino = LugarDe(IdLugar.Clandestino);
            IPuerta puerta = PuertaDe(IdLugar.Clandestino);
            Assert.IsFalse(clandestino.EstaAbierto, "A las 12:00 el Clandestino está cerrado");

            yield return Tecla(teclado.eKey);
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.CerradaPorHorario }, resultados);
            Assert.IsFalse(puerta.EstaAbierta);
            StringAssert.Contains(HorarioLugar.TextoHora(clandestino.Horario.Abre), hud.TextoAvisoInteraccion.text, "El HUD dice a qué hora abre");

            Avanzar(true);
            yield return Esperar(SegundosCaminata);
            Avanzar(false);
            yield return Frame();
            Assert.AreEqual(0, entradas.Count, "La puerta cerrada no lo deja pasar");
            Assert.AreEqual(1, Registro<IJugador>.Cantidad);
        }
    }
}
