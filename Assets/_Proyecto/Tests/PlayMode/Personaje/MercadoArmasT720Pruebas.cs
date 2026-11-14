using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-720: en El Mercado (callejón del mercado negro) se compran escopeta, fusil y sniper y las balas de cada una con flechas y Enter.
    /// Avisa "NO TE ALCANZA" sin dinero y el máximo con la reserva llena; la recarga gasta la reserva y sin ella no recarga.
    /// Con CAPTURAR_T720=1 deja docs/capturas/s1c-T-720-mercado.png.
    /// </summary>
    public class MercadoArmasT720Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T720";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const int Dinero = 10000;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject plataforma;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (plataforma != null)
            {
                Object.Destroy(plataforma);
            }
            base.TearDown();
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

        private static Pose Mirando(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = hacia - desde;
            dir.y = 0f;
            return new Pose(desde, Quaternion.LookRotation(dir.normalized, Vector3.up));
        }

        // La fila del arma en su nivel base (no la de "<nombre> I" ni la de balas).
        private static int FilaBase(PantallaTienda pantalla, string nombre)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                string texto = pantalla.TextoFila(i).ToUpperInvariant();
                if (texto.Contains(nombre) && !texto.Contains(nombre + " I") && !texto.Contains("BALAS"))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FilaCon(PantallaTienda pantalla, string texto)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).ToUpperInvariant().Contains(texto))
                {
                    return i;
                }
            }
            return -1;
        }

        // Elige la fila con las flechas y compra con Enter.
        private IEnumerator Comprar(PantallaTienda pantalla, int fila, string que)
        {
            Assert.GreaterOrEqual(fila, 0, "La tienda ofrece " + que);
            int pasos = 0;
            while (pantalla.IndiceElegido != fila && pasos++ < 60)
            {
                yield return Pulsar(pantalla.IndiceElegido < fila ? teclado.downArrowKey : teclado.upArrowKey);
            }
            Assert.AreEqual(fila, pantalla.IndiceElegido, "Las flechas llegan a " + que);
            yield return Pulsar(teclado.enterKey);
        }

        private static Transform Buscar(string nombre)
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

        private void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = Registro<IJugador>.Actual.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay && lienzo.enabled)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = 1f;
                    movidos.Add(lienzo);
                }
            }
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            Texture2D imagen = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
        }

        private static void QuitarExpendedoras()
        {
            GameObject raiz = GameObject.Find(ColocadorExpendedoras.NombreRaiz);
            if (raiz != null)
            {
                Object.DestroyImmediate(raiz);
            }
        }

        [UnityTest]
        public IEnumerator ComprarCadaArmaYSusBalas()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null
                && Registro<ICartera>.Actual != null && Registro<ReservaBalas>.Actual != null, "jugador, arsenal, cartera y reserva", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            ArmasAPie armas = Object.FindFirstObjectByType<ArmasAPie>();
            yield return new WaitForSecondsRealtime(1f);
            QuitarExpendedoras();

            cartera.Abonar(Dinero - cartera.Dinero, MotivoDinero.Otro);

            // a. Camina al mostrador de El Mercado y E abre la tienda.
            Transform lugar = Buscar("Lugar_MercadoNegro");
            Transform puerta = null;
            foreach (Transform t in lugar.GetComponentsInChildren<Transform>(true))
            {
                puerta = t.name == "Puerta" ? t : puerta;
            }
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                mostrador = m.name.Contains("MercadoNegro") ? m : mostrador;
            }
            Assert.IsNotNull(puerta);
            Assert.IsNotNull(mostrador);
            Vector3 haciaAdentro = lugar.position - puerta.position;
            haciaAdentro.y = 0f;
            haciaAdentro.Normalize();
            Vector3 inicio = puerta.position - haciaAdentro * 5f;
            inicio.y = puerta.position.y + 0.1f;
            jugador.Colocar(Mirando(inicio, puerta.position));
            yield return null;
            yield return null;
            Press(teclado.wKey);
            float fin = Time.realtimeSinceStartup + 25f;
            while ((mostrador.transform.position - jugador.Raiz.position).sqrMagnitude > 2.2f * 2.2f && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return new WaitForSecondsRealtime(0.3f);
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta && pantalla.Visible, "abrir El Mercado con E", 5f);

            // b. Sin el arma no se ofrecen sus balas.
            string[] nombres = { "ESCOPETA", "FUSIL", "SNIPER" };
            ArmaAPie[] tipos = { ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper };
            foreach (string arma in nombres)
            {
                Assert.AreEqual(-1, FilaCon(pantalla, "BALAS DE " + arma), "Sin " + arma + " no hay balas de " + arma);
            }

            // c. Compra las tres armas con flechas y Enter; cada una cuesta lo que dice la fila.
            int[] precios = { 350, 700, 900 };
            int dinero = Dinero;
            for (int i = 0; i < nombres.Length; i++)
            {
                Assert.IsFalse(arsenal.Tiene(tipos[i]), "Empieza sin " + nombres[i]);
                yield return Comprar(pantalla, FilaBase(pantalla, nombres[i]), nombres[i]);
                Assert.IsTrue(arsenal.Tiene(tipos[i]), "Enter compra " + nombres[i]);
                dinero -= precios[i];
                Assert.AreEqual(dinero, cartera.Dinero, "Cobra el precio de " + nombres[i]);
            }
            yield return new WaitForSecondsRealtime(0.4f);
            Capturar("s1c-T-720-mercado.png");

            // d. Ahora sí ofrece las balas de cada arma, con la reserva inicial que da el arma.
            int[] porPaquete = new int[3];
            int[] precioBalas = { 60, 120, 150 };
            for (int i = 0; i < nombres.Length; i++)
            {
                int fila = FilaCon(pantalla, "BALAS DE " + nombres[i]);
                Assert.GreaterOrEqual(fila, 0, "Con " + nombres[i] + " ya hay balas de " + nombres[i]);
                StringAssert.Contains(precioBalas[i].ToString(), pantalla.TextoFila(fila));
                DatosMunicionArma datos = ReservaBalas.Datos(tipos[i]);
                Assert.IsNotNull(datos);
                porPaquete[i] = datos.BalasPorPaquete;
                Assert.AreEqual(datos.BalasIniciales, reserva.Cantidad(tipos[i]), "Reserva inicial de " + nombres[i]);
            }

            // e. Compra un paquete de balas de cada arma: suma a la reserva y cobra.
            for (int i = 0; i < nombres.Length; i++)
            {
                int antes = reserva.Cantidad(tipos[i]);
                yield return Comprar(pantalla, FilaCon(pantalla, "BALAS DE " + nombres[i]), "balas de " + nombres[i]);
                Assert.AreEqual(antes + porPaquete[i], reserva.Cantidad(tipos[i]), "Un paquete suma sus balas de " + nombres[i]);
                dinero -= precioBalas[i];
                Assert.AreEqual(dinero, cartera.Dinero, "Cobra las balas de " + nombres[i]);
                StringAssert.Contains("COMPRASTE", pantalla.Aviso);
            }

            // f. Sin dinero: avisa que no alcanza y no da balas.
            cartera.IntentarPagar(cartera.Dinero, MotivoDinero.Otro);
            Assert.AreEqual(0, cartera.Dinero);
            int reservaFusil = reserva.Cantidad(ArmaAPie.Fusil);
            yield return Comprar(pantalla, FilaCon(pantalla, "BALAS DE FUSIL"), "balas de fusil");
            StringAssert.Contains("NO TE ALCANZA", pantalla.Aviso);
            Assert.AreEqual(reservaFusil, reserva.Cantidad(ArmaAPie.Fusil), "Sin dinero no hay balas");

            // g. Con la reserva llena avisa el máximo y no cobra.
            cartera.Abonar(Dinero, MotivoDinero.Otro);
            reserva.Agregar(ArmaAPie.Sniper, 999);
            Assert.AreEqual(ReservaBalas.Maximo(ArmaAPie.Sniper), reserva.Cantidad(ArmaAPie.Sniper));
            yield return Comprar(pantalla, FilaCon(pantalla, "BALAS DE SNIPER"), "balas de sniper");
            Assert.AreEqual(Dinero, cartera.Dinero, "Con la reserva llena no cobra");
            Assert.AreNotEqual(string.Empty, pantalla.Aviso);
            StringAssert.DoesNotContain("COMPRASTE", pantalla.Aviso);
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !mostrador.PantallaAbierta, "cerrar la tienda con Esc", 5f);

            // h. Las armas compradas se sacan con 3, 4 y 5 y la recarga gasta la reserva.
            plataforma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plataforma.transform.position = new Vector3(4000f, 400f, 4000f);
            plataforma.transform.localScale = new Vector3(300f, 1f, 300f);
            Vector3 suelo = plataforma.transform.position + Vector3.up * 0.55f;
            Physics.SyncTransforms();
            jugador.Colocar(Mirando(suelo, suelo + Vector3.right));
            yield return new WaitForSecondsRealtime(0.5f);

            armas.Equipar(ArmaAPie.Escopeta); yield return null; // T-991: ya no hay tecla 3
            Assert.AreEqual(ArmaAPie.Escopeta, armas.ArmaActiva, "La escopeta comprada se equipa");
            Assert.AreEqual(4, armas.Balas);
            Time.timeScale = 8f;
            while (armas.Balas > 0)
            {
                yield return Pulsar(raton.leftButton);
                yield return new WaitForSeconds(1f);
            }
            int reservaAntes = reserva.Cantidad(ArmaAPie.Escopeta);
            yield return Pulsar(teclado.rKey);
            yield return EsperarHasta(() => !armas.Recargando && armas.Balas == 4, "recargar la escopeta", 10f);
            Time.timeScale = 1f;
            Assert.AreEqual(reservaAntes - 4, reserva.Cantidad(ArmaAPie.Escopeta), "La recarga gasta 4 balas de la reserva");
            Assert.AreEqual(reservaAntes - 4, armas.Reserva, "El arma en la mano informa su reserva");

            // i. Sin reserva no hay recarga.
            reserva.Tomar(ArmaAPie.Escopeta, 999);
            Time.timeScale = 8f;
            while (armas.Balas > 0)
            {
                yield return Pulsar(raton.leftButton);
                yield return new WaitForSeconds(1f);
            }
            Time.timeScale = 1f;
            yield return Pulsar(teclado.rKey);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(armas.Recargando, "Sin balas de reserva R no recarga");
            Assert.AreEqual(0, armas.Balas);

            armas.Equipar(ArmaAPie.Fusil); yield return null;
            Assert.AreEqual(ArmaAPie.Fusil, armas.ArmaActiva, "El fusil comprado se equipa");
            armas.Equipar(ArmaAPie.Sniper); yield return null;
            Assert.AreEqual(ArmaAPie.Sniper, armas.ArmaActiva, "El sniper comprado se equipa");
        }
    }
}
