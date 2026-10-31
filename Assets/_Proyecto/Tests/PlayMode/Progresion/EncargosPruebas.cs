using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-426: los encargos del juego libre como un jugador. El tablero no ofrece nada antes del juego libre; en el juego libre E acepta COBRAR CUOTA
    /// (se cobra en los 3 locales, el que se resiste manda un moroso que hay que vencer), paga y deja enfriamiento; DEFENDER ZONA suelta una oleada de 4 rivales.
    /// Atajos anotados: F6 salta la historia hasta el juego libre, el jugador se coloca en cada punto con Colocar y los rivales caen por IRecibeDanio.
    /// Con CAPTURAR_T426=1 y el editor abierto deja docs/capturas/T-426-1.png.
    /// </summary>
    public class EncargosPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float DanoMatar = 100000f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static string Objetivo => Registro<IObjetivos>.Actual.HayObjetivo ? Registro<IObjetivos>.Actual.Actual.Texto : string.Empty;

        private static IEnumerator Poner(IJugador jugador, Vector3 punto)
        {
            jugador.Colocar(new Pose(punto + Vector3.up * 0.1f, Quaternion.identity));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private static void Matar(PandilleroAPie p)
        {
            p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
        }

        private static Transform Hijo(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private IEnumerator Arrancar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                    && Registro<ICartera>.Actual != null && Registro<IObjetivos>.Actual != null;
            }, "el director arrancó", 30f);
        }

        private IEnumerator HastaJuegoLibre(DirectorPandillas director)
        {
            for (int i = 0; i < 12 && director.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, director.Paso);
        }

        [UnityTest]
        public IEnumerator CobrarCuotaYDefenderZona()
        {
            yield return Arrancar();
            DirectorPandillas historia = Object.FindFirstObjectByType<DirectorPandillas>();
            DirectorEncargos encargos = Object.FindFirstObjectByType<DirectorEncargos>();
            Assert.IsNotNull(encargos, "La escena trae el DirectorEncargos");
            TableroEncargos tablero = Object.FindFirstObjectByType<TableroEncargos>();
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;

            // Durante la historia el tablero no ofrece nada.
            Assert.IsFalse(tablero.PuedeUsar, "Con la historia en curso el tablero no ofrece encargos");
            Assert.IsFalse(encargos.Iniciar(TipoEncargo.Cuota));

            yield return HastaJuegoLibre(historia);
            yield return Poner(jugador, tablero.Posicion + tablero.transform.forward * -1.5f);
            Assert.IsTrue(tablero.PuedeUsar);
            StringAssert.Contains("COBRAR CUOTA", tablero.Texto);

            // E en el tablero: empieza COBRAR CUOTA con su marcador.
            int dineroAntes = cartera.Dinero;
            tablero.Usar();
            yield return null;
            Assert.IsTrue(encargos.HayEncargo);
            Assert.AreEqual(DirectorEncargos.TextoCuota(0, 3), Objetivo);
            Assert.IsTrue(Registro<IObjetivos>.Actual.Actual.TienePosicion, "El objetivo tiene marca");
            Assert.IsFalse(encargos.Iniciar(TipoEncargo.Defender), "No hay dos encargos a la vez");

            // Cobra en los 3 locales; si alguien se resiste, se vence al moroso.
            for (int i = 0; i < 3; i++)
            {
                Transform local = Hijo("Local_" + (i + 1));
                yield return Poner(jugador, local.position);
                if (Objetivo == DirectorEncargos.TextoMoroso)
                {
                    PandilleroAPie moroso = Hijo("RivalEncargo_1").GetComponent<PandilleroAPie>();
                    Assert.IsTrue(moroso.gameObject.activeSelf, "El moroso aparece");
                    Matar(moroso);
                    yield return EsperarHasta(() => Objetivo != DirectorEncargos.TextoMoroso, "cobrar tras vencer al moroso", 5f);
                }
            }
            yield return EsperarHasta(() => !encargos.HayEncargo, "terminar la cuota", 5f);
            Assert.GreaterOrEqual(cartera.Dinero - dineroAntes, ReglaEncargos.Premio(TipoEncargo.Cuota) + 3 * ReglaEncargos.CuotaPorLocal, "Cuotas y premio en Biits");
            Assert.IsFalse(encargos.Regla.Disponible(TipoEncargo.Cuota), "Enfriamiento antes de repetir");
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo);
            StringAssert.Contains("DEFENDER ZONA", tablero.Texto, "El tablero ofrece el siguiente tipo");

            // DEFENDER ZONA: llega al punto, salen 4 rivales y al vencerlos paga.
            dineroAntes = cartera.Dinero;
            yield return Poner(jugador, tablero.Posicion + tablero.transform.forward * -1.5f);
            tablero.Usar();
            yield return null;
            Assert.AreEqual(DirectorEncargos.TextoIrDefender, Objetivo);
            Transform punto = Hijo("PuntoDefensa");
            yield return Poner(jugador, punto.position);
            yield return EsperarHasta(() => Objetivo == DirectorEncargos.TextoDefender(0, DirectorEncargos.RivalesOleada), "la oleada", 5f);
            yield return Capturar();
            int vivos = 0;
            for (int i = 0; i < DirectorEncargos.RivalesOleada; i++)
            {
                PandilleroAPie r = Hijo("RivalEncargo_" + (i + 1)).GetComponent<PandilleroAPie>();
                Assert.IsTrue(r.gameObject.activeSelf);
                Matar(r);
                vivos++;
            }
            Assert.AreEqual(DirectorEncargos.RivalesOleada, vivos);
            yield return EsperarHasta(() => !encargos.HayEncargo, "terminar la defensa", 5f);
            Assert.AreEqual(ReglaEncargos.Premio(TipoEncargo.Defender), cartera.Dinero - dineroAntes);
            Assert.IsFalse(encargos.Regla.Disponible(TipoEncargo.Defender));

            // El guardado conserva los enfriamientos.
            string guardado = encargos.Guardar();
            ReglaEncargos leida = new ReglaEncargos();
            Assert.IsTrue(leida.Cargar(guardado));
            Assert.IsFalse(leida.Disponible(TipoEncargo.Cuota));
            Assert.AreEqual(1, leida.Hechos(TipoEncargo.Defender));
        }

        [UnityTest]
        public IEnumerator RobarCarroYEntregarlo()
        {
            yield return Arrancar();
            DirectorPandillas historia = Object.FindFirstObjectByType<DirectorPandillas>();
            DirectorEncargos encargos = Object.FindFirstObjectByType<DirectorEncargos>();
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            yield return HastaJuegoLibre(historia);

            int dineroAntes = cartera.Dinero;
            Assert.IsTrue(encargos.Iniciar(TipoEncargo.Robar), "Hay un CarroRobable libre");
            Assert.AreEqual(DirectorEncargos.TextoRobar, Objetivo);

            // Sube al carro marcado con E.
            Vector3 posCarro = Registro<IObjetivos>.Actual.Actual.Posicion;
            IMontable carro = null;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                Component c = m as Component;
                if (c != null && (c.transform.position - posCarro).sqrMagnitude < 0.01f)
                {
                    carro = m;
                }
            }
            Assert.IsNotNull(carro);
            Vector3 puerta = carro.PuntoSubida;
            Vector3 hacia = puerta - posCarro;
            hacia.y = 0f;
            Vector3 parado = puerta + hacia.normalized * 1.2f;
            parado.y = jugador.Raiz.position.y;
            Vector3 mirar = puerta - parado;
            mirar.y = 0f;
            jugador.Colocar(new Pose(parado, Quaternion.LookRotation(mirar.normalized, Vector3.up)));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => Objetivo == DirectorEncargos.TextoEntregar, "llevarlo con CANEPA", 5f);

            // Lo lleva al taller (el carro se coloca junto a CANEPA).
            PersonajeApoyoCerca(out Vector3 taller);
            Transform carroT = ((Component)carro).transform;
            carroT.position = taller + Vector3.right * 2f;
            yield return EsperarHasta(() => !encargos.HayEncargo, "entregar el carro", 5f);
            Assert.AreEqual(ReglaEncargos.Premio(TipoEncargo.Robar), cartera.Dinero - dineroAntes);
            Assert.IsFalse(encargos.Regla.Disponible(TipoEncargo.Robar));
        }

        private static void PersonajeApoyoCerca(out Vector3 posicion)
        {
            posicion = Vector3.zero;
            foreach (MechaCyberpunk.Progresion.Apoyo.PersonajeApoyo p in Object.FindObjectsByType<MechaCyberpunk.Progresion.Apoyo.PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (p.Datos.Id == "mecanico")
                {
                    posicion = p.Posicion;
                }
            }
        }

        // Foto de la vista Game (solo con CAPTURAR_T426=1 y el editor abierto: OnGUI no corre en batchmode).
        private static IEnumerator Capturar()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T426") != "1" || Application.isBatchMode)
            {
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine("docs", "capturas", "T-426-1.png"), foto.EncodeToPNG());
            Object.Destroy(foto);
        }
    }
}
