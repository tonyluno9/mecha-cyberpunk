using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Tests.Personaje;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-693: el jefe de Nova golpea de verdad con un patrón claro. Telegrafía (pausa y brillo), golpea en área corta, se recupera (se le puede
    /// castigar) y cada 3 golpes carga. Con entrada simulada: si te quedas quieto te pega; si ruedas agachado (Ctrl, A, Espacio) lo esquivas.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-693-*.png.
    /// </summary>
    public class JefeNovaT693Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";

        private Keyboard teclado;
        private GameObject piso;
        private GameObject jugador;
        private GameObject objetoJefe;
        private PandilleroAPie jefe;
        private DatosPandilleroAPie datos;
        private VidaAPie vida;
        private float impactosRecibidos;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vida = jugador.GetComponent<VidaAPie>();
            datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            impactosRecibidos = 0f;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                if (objetivo == null && info.Tipo == TipoDanio.Golpe) impactosRecibidos += resultado.DanioAplicado;
            };
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(objetoJefe);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void CrearJefe(Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            prefab.SetActive(false);
            objetoJefe = Object.Instantiate(prefab, posicion, Quaternion.LookRotation(-Vector3.forward));
            prefab.SetActive(true);
            SerializedObject so = new SerializedObject(objetoJefe.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
            objetoJefe.SetActive(true);
            Physics.SyncTransforms();
            jefe = objetoJefe.GetComponent<PandilleroAPie>();
            jefe.MarcarComoJefe();
            jefe.Alerta = true;
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        private IEnumerator HastaFase(ReglaJefeNova.Fase fase, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (jefe.FaseJefe != fase && Time.time < fin) yield return null;
            Assert.AreEqual(fase, jefe.FaseJefe, "el jefe llega a la fase " + fase);
        }

        private IEnumerator Agacharse()
        {
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(0.4f);
            Assert.IsTrue(jugador.GetComponent<CubiertaYRodada>().AgachadoLibre, "agachado");
        }

        private IEnumerator RodarALaIzquierda()
        {
            Press(teclado.aKey);
            yield return Esperar(0.1f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<CubiertaYRodada>().Rodando, "rueda");
            Release(teclado.aKey);
        }

        [UnityTest]
        public IEnumerator Quieto_ElJefeTelegrafiaConBrilloYTePegaLaCantidadDelGolpe_YLuegoSeDejaCastigar()
        {
            yield return Esperar(0.5f);
            Assert.IsTrue(datos.JefeCuerpoACuerpo, "el Capitán Nova pelea cuerpo a cuerpo");
            CrearJefe(new Vector3(0f, 0.05f, 7f));
            float vidaAntes = vida.Actual;
            yield return HastaFase(ReglaJefeNova.Fase.TelegrafiaGolpe, 6f);
            yield return Esperar(0.45f);
            Assert.IsNotNull(jefe.BrilloJefe, "telegrafía con brillo");
            Assert.IsTrue(jefe.BrilloJefe.enabled);
            Assert.Greater(jefe.BrilloJefe.intensity, 1f);
            Assert.AreEqual(vidaAntes, vida.Actual, 0.001f, "durante la pausa todavía no pega");
            CapturaS1.Foto("s1t-T-693-jefe-telegrafia", jugador.transform, new Vector3(2.4f, 1.8f, -2.2f), new Vector3(0f, 1.1f, 3f), 55f, 900, 900);
            yield return HastaFase(ReglaJefeNova.Fase.Recupera, 2f);
            yield return null;
            Assert.AreEqual(vidaAntes - datos.DanioGolpeJefe, vida.Actual, 0.5f, "el golpe de área le quita " + datos.DanioGolpeJefe + " de vida");
            Assert.IsTrue(jefe.Castigable, "recuperándose se le puede castigar");
            float vidaJefe = jefe.Actual;
            jefe.RecibirDanio(new InfoDanio(10f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null));
            Assert.AreEqual(10f * datos.MultiplicadorCastigo, vidaJefe - jefe.Actual, 0.01f, "castigado en la recuperación recibe más daño");
        }

        [UnityTest]
        public IEnumerator Agachado_SiRuedaDuranteLaTelegrafia_EsquivaElGolpe()
        {
            yield return Esperar(0.5f);
            yield return Agacharse();
            CrearJefe(new Vector3(0f, 0.05f, 7f));
            float vidaAntes = vida.Actual;
            yield return HastaFase(ReglaJefeNova.Fase.TelegrafiaGolpe, 6f);
            yield return Esperar(0.4f);
            yield return RodarALaIzquierda();
            yield return HastaFase(ReglaJefeNova.Fase.Recupera, 2f);
            yield return Esperar(0.3f);
            Assert.AreEqual(vidaAntes, vida.Actual, 0.001f, "rodando agachado esquivó el golpe");
            Assert.AreEqual(0f, impactosRecibidos, 0.001f);
            Assert.IsTrue(jefe.Castigable, "y el jefe quedó recuperándose: ahora es tu turno");
        }

        [UnityTest]
        public IEnumerator CadaTresGolpes_CargaEnLinea_ElJugadorQuietoRecibeLaCarga_YRodandoLaEsquiva()
        {
            yield return Esperar(0.5f);
            CrearJefe(new Vector3(0f, 0.05f, 6f));
            // Quieto recibe tres golpes; tras el tercero se aleja para provocar la carga.
            float fin = Time.time + 20f;
            while (jefe.GolpesDesdeCarga < 3 && Time.time < fin)
            {
                vida.CurarTodo();
                yield return null;
            }
            Assert.AreEqual(3, jefe.GolpesDesdeCarga, "tres golpes dados");
            yield return HastaFase(ReglaJefeNova.Fase.Recupera, 3f);
            // Se queda recuperando; el jugador se aleja a 9 m y se agacha.
            jugador.GetComponent<JugadorAPie>().Colocar(new Pose(jefe.transform.position + jefe.transform.forward * 8f, Quaternion.identity));
            yield return Agacharse();
            vida.CurarTodo();
            yield return HastaFase(ReglaJefeNova.Fase.TelegrafiaCarga, 8f);
            yield return Esperar(0.5f);
            Assert.IsTrue(jefe.BrilloJefe.enabled, "la carga también telegrafía con brillo");
            CapturaS1.Foto("s1t-T-693-jefe-carga", jugador.transform, new Vector3(2.0f, 1.6f, -2.0f), new Vector3(0f, 1.2f, 7f), 55f, 900, 900);
            yield return HastaFase(ReglaJefeNova.Fase.Carga, 3f);
            float vidaAntes = vida.Actual;
            yield return RodarALaIzquierda();
            yield return HastaFase(ReglaJefeNova.Fase.RecuperaCarga, 3f);
            yield return Esperar(0.2f);
            Assert.AreEqual(vidaAntes, vida.Actual, 0.001f, "rodando agachado esquivó la carga");
            Assert.IsTrue(jefe.Castigable, "tras la carga queda recuperándose");

            // La misma carga contra alguien que no esquiva pega el daño de la carga.
            jefe.RecibirDanio(new InfoDanio(0.01f, TipoDanio.Golpe, jefe.transform.position, Vector3.forward, null));
            Assert.Greater(datos.DanioCarga, datos.DanioGolpeJefe, "la carga pega más que el golpe");
        }

        [UnityTest]
        public IEnumerator Carga_SiNoEsquivas_PegaElDanioDeLaCarga()
        {
            yield return Esperar(0.5f);
            CrearJefe(new Vector3(0f, 0.05f, 6f));
            float fin = Time.time + 20f;
            while (jefe.GolpesDesdeCarga < 3 && Time.time < fin)
            {
                vida.CurarTodo();
                yield return null;
            }
            yield return HastaFase(ReglaJefeNova.Fase.Recupera, 3f);
            jugador.GetComponent<JugadorAPie>().Colocar(new Pose(jefe.transform.position + jefe.transform.forward * 8f, Quaternion.identity));
            vida.CurarTodo();
            yield return HastaFase(ReglaJefeNova.Fase.Carga, 8f);
            float vidaAntes = vida.Actual;
            yield return HastaFase(ReglaJefeNova.Fase.RecuperaCarga, 3f);
            yield return Esperar(0.1f);
            Assert.AreEqual(vidaAntes - datos.DanioCarga, vida.Actual, 1f, "la carga te alcanza y pega " + datos.DanioCarga);
        }
    }
}
