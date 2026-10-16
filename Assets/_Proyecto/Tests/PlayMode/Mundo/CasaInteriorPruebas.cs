using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Interior de la casa (T-086): se entra sin cargar escenas, no tiene huecos y el taller cumple el contrato ITallerMecha.</summary>
    public class CasaInteriorPruebas : InputTestFixture
    {
        private const float RadioLibre = 2f;
        private const float AltoLibre = 5f;
        private const float SegundosMaximosAlCentro = 10f;
        private const float MetrosAlCentro = 1f;
        private const float AlcanceRayos = 15f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;
        private const float AlturaApoyo = 0.05f;
        private const float MargenCapsula = 0.1f;
        private const float GradosMaximosHaciaLaSalida = 60f;

        private Keyboard teclado;
        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            EventosLugares.LimpiarSuscriptores();
        }

        public override void TearDown()
        {
            EventosLugares.LimpiarSuscriptores();
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.RelojPausado().AvanzarHasta(EscenaLugares.HoraNoche);
            foreach (ZonaInterior candidata in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                if (candidata.GetComponentInParent<LugarMundo>().Id == IdLugar.Casa)
                {
                    zona = candidata;
                }
            }
            Assert.IsNotNull(zona, "Ciudad debe traer la ZonaInterior de la casa");
        }

        [UnityTest]
        public IEnumerator Casa_TieneUnSoloTallerRegistrado()
        {
            yield return Preparar();
            Assert.AreEqual(1, Registro<ITallerMecha>.Todos.Count, "Hay exactamente un ITallerMecha");
        }

        [UnityTest]
        public IEnumerator Casa_EstacionamientoLibreDe2MetrosDeRadioY5DeAlto()
        {
            yield return Preparar();
            ITallerMecha taller = Registro<ITallerMecha>.Todos[0];
            Pose base0 = taller.Estacionamiento;
            Vector3 abajo = base0.position + Vector3.up * (RadioLibre + MargenCapsula);
            Vector3 arriba = base0.position + Vector3.up * (AltoLibre - RadioLibre);
            Assert.IsFalse(Physics.CheckCapsule(abajo, arriba, RadioLibre, ~0, QueryTriggerInteraction.Ignore), "El cilindro de 2 m × 5 m sobre Estacionamiento está libre");
            Assert.IsTrue(Physics.Raycast(base0.position + Vector3.up * 0.5f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore), "Hay suelo bajo Estacionamiento");
            Assert.IsTrue(Physics.Raycast(base0.position + Vector3.up * 0.5f, Vector3.up, out RaycastHit techo, 10f, ~0, QueryTriggerInteraction.Ignore));
            Assert.Greater(techo.distance + 0.5f, AltoLibre, "El techo está a más de 5 m");
        }

        [UnityTest]
        public IEnumerator Casa_EstacionamientoMiraHaciaLaSalida()
        {
            yield return Preparar();
            ITallerMecha taller = Registro<ITallerMecha>.Todos[0];
            Vector3 haciaLaSalida = -EscenaLugares.HaciaLaPuerta(IdLugar.Casa);
            Vector3 adelante = taller.Estacionamiento.rotation * Vector3.forward;
            Assert.Less(Vector3.Angle(adelante, haciaLaSalida), GradosMaximosHaciaLaSalida, "+Z del Estacionamiento mira hacia la puerta");
            Assert.AreEqual(0f, adelante.y, 0.001f, "Mira en horizontal");
            Assert.Less(Vector3.Distance(taller.PuntoInteraccion, taller.Estacionamiento.position), 3.5f, "El banco está junto al mecha");
        }

        [UnityTest]
        public IEnumerator Casa_PuertaCerrada_36RayosDesdeElCentroChocanConUnMuro()
        {
            yield return Preparar();
            BoxCollider caja = zona.GetComponent<BoxCollider>();
            Vector3 centro = caja.bounds.center;
            float suelo = caja.bounds.min.y;
            foreach (float altura in new[] { 1f, 3f })
            {
                for (int i = 0; i < CantidadRayos; i++)
                {
                    Vector3 direccion = Quaternion.Euler(0f, i * GradosPorVuelta / CantidadRayos, 0f) * Vector3.forward;
                    Vector3 origen = new Vector3(centro.x, suelo + altura, centro.z);
                    bool choco = Physics.Raycast(origen, direccion, out RaycastHit golpe, AlcanceRayos, ~0, QueryTriggerInteraction.Ignore);
                    Assert.IsTrue(choco, "El rayo " + i + " a " + altura + " m de alto no choca con nada: hay un hueco");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " choca con algo que no es de la casa: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Casa_DeLaBanquetaAlCentro_SinCargarEscenas()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Casa, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount);
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "La casa abre de noche");

            Vector3 centro = zona.GetComponent<BoxCollider>().bounds.center;
            Press(teclado.wKey);
            float inicio = Time.time;
            while (Time.time - inicio < SegundosMaximosAlCentro)
            {
                Vector3 d = camara.transform.position - centro;
                d.y = 0f;
                if (d.magnitude < MetrosAlCentro)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            Vector3 resto = camara.transform.position - centro;
            resto.y = 0f;
            Assert.Less(resto.magnitude, MetrosAlCentro, "Llega al centro de la casa en menos de 10 s");
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount, "No hay escenas extra");
            yield return null;
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casa).JugadorDentro);
        }
    }
}
