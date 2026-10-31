using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
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
    /// T-430: con la vida llena, EL DOC (E) abre la tienda de implantes de I+D+I; el jugador compra OJOS I (Enter dos veces) y PIERNAS I, se cobra,
    /// el HUD muestra los iconos y los efectos a pie cambian (apuntado asistido más lejos, rodada con menos enfriamiento). Atajos: F6 salta la
    /// historia, Colocar mueve al jugador, el dinero y el reloj se fijan por código. Con CAPTURAR_T430=1 deja docs/capturas/T-430-1.png (1920x1080).
    /// </summary>
    public class ImplantesDocPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T430";
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

        private static void Capturar(IJugador jugador, string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            const int Ancho = 1920;
            const int Alto = 1080;
            Camera camara = jugador.Camara;
            var movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }
                lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                lienzo.worldCamera = camara;
                lienzo.planeDistance = 0.35f;
                movidos.Add(lienzo);
            }
            var rt = new RenderTexture(Ancho, Alto, 24);
            var imagen = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), imagen.EncodeToPNG());
            Object.Destroy(imagen);
            rt.Release();
            Object.Destroy(rt);
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        [UnityTest]
        public IEnumerator ComprarConElDocYNotarLosEfectos()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<ServicioImplantes>.Actual != null && Registro<IRelojMundo>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioImplantes servicio = Registro<ServicioImplantes>.Actual;
            Registro<IRelojMundo>.Actual.AvanzarHasta(12f);
            cartera.Abonar(20000 - cartera.Dinero, MotivoDinero.Otro);
            int dinero = cartera.Dinero;

            PersonajeApoyo doc = null;
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                doc = p.Datos.Id == "doc" ? p : doc;
            }
            Assert.IsNotNull(doc);
            Vector3 frente = doc.transform.position + doc.transform.forward * 1.6f;
            if (Physics.Raycast(frente + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f))
            {
                frente = piso.point;
            }
            frente.y += 0.1f;
            Vector3 hacia = doc.transform.position - frente;
            hacia.y = 0f;
            jugador.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return new WaitForSecondsRealtime(0.5f);

            // Sin implantes todo es neutro y el HUD no dibuja iconos.
            HudImplantes hud = Object.FindFirstObjectByType<HudImplantes>();
            Assert.IsNotNull(hud);
            Assert.AreEqual(0, hud.Visibles);
            Assert.AreEqual(1f, EfectosAPie.FactorAlcanceApuntado(servicio.Efectos), 0.0001f);

            // Vida llena: EL DOC ofrece IMPLANTES; E abre la tienda.
            Assert.AreEqual("IMPLANTES", doc.Texto);
            MostradorClinica mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            Assert.IsNotNull(mostrador);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta && mostrador.Pantalla.Visible, "abrir la tienda con E", 5f);
            yield return new WaitForSecondsRealtime(0.5f);
            Capturar(jugador, "T-430-1.png");

            // OJOS I: Enter pide confirmar, Enter instala.
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(mostrador.Pantalla.Confirmando);
            yield return Pulsar(teclado.enterKey);
            yield return EsperarHasta(() => servicio.Nivel(EspacioImplante.Ojos) == 1 && !mostrador.Operando, "operación de ojos", 30f);
            Assert.Less(cartera.Dinero, dinero, "Se cobró");
            Assert.Greater(EfectosAPie.FactorAlcanceApuntado(servicio.Efectos), 1f, "OJOS alarga el apuntado asistido");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(1, hud.Visibles);
            Assert.AreEqual("O1", hud.TextoIcono(EspacioImplante.Ojos));

            // PIERNAS I (tercera fila): ahora la rodada espera menos.
            Assert.AreEqual("IMPLANTES", doc.Texto);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta, "reabrir la tienda", 5f);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.enterKey);
            yield return Pulsar(teclado.enterKey);
            yield return EsperarHasta(() => servicio.Nivel(EspacioImplante.Piernas) == 1 && !mostrador.Operando, "operación de piernas", 30f);
            Assert.Less(EfectosAPie.EnfriamientoRodada(2f, servicio.Efectos), 2f, "PIERNAS acorta el enfriamiento de la rodada");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(2, hud.Visibles);
            Assert.AreEqual("P1", hud.TextoIcono(EspacioImplante.Piernas));
        }
    }
}
