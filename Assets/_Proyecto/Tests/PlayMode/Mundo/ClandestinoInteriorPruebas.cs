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
    /// <summary>Interior del clandestino (T-089): se entra de noche sin cargas y se llega a la puerta de la fosa en menos de 20 s, sin huecos y con luces por presencia.</summary>
    public class ClandestinoInteriorPruebas : InputTestFixture
    {
        private const float SegundosMaximosAlBorde = 20f;
        private const float MetrosAlBorde = 1.5f;
        private const float AlcanceRayos = 30f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;
        private const int MaximoLuces = 12;
        private const float DistanciaLejos = 40f;
        private const float AlturaApoyo = 0.05f;
        private const float AlturaOjos = 1.7f;
        private const float SegundosFisica = 0.3f;

        private Keyboard teclado;
        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;
        private Transform borde;

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
                if (candidata.GetComponentInParent<LugarMundo>().Id == IdLugar.Clandestino)
                {
                    zona = candidata;
                }
            }
            Assert.IsNotNull(zona, "Ciudad debe traer la ZonaInterior del clandestino");
            borde = zona.transform.parent.Find("BordeFosa");
            Assert.IsNotNull(borde, "El interior tiene BordeFosa");
        }

        private float DistanciaPlana(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator Clandestino_DeLaBanquetaALaPuertaDeLaFosa_EnMenosDe20SegundosSinCargas()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Clandestino, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount);
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "El clandestino abre de noche");

            Press(teclado.wKey);
            float inicio = Time.time;
            while (Time.time - inicio < SegundosMaximosAlBorde && DistanciaPlana(camara.transform.position, borde.position) > MetrosAlBorde)
            {
                yield return null;
            }
            Release(teclado.wKey);
            float segundos = Time.time - inicio;
            Assert.LessOrEqual(DistanciaPlana(camara.transform.position, borde.position), MetrosAlBorde, "Llega a la puerta de la fosa (usó " + segundos + " s)");
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount, "No hay escenas extra");
            yield return null;
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Clandestino).JugadorDentro);
        }

        [UnityTest]
        public IEnumerator Clandestino_PuertaCerrada_36RayosDesdeElCentroChocanConUnMuro()
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
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " choca con algo que no es del clandestino: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Clandestino_Luces_PocasYApagadasConLaCamaraLejos()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Clandestino, out camaraObjeto);
            Assert.LessOrEqual(zona.Luces.Length, MaximoLuces);
            Assert.Greater(zona.Luces.Length, 0);
            Vector3 lejos = EscenaLugares.Lugar(IdLugar.Clandestino).PosicionEntrada + EscenaLugares.HaciaLaPuerta(IdLugar.Clandestino) * -DistanciaLejos;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Con la cámara lejos '" + luz.name + "' está apagada");
            }
            Vector3 centro = zona.GetComponent<BoxCollider>().bounds.center;
            camara.Colocar(new Pose(new Vector3(centro.x, zona.GetComponent<BoxCollider>().bounds.min.y + AlturaApoyo, centro.z), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Dentro '" + luz.name + "' está prendida");
            }
        }

        [UnityTest]
        public IEnumerator Clandestino_Capturas()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Clandestino, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Vector3 haciaAdentro = EscenaLugares.HaciaLaPuerta(IdLugar.Clandestino);
            Bounds limites = zona.GetComponent<BoxCollider>().bounds;
            Vector3 desdeEntrada = new Vector3(borde.position.x, limites.min.y + AlturaOjos, borde.position.z) - haciaAdentro * 3f;
            Vector3 fosa = new Vector3(borde.position.x, limites.min.y + 1.5f, borde.position.z) + haciaAdentro * 6f;
            camara.Colocar(new Pose(desdeEntrada, Quaternion.LookRotation(fosa - desdeEntrada, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-089-1.png");

            Vector3 esquina = borde.position + haciaAdentro * 5f;
            camara.Colocar(new Pose(new Vector3(esquina.x, limites.min.y + AlturaOjos, esquina.z), Quaternion.LookRotation(-haciaAdentro + Vector3.down * 0.1f, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-089-2.png");
        }
    }
}
