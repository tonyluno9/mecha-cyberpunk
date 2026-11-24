using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// VR16: con <see cref="ModoVR.Activo"/> el paso 3 ("Lo molestan") no se juega: no hay SKULLS (<see cref="MolestiaSkulls"/>) y el director pasa
    /// directo al Maletín con el mismo estado (paso, guardado, candados, misión protegida, cinemática) que si la escena se hubiera jugado y terminado.
    /// VR24: ahora el paso 3 tampoco se juega sin VR (cambio de historia común): en los dos modos se salta igual.
    /// </summary>
    public class SaltarMolestiaVR16Pruebas
    {
        private const int Cuadros = 5;

        private struct Estado
        {
            public PasoRecorrido Paso;
            public string Guardado;
            public bool Policia, Pandillas, Nova, RoboCarros, Tiendas;
            public string TiendaAbierta;
            public string MotivoProtegida;
            public string Cinematica;

            public override string ToString() => Paso + " | " + Guardado + " | candados " + Policia + Pandillas + Nova + RoboCarros + Tiendas + " '" + TiendaAbierta
                + "' | protegida '" + MotivoProtegida + "' | cinemática '" + Cinematica + "'";
        }

        [SetUp]
        public void Preparar()
        {
            LogAssert.ignoreFailingMessages = true; // sin la ciudad, los pasos avisan que les faltan cosas
            ModoVR.Fijar(false);
        }

        [TearDown]
        public void Limpiar()
        {
            Destruir();
            ModoVR.Fijar(false);
            MisionProtegida.Desactivar();
            CandadosMundo.Liberar();
            ClimaMundo.Lluvia = 0f;
            LogAssert.ignoreFailingMessages = false;
        }

        private static void Destruir()
        {
            if (DirectorRecorrido.Actual != null)
            {
                Object.DestroyImmediate(DirectorRecorrido.Actual.gameObject);
            }
            // La C02 del primer camino no debe quedar en pantalla para el segundo.
            foreach (CinematicaPendiente c in Object.FindObjectsByType<CinematicaPendiente>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(c.gameObject);
            }
        }

        private static IEnumerator Esperar(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        private static void Saltar(PasoRecorrido p)
        {
            try
            {
                DirectorRecorrido.SaltarAPaso((int)p);
            }
            catch (Exception e)
            {
                Debug.Log("[VR16] El paso " + p + " no entró completo sin la ciudad: " + e.Message);
            }
        }

        private static Estado Leer(DirectorRecorrido d) => new Estado
        {
            Paso = d.Paso,
            Guardado = d.Guardar(),
            Policia = CandadosMundo.Policia,
            Pandillas = CandadosMundo.Pandillas,
            Nova = CandadosMundo.Nova,
            RoboCarros = CandadosMundo.RoboCarros,
            Tiendas = CandadosMundo.Tiendas,
            TiendaAbierta = CandadosMundo.TiendaAbierta,
            MotivoProtegida = MisionProtegida.Motivo,
            Cinematica = CinematicaPendiente.IdActual,
        };

        // Desde Despierta entra a "Lo molestan" y, si la escena de los SKULLS sigue (camino normal), la termina como al irse los SKULLS.
        private static IEnumerator Recorrer(bool vr, List<PasoRecorrido> pasos, Action<DirectorRecorrido, bool> fin)
        {
            ModoVR.Fijar(vr, vr);
            DirectorRecorrido d = DirectorRecorrido.Asegurar();
            d.PasoCambio += pasos.Add;
            Saltar(PasoRecorrido.Despierta);
            yield return Esperar(Cuadros);
            Saltar(PasoRecorrido.Molestan);
            bool habiaSkulls = d.GetComponent<MolestiaSkulls>() != null;
            yield return Esperar(Cuadros);
            if (d.Paso == PasoRecorrido.Molestan)
            {
                // La escena de los SKULLS se armó (camino normal): termina como cuando se van.
                MethodInfo terminar = typeof(DirectorRecorrido).GetMethod("AlTerminarMolestia", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(terminar, "DirectorRecorrido.AlTerminarMolestia existe");
                terminar.Invoke(d, null);
                yield return Esperar(Cuadros);
            }
            fin(d, habiaSkulls);
        }

        [Test]
        public void SoloMolestanYRanura_IgualEnPCYVR()
        {
            foreach (PasoRecorrido p in Enum.GetValues(typeof(PasoRecorrido)))
            {
                bool esperado = p == PasoRecorrido.Molestan || p == PasoRecorrido.Ranura;
                Assert.AreEqual(esperado, GestorCandados.SeSaltaEnVR(p, false), "Sin VR " + p);
                Assert.AreEqual(esperado, GestorCandados.SeSaltaEnVR(p, true), "En VR " + p);
            }
        }

        [UnityTest]
        public IEnumerator EnVR_SeSaltaLoMolestan_MismoEstadoQueJugado()
        {
            yield return null;

            var pasosNormal = new List<PasoRecorrido>();
            Estado normal = default;
            bool saltadaNormal = true;
            yield return Recorrer(false, pasosNormal, (d, skulls) =>
            {
                normal = Leer(d);
                saltadaNormal = d.MolestiaSaltadaEnVR;
            });
            Debug.Log("[VR16] Normal: " + normal + " pasos=" + string.Join(",", pasosNormal));
            Destruir();
            MisionProtegida.Desactivar();
            CandadosMundo.Liberar();
            yield return Esperar(2);

            var pasosVR = new List<PasoRecorrido>();
            Estado enVR = default;
            bool saltadaVR = false;
            bool skullsVR = true;
            bool hayMolestiaVR = true;
            yield return Recorrer(true, pasosVR, (d, skulls) =>
            {
                enVR = Leer(d);
                saltadaVR = d.MolestiaSaltadaEnVR;
                skullsVR = skulls;
                hayMolestiaVR = d.GetComponent<MolestiaSkulls>() != null;
            });
            Debug.Log("[VR16] VR: " + enVR + " pasos=" + string.Join(",", pasosVR));

            Assert.IsTrue(saltadaNormal, "VR24: sin VR el paso 3 también se salta");
            Assert.IsTrue(saltadaVR, "En VR el paso 3 se salta");
            Assert.IsFalse(skullsVR, "En VR no se arma la escena de los SKULLS");
            Assert.IsFalse(hayMolestiaVR, "En VR no queda MolestiaSkulls");

            Assert.AreEqual(PasoRecorrido.Maletin, enVR.Paso, "En VR sigue directo al Maletín");
            int iMolestan = pasosVR.IndexOf(PasoRecorrido.Molestan);
            Assert.GreaterOrEqual(iMolestan, 0, "En VR el paso 3 se anuncia (los que escuchan PasoCambio no se pierden)");
            Assert.AreEqual(PasoRecorrido.Maletin, pasosVR[iMolestan + 1], "En VR, de Lo molestan pasa directo al Maletín");

            Assert.AreEqual(normal.Paso, enVR.Paso, "Mismo paso que jugado");
            Assert.AreEqual(normal.Guardado, enVR.Guardado, "Mismo guardado que jugado");
            Assert.AreEqual(normal.Policia, enVR.Policia, "Candado policía");
            Assert.AreEqual(normal.Pandillas, enVR.Pandillas, "Candado pandillas");
            Assert.AreEqual(normal.Nova, enVR.Nova, "Candado NOVA");
            Assert.AreEqual(normal.RoboCarros, enVR.RoboCarros, "Candado carros");
            Assert.AreEqual(normal.Tiendas, enVR.Tiendas, "Candado tiendas");
            Assert.AreEqual(normal.TiendaAbierta, enVR.TiendaAbierta, "Tienda abierta");
            Assert.AreEqual(normal.MotivoProtegida, enVR.MotivoProtegida, "Misión protegida igual (apagada)");
            Assert.AreEqual(string.Empty, enVR.MotivoProtegida, "Sin misión protegida de los SKULLS");
            Assert.AreEqual(normal.Cinematica, enVR.Cinematica, "Misma cinemática en curso (C02)");
        }
    }
}
