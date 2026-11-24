using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR48: en VR, al subirse a un carro su carrocería y su interior no se dibujan para la cámara (solo-sombra), los colliders siguen igual y al
    /// bajar todo vuelve. En moto no se oculta nada.
    /// </summary>
    public class VR48CarroceriaOcultaPruebas : BaseJugadorVR
    {
        private const string RutaMoto = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_C19.prefab";

        [Test]
        public void OcultarCarroceria_SoloEnCarros()
        {
            Assert.IsTrue(ReglaAsientoVR.OcultarCarroceria(false));
            Assert.IsFalse(ReglaAsientoVR.OcultarCarroceria(true));
        }

        [UnityTest]
        public IEnumerator TresCarros_SeOcultanAlSubirYVuelvenAlBajar_ColliderIntactos(
            [Values("CarroViejo", "Carro_V01a", "Carro_V05")] string nombre)
        {
            yield return ArmarVR();
            CarroViejo carro = null;
            yield return CrearVehiculo("Assets/_Proyecto/Vehiculos/Prefabs/" + nombre + ".prefab", new Vector3(3f, 0.3f, 0f), c => carro = c);
            ManejoVR manejo = jugador.GetComponent<ManejoVR>();
            Assert.IsNotNull(manejo);
            List<Renderer> mallas = Mallas(carro);
            Assert.Greater(mallas.Count, 0, "El carro tiene mallas.");
            var sombras = new Dictionary<Renderer, ShadowCastingMode>();
            foreach (Renderer r in mallas)
            {
                sombras[r] = r.shadowCastingMode;
            }
            int colliders = Colliders(carro);

            yield return SubirConB(carro);
            Assert.IsTrue(manejo.Manejando);
            Assert.AreEqual(mallas.Count, manejo.RenderersOcultos, "Se ocultan todas las mallas del carro.");
            foreach (Renderer r in mallas)
            {
                Assert.IsTrue(r.shadowCastingMode == ShadowCastingMode.ShadowsOnly || r.forceRenderingOff,
                    r.name + " no debe dibujarse para la cámara dentro del carro.");
            }
            Assert.AreEqual(colliders, Colliders(carro), "Los colliders no se tocan.");

            yield return Pulsar(controlDer.secondaryButton);
            float fin = Time.time + 1f;
            while (jugadorAPie.Modo != ModoJugador.APie && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "B baja.");
            Assert.AreEqual(0, manejo.RenderersOcultos);
            foreach (Renderer r in mallas)
            {
                Assert.AreEqual(sombras[r], r.shadowCastingMode, r.name + " vuelve a su sombra de antes.");
                Assert.IsFalse(r.forceRenderingOff, r.name + " se vuelve a dibujar.");
            }
        }

        [UnityTest]
        public IEnumerator EnMoto_NoSeOcultaNada()
        {
            yield return ArmarVR();
            CarroViejo moto = null;
            yield return CrearVehiculo(RutaMoto, new Vector3(3f, 0.3f, 0f), c => moto = c);
            yield return SubirConB(moto);
            ManejoVR manejo = jugador.GetComponent<ManejoVR>();
            Assert.IsTrue(manejo.Manejando);
            Assert.AreEqual(0, manejo.RenderersOcultos, "La moto se ve como está.");
        }

        private IEnumerator CrearVehiculo(string ruta, Vector3 posicion, System.Action<CarroViejo> listo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta el prefab " + ruta);
            GameObject objeto = Object.Instantiate(prefab, posicion, Quaternion.identity);
            extras.Add(objeto);
            yield return Segundos(1f);
            CarroViejo carro = objeto.GetComponentInChildren<CarroViejo>();
            Assert.IsNotNull(carro, "El vehículo es un CarroViejo.");
            listo(carro);
        }

        // Junto a la puerta del conductor, mirando al vehículo; B sube (como en ManejoVRPruebas).
        private IEnumerator SubirConB(CarroViejo vehiculo)
        {
            jugadorAPie.Colocar(new Pose(vehiculo.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            float fin = Time.time + 1.5f;
            while (!ReferenceEquals(interaccion.MontableCandidato, vehiculo) && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreSame(vehiculo, interaccion.MontableCandidato, "El vehículo es el candidato de B.");
            yield return Pulsar(controlDer.secondaryButton);
            fin = Time.time + 1f;
            while (jugadorAPie.Modo != ModoJugador.Carro && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "En VR B sube al vehículo.");
            yield return Segundos(0.6f);
        }

        private List<Renderer> Mallas(CarroViejo carro)
        {
            var lista = new List<Renderer>();
            foreach (Renderer r in carro.transform.GetComponentsInChildren<Renderer>())
            {
                if ((r is MeshRenderer || r is SkinnedMeshRenderer) && !r.transform.IsChildOf(jugador.transform) && r.GetComponentInParent<IgnorarOcultarVR>() == null)
                {
                    lista.Add(r);
                }
            }
            return lista;
        }

        private static int Colliders(CarroViejo carro)
        {
            int n = 0;
            foreach (Collider c in carro.transform.GetComponentsInChildren<Collider>())
            {
                if (c.enabled)
                {
                    n++;
                }
            }
            return n;
        }
    }
}
