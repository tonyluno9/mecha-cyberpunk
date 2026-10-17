using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Grafitis (T-095): entre 30 y 60 decales sin collider, pegados a un muro y mirando hacia afuera.</summary>
    public class GrafitisPruebas
    {
        private const int Minimo = 30;
        private const int Maximo = 60;
        private const float DistanciaMuro = 0.05f;     // el decal queda a 0.5 m del muro, pegado a las fachadas que sobresalen
        private const float AlcanceMuro = 0.7f;
        private const float AlturaOjos = 1.7f;
        private const float DistanciaCaptura = 5f;
        private const float SegundosFisica = 0.3f;

        private GameObject camaraObjeto;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        private static List<Transform> Decales()
        {
            GameObject grupo = GameObject.Find("Grafitis");
            Assert.IsNotNull(grupo, "El barrio trae el grupo Grafitis");
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in grupo.transform)
            {
                lista.Add(t);
            }
            return lista;
        }

        [UnityTest]
        public IEnumerator Grafitis_HayEntre30Y60_SinCollider()
        {
            yield return EscenaLugares.CargarCiudad();
            List<Transform> decales = Decales();
            Assert.GreaterOrEqual(decales.Count, Minimo);
            Assert.LessOrEqual(decales.Count, Maximo);
            foreach (Transform d in decales)
            {
                Assert.IsNull(d.GetComponent<Collider>(), d.name + " no debe tener collider");
            }
        }

        [UnityTest]
        public IEnumerator Grafitis_CadaUnoMiraAfueraYTieneUnMuroDetras()
        {
            yield return EscenaLugares.CargarCiudad();
            foreach (Transform d in Decales())
            {
                Vector3 normal = -d.forward;
                Vector3 origen = d.position + normal * DistanciaMuro;
                Assert.IsTrue(Physics.Raycast(origen, -normal, out RaycastHit golpe, AlcanceMuro, ~0, QueryTriggerInteraction.Ignore), d.name + " no tiene muro ni fachada detrás (o mira hacia adentro)");
                Assert.IsFalse(Physics.CheckSphere(origen, 0.05f, ~0, QueryTriggerInteraction.Ignore), d.name + " tiene algo delante, pegado");
            }
        }

        // Hay calle libre entre el punto de la cámara (a DistanciaCaptura del decal, mirando al muro) y el decal.
        private static bool TieneVistaLibre(Transform d)
        {
            Vector3 desde = d.position - d.forward * DistanciaCaptura;
            desde.y = AlturaOjos + 0.2f;
            return !Physics.CheckSphere(desde, 0.5f, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.Linecast(desde, d.position - d.forward * 0.4f, ~0, QueryTriggerInteraction.Ignore);
        }

        [UnityTest]
        public IEnumerator Grafitis_Capturas_ZonaPobreYCallejon()
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.RelojPausado().AvanzarHasta(EscenaLugares.HoraNoche);
            EscenaLugares.ColocarFrenteA(IdLugar.Casa, out camaraObjeto);
            CamaraLibre camara = camaraObjeto.GetComponent<CamaraLibre>();
            List<Transform> decales = Decales();
            Transform calle = decales.Find(d => !d.name.Contains("Callejon") && TieneVistaLibre(d));
            Transform callejon = decales.Find(d => d.name.Contains("Callejon_S"));
            string[] rutas = { "docs/capturas/T-095-1.png", "docs/capturas/T-095-2.png" };
            Transform[] objetivos = { calle, callejon };
            for (int i = 0; i < objetivos.Length; i++)
            {
                Assert.IsNotNull(objetivos[i]);
                Vector3 normal = -objetivos[i].forward;
                Vector3 desde = objetivos[i].position + normal * (i == 0 ? DistanciaCaptura : DistanciaCaptura * 0.6f);
                desde.y = AlturaOjos + 0.2f;
                camara.Colocar(new Pose(desde, Quaternion.LookRotation(objetivos[i].position - desde, Vector3.up)));
                yield return EscenaLugares.Calentar();
                yield return EscenaLugares.Esperar(SegundosFisica);
                CapturaPruebas.Guardar(camara.Camara, rutas[i]);
            }
        }
    }
}
