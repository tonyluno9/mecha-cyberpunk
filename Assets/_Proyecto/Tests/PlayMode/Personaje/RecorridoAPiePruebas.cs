using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// Recorre Pruebas_APie.unity con el prefab completo del jugador y entrada simulada: el camino se sigue con la flecha del HUD
    /// (giro con delta del ratón proporcional a su ángulo y W para avanzar), sin leer posiciones del mundo salvo donde se indica.
    /// </summary>
    public class RecorridoAPiePruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private static float SensibilidadRaton => UnityEditor.AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>("Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset").SensibilidadRaton;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float MaxSegundosPorTramo = 20f;
        private const float MaxSegundosRecorrido = 60f;
        private const float MaxSegundosREGRESA = 4f;
        private const float MetrosAlejarse = 40f;
        private const float RadioLlegadaEsquina = 4.5f;
        private const float DistanciaSubir = 1.2f;
        private const float MetrosManejando = 10f;
        private const float EsquinaZ = 14f;
        private const string TextoRegresa = "REGRESA";

        private Keyboard teclado;
        private Mouse raton;
        private GameObject jugador;
        private JugadorAPie jugadorAPie;
        private HudAPie hud;
        private FijadorObjetivoDePrueba fijador;
        private CarroDePrueba carro;
        private readonly List<IdLugar> entradas = new List<IdLugar>();
        private readonly List<IdLugar> salidas = new List<IdLugar>();
        private readonly MedidorFrames medidor = new MedidorFrames();
        private bool avanzando;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            entradas.Clear();
            salidas.Clear();
            medidor.Reiniciar();
            avanzando = false;
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        // La escena se cargó en Single: se destruyen sus raíz para que no contamine otras pruebas.
        [UnityEngine.TestTools.UnityTearDown]
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

        // ---- Preparación y utilidades ----

        private IEnumerator CargarEscena()
        {
            AsyncOperation carga = EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return carga;

            // Los primeros frames de PlayMode son lentos: se espera a que se estabilicen antes de medir.
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }

            EventosLugares.JugadorEntro += id => entradas.Add(id);
            EventosLugares.JugadorSalio += id => salidas.Add(id);

            jugadorAPie = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugadorAPie, "La escena trae el jugador");
            jugador = jugadorAPie.gameObject;
            hud = UnityEngine.Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(hud, "El HUD viene con el jugador");
            fijador = UnityEngine.Object.FindFirstObjectByType<FijadorObjetivoDePrueba>();
            Assert.IsNotNull(fijador, "La escena trae el fijador de objetivos");
            carro = UnityEngine.Object.FindFirstObjectByType<CarroDePrueba>();
            Assert.IsNotNull(carro, "La escena trae el carro");
            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo jugador en la escena");
            yield return null;
        }

        private IEnumerator Frame()
        {
            yield return null;
            medidor.Registrar(Time.unscaledDeltaTime);
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

        // Un paso de control: gira en proporción al ángulo (° a la derecha) y camina solo si el objetivo no queda muy de lado.
        private void Conducir(float angulo)
        {
            float delta = Mathf.Clamp(angulo * GananciaGiro / SensibilidadRaton, -GradosMediaVuelta / SensibilidadRaton, GradosMediaVuelta / SensibilidadRaton);
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

        // Sigue la flecha del HUD hasta que se cumpla la condición. Falla si pasa el tiempo.
        private IEnumerator SeguirFlecha(Func<bool> listo, string que, float maxSegundos = MaxSegundosPorTramo)
        {
            float fin = Time.time + maxSegundos;
            while (!listo())
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (objetivo '" + hud.TextoObjetivo.text + "', distancia " + hud.TextoDistancia + ")");
                }
                Conducir(hud.AnguloFlecha);
                yield return Frame();
            }
            Avanzar(false);
            yield return Frame();
        }

        // Camina hacia un punto (cuando ya no se confía en la flecha) hasta quedar a menos de la distancia dada.
        private IEnumerator IrA(Vector3 destino, float distancia, string que)
        {
            float fin = Time.time + MaxSegundosPorTramo;
            while (DistanciaHorizontal(destino) > distancia)
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que);
                }
                Conducir(AnguloHacia(destino));
                yield return Frame();
            }
            Avanzar(false);
            yield return Frame();
        }

        // Gira en su lugar hasta mirar al punto.
        private IEnumerator MirarA(Vector3 destino, string que)
        {
            float fin = Time.time + MaxSegundosPorTramo;
            while (Mathf.Abs(AnguloHacia(destino)) > 2f)
            {
                if (Time.time > fin)
                {
                    Assert.Fail("No pudo mirar a: " + que);
                }
                Set(raton.delta, new Vector2(AnguloHacia(destino) * GananciaGiro / SensibilidadRaton, 0f));
                yield return Frame();
            }
            yield return Frame();
        }

        private static IPuerta PuertaDe(IdLugar lugar)
        {
            foreach (IPuerta puerta in Registro<IPuerta>.Todos)
            {
                if (puerta.Lugar == lugar)
                {
                    return puerta;
                }
            }
            return null;
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

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Recorrido_DeLaEsquinaAlBarYAlCarro_SiguiendoSoloLaFlecha()
        {
            yield return CargarEscena();
            float inicioRecorrido = Time.time;

            // a) Aparece en Prueba_Inicio, mirando a +Z, con el objetivo "Ve a la esquina" a la vista.
            Assert.Less(Vector3.Distance(new Vector3(0f, 0.05f, 0f), jugador.transform.position), 0.1f, "Aparece en Prueba_Inicio");
            Assert.Less(Quaternion.Angle(Quaternion.identity, jugador.transform.rotation), 1f, "Con la rotación del punto");
            Assert.AreEqual(0, fijador.Indice);
            Assert.IsTrue(hud.GrupoObjetivo.activeSelf, "El HUD muestra el objetivo");
            StringAssert.Contains("esquina", hud.TextoObjetivo.text);
            Assert.IsTrue(hud.HayPosicionObjetivo, "Y su flecha");

            // b) Sigue la flecha hasta la esquina.
            yield return SeguirFlecha(() => fijador.Indice >= 1, "la esquina");
            Assert.Less(DistanciaHorizontal(new Vector3(0f, 0f, EsquinaZ)), RadioLlegadaEsquina, "Llegó a la esquina");
            StringAssert.Contains("bar", hud.TextoObjetivo.text.ToLowerInvariant());

            // c) El objetivo ya es el Bar: lo sigue hasta que el seguimiento da la llegada (a 4 m de su entrada).
            yield return SeguirFlecha(() => fijador.Indice >= 2, "el Bar");
            StringAssert.Contains("carro", hud.TextoObjetivo.text.ToLowerInvariant());
            Assert.Less(Time.time - inicioRecorrido, MaxSegundosRecorrido, "A la esquina y al Bar en menos de 60 s");

            // El objetivo ya pasó al carro (llegó a 4 m de la entrada del Bar); los últimos metros hasta la puerta se hacen
            // hacia la entrada del lugar, que es lo que la flecha señalaba un instante antes.
            ILugar bar = LugarDe(IdLugar.Bar);
            IPuerta puerta = PuertaDe(IdLugar.Bar);
            Assert.IsNotNull(bar);
            Assert.IsNotNull(puerta);
            yield return IrA(bar.PosicionEntrada, 0.4f, "la entrada del Bar");
            yield return MirarA(puerta.Posicion, "la puerta del Bar");
            Assert.IsFalse(puerta.EstaAbierta);

            // d) E abre, W entra, media vuelta y sale.
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(puerta.EstaAbierta, "E abre la puerta del Bar");
            yield return Esperar(0.5f);
            Press(teclado.wKey);
            float limite = Time.time + MaxSegundosPorTramo;
            while (entradas.Count == 0 && Time.time < limite)
            {
                yield return Frame();
            }
            yield return Esperar(1f);
            Release(teclado.wKey);
            yield return Frame();
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "JugadorEntro(Bar) una vez");
            Assert.AreEqual(0, salidas.Count);

            Set(raton.delta, new Vector2(GradosMediaVuelta / SensibilidadRaton, 0f));
            yield return Frame();
            yield return Frame();
            Press(teclado.wKey);
            limite = Time.time + MaxSegundosPorTramo;
            while (salidas.Count == 0 && Time.time < limite)
            {
                yield return Frame();
            }
            Release(teclado.wKey);
            yield return Esperar(0.3f);
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, salidas, "JugadorSalio(Bar) una vez");
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "Y no hubo otra entrada");

            // e) Sigue la flecha al carro, sube, maneja y baja.
            Vector3 puntoSubida = carro.PuntoSubida;
            yield return SeguirFlecha(() => DistanciaHorizontal(puntoSubida) < DistanciaSubir, "la puerta del carro");
            yield return MirarA(puntoSubida, "la puerta del carro");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "E frente al carro sube");
            Assert.IsTrue(hud.GrupoObjetivo.activeSelf, "En el carro se sigue viendo el objetivo");

            Vector3 inicioCarro = carro.transform.position;
            Press(teclado.upArrowKey);
            limite = Time.time + MaxSegundosPorTramo;
            while (Vector3.Distance(inicioCarro, carro.transform.position) < MetrosManejando + 1f && Time.time < limite)
            {
                yield return Frame();
            }
            Release(teclado.upArrowKey);
            limite = Time.time + MaxSegundosPorTramo;
            while (!carro.PuedeBajar && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.Greater(Vector3.Distance(inicioCarro, carro.transform.position), MetrosManejando, "El carro se movió más de 10 m");
            Assert.IsTrue(carro.PuedeBajar, "El carro se detuvo");

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "E baja en modo APie");
            Assert.IsFalse(carro.Ocupado);

            Assert.IsTrue(medidor.Cumple, "Frames del recorrido: " + medidor.Resumen());
        }

        [UnityTest]
        public IEnumerator Recorrido_AlejarseDelBar40Metros_AparecePronto_REGRESA()
        {
            yield return CargarEscena();

            // Desde la esquina, mirando al oeste (el Bar queda al este).
            jugadorAPie.Colocar(new Pose(new Vector3(0f, 0.05f, EsquinaZ), Quaternion.Euler(0f, 270f, 0f)));
            float limite = Time.time + 2f;
            while (fijador.Indice < 1 && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.AreEqual(1, fijador.Indice, "En la esquina el objetivo pasa al Bar");
            yield return Esperar(0.3f);
            StringAssert.DoesNotContain(TextoRegresa, hud.TextoObjetivo.text, "Todavía no se aleja");

            Vector3 inicio = jugador.transform.position;
            Press(teclado.wKey);
            limite = Time.time + MaxSegundosPorTramo;
            while (DistanciaHorizontal(inicio) < MetrosAlejarse && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.GreaterOrEqual(DistanciaHorizontal(inicio), MetrosAlejarse, "Caminó 40 m");
            float llegoA40 = Time.time;
            while (!hud.TextoObjetivo.text.Contains(TextoRegresa) && Time.time - llegoA40 < MaxSegundosREGRESA)
            {
                yield return Frame();
            }
            Release(teclado.wKey);
            yield return Frame();

            StringAssert.Contains(TextoRegresa, hud.TextoObjetivo.text, "REGRESA en menos de 4 s tras alejarse 40 m");
            Assert.Less(Time.time - llegoA40, MaxSegundosREGRESA + 0.5f);
            Assert.IsTrue(medidor.Cumple, "Frames del recorrido: " + medidor.Resumen());
        }
    }
}
