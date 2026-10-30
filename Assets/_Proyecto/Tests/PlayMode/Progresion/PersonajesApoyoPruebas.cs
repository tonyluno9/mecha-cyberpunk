using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Tiendas;
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
    /// T-417: el recorrido de un jugador con Ƀ500, vida 40 y calor nivel 2 en Ciudad_Prologo. Hay 5 personajes de apoyo sobre el piso y cerca de su lugar;
    /// E con EL DOC cura y cobra Ƀ50, con EL MECÁNICO borra el calor y cobra Ƀ100, con EL INVENTOR abre una tienda de 15 mejoras (Esc la cierra),
    /// y EL HACKER y EL DIRECTOR sacan un subtítulo. De noche hay 6 peatones o menos y de día 4 o menos.
    /// Con CAPTURAR_T417=1 deja docs/capturas/T-417-1.png (el CIC) y T-417-2.png (la calle principal de noche).
    /// </summary>
    public class PersonajesApoyoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T417";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const float DistanciaFrente = 1.6f;

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

        private static PersonajeApoyo Personaje(string id)
        {
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (p.Datos.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        private static int PeatonesPrendidos()
        {
            int total = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "PeatonMundo" && mb.gameObject.activeInHierarchy)
                {
                    total++;
                }
            }
            return total;
        }

        // El jugador aparece frente al personaje (donde mira el personaje), mirándolo, y espera un momento.
        private IEnumerator PonerFrente(IJugador jugador, PersonajeApoyo p, float distancia = DistanciaFrente)
        {
            Vector3 frente = p.transform.position + p.transform.forward * distancia;
            if (Physics.Raycast(frente + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f))
            {
                frente = piso.point;
            }
            frente.y += 0.1f;
            Vector3 hacia = p.transform.position - frente;
            hacia.y = 0f;
            jugador.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private void Capturar(IJugador jugador, string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = jugador.Camara;
            // El muñeco del jugador tapa la vista en tercera persona: se apaga solo para la foto.
            List<Renderer> ocultos = new List<Renderer>();
            foreach (Renderer r in jugador.Raiz.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    ocultos.Add(r);
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
            foreach (Renderer r in ocultos)
            {
                r.enabled = true;
            }
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
        }

        [UnityTest]
        public IEnumerator RecorridoProfes()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null && Registro<ICalor>.Actual != null
                && Registro<IJugador>.Actual.Raiz.GetComponentInChildren<VidaAPie>() != null, "jugador, cartera, calor y vida", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            VidaAPie vida = jugador.Raiz.GetComponentInChildren<VidaAPie>();
            yield return new WaitForSecondsRealtime(1f);

            cartera.Abonar(500 - cartera.Dinero, MotivoDinero.Otro);
            vida.RecibirDanio(new InfoDanio(vida.Maxima - 40f, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual(40f, vida.Actual, 0.5f, "Vida 40");
            for (int i = 0; i < 200 && calor.Nivel < 2; i++)
            {
                calor.Sumar(5f, MotivoCalor.Otro);
            }
            Assert.AreEqual(2, calor.Nivel, "Calor nivel 2");

            // a. Exactamente 5 personajes, sobre el piso y cerca de su lugar.
            Assert.AreEqual(5, Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None).Length, "Hay 5 personajes de apoyo");
            Transform casa = Buscar("Casa_Puerta");
            Transform carro = null;
            float mejor = float.MaxValue;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "CarroEstacionado" && (mb.transform.position - casa.position).sqrMagnitude < mejor)
                {
                    mejor = (mb.transform.position - casa.position).sqrMagnitude;
                    carro = mb.transform;
                }
            }
            var lugares = new Dictionary<string, Transform>
            {
                { "inventor", Buscar("Lugar_CIC") }, { "doc", Buscar("EntradaClinica") }, { "hacker", Buscar("Lugar_Casino") },
                { "mecanico", carro }, { "director", Buscar("Lugar_Bar") }
            };
            foreach (KeyValuePair<string, Transform> par in lugares)
            {
                PersonajeApoyo p = Personaje(par.Key);
                Assert.IsNotNull(p, "Existe " + par.Key);
                Assert.IsNotNull(par.Value, "Existe el lugar de " + par.Key);
                Assert.IsNotEmpty(p.Datos.Titulo);
                Assert.IsNotEmpty(p.Datos.Nombre);
                Assert.IsTrue(Physics.Raycast(p.transform.position + Vector3.up * 0.3f, Vector3.down, 0.6f), par.Key + " está sobre el piso");
                Assert.Less(Vector3.Distance(p.transform.position, par.Value.position), 3f, par.Key + " está a menos de 3 m de su lugar");
            }

            // b. EL DOC: vida 40 y Ƀ500 -> vida máxima y Ƀ450 (Ƀ50 de cobro).
            PersonajeApoyo doc = Personaje("doc");
            yield return PonerFrente(jugador, doc);
            Assert.IsTrue(doc.PuedeUsar);
            StringAssert.Contains("CURARTE", doc.Texto);
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.5f, "El Doc deja la vida al máximo");
            Assert.AreEqual(450, cartera.Dinero, "El Doc cobra Ƀ50");
            Assert.AreEqual("ESTÁS BIEN, MIJO", doc.Texto, "Con la vida llena dice que estás bien");

            // c. EL MECÁNICO: calor 2 -> 0 y cobra Ƀ100.
            PersonajeApoyo mecanico = Personaje("mecanico");
            yield return PonerFrente(jugador, mecanico);
            Assert.IsTrue(mecanico.PuedeUsar);
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(0, calor.Nivel, "El Mecánico borra el calor");
            Assert.AreEqual(350, cartera.Dinero, "El Mecánico cobra Ƀ100");
            Assert.AreEqual("TU CARRO ESTÁ LIMPIO", mecanico.Texto);

            // d. EL INVENTOR: la tienda con 15 mejoras y ninguna arma; Esc la cierra.
            PersonajeApoyo inventor = Personaje("inventor");
            yield return PonerFrente(jugador, inventor, 2.2f);
            MostradorTienda cic = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                cic = m.name == "MostradorCIC" ? m : cic;
            }
            Assert.IsNotNull(cic, "Existe el mostrador del CIC");
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            Assert.IsNotNull(pantalla, "El mostrador del CIC trae su pantalla");
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => cic.PantallaAbierta && pantalla.Visible, "abrir la tienda del CIC con E", 5f);
            Assert.AreEqual(15, cic.EnVenta.Count, "El CIC vende 15 mejoras");
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                string fila = pantalla.TextoFila(i).ToUpperInvariant();
                Assert.IsTrue(fila.Contains(" I"), "Solo mejoras, no armas: " + fila);
            }
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !cic.PantallaAbierta, "cerrar la tienda con Esc", 5f);
            yield return new WaitForSecondsRealtime(0.3f);

            // e. EL HACKER y EL DIRECTOR sacan un subtítulo.
            PantallaHistoriaPandillas subtitulos = null;
            foreach (string id in new[] { "hacker", "director" })
            {
                PersonajeApoyo p = Personaje(id);
                yield return PonerFrente(jugador, p);
                Assert.IsTrue(p.PuedeUsar, id);
                yield return Pulsar(teclado.eKey);
                Assert.IsNotEmpty(p.UltimoTexto, id + " dice algo");
                subtitulos = Object.FindFirstObjectByType<PantallaHistoriaPandillas>();
                Assert.IsNotNull(subtitulos, "Hay pantalla de subtítulos");
                Assert.IsNotEmpty(subtitulos.Subtitulo, id + " sacó un subtítulo");
                StringAssert.Contains(p.Datos.Nombre, subtitulos.Subtitulo);
            }

            // f. Peatones: de día 4 o menos, de noche 6 o menos.
            Registro<IRelojMundo>.Actual.AvanzarHasta(12f);
            yield return new WaitForSecondsRealtime(3f);
            Assert.LessOrEqual(PeatonesPrendidos(), 4, "De día hay 4 peatones como máximo");
            Registro<IRelojMundo>.Actual.AvanzarHasta(22f);
            yield return new WaitForSecondsRealtime(3f);
            int deNoche = PeatonesPrendidos();
            Assert.LessOrEqual(deNoche, 6, "De noche hay 6 peatones como máximo");
            Debug.Log("[T-417] Peatones prendidos de noche: " + deNoche);

            // g. Capturas: el CIC con EL INVENTOR y la calle principal de noche.
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Vector3 desde = inventor.transform.position + inventor.transform.forward * 5f;
                if (Physics.Raycast(desde + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f))
                {
                    desde = piso.point;
                }
                desde.y += 0.1f;
                Vector3 mira = inventor.transform.position - desde;
                mira.y = 0f;
                jugador.Colocar(new Pose(desde, Quaternion.LookRotation(mira.normalized, Vector3.up)));
                yield return new WaitForSecondsRealtime(1f);
                Capturar(jugador, "T-417-1.png");
                jugador.Colocar(new Pose(new Vector3(-30f, 0.2f, -2f), Quaternion.LookRotation(new Vector3(1f, 0f, 0.12f), Vector3.up)));
                yield return new WaitForSecondsRealtime(1f);
                Capturar(jugador, "T-417-2.png");
            }
        }
    }
}
