using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1018: las prótesis de ataque que están en el inventario se ven en el cuerpo del jugador: el brazo láser pegado al antebrazo derecho y la hombrera de misiles al hombro
    /// izquierdo, a tamaño humano, sin tapar la cámara; al comprarlas aparecen sin reiniciar y sin ellas no hay modelo activo. El rayo sale de la punta del brazo.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/t1018/.
    /// </summary>
    public class ProtesisEnElCuerpoT1018Pruebas : BaseJugadorAPie
    {
        private const float TamanoMinimo = 0.15f;
        private const float TamanoMaximo = 1.3f; // T-1311: la hombrera nueva va pesada a los huesos y su caja abarca más que la pieza
        private const float DistanciaAlHueso = 0.6f;
        private const float DistanciaMinimaCamara = 0.25f;
        private const float DistanciaPunta = 0.03f;
        private const float DistanciaCuerpo = 2.6f;
        private const float DistanciaDetalle = 0.9f;

        private ProtesisVisiblesAPie visibles;
        private HabilidadesProtesis habilidades;
        private Animator animador;

        private static void Dar(HabilidadProtesis h)
        {
            DatosProtesisAtaque p = CatalogoProtesisAtaque.Actual.De(h);
            Assert.IsNotNull(p, "Hay prótesis de " + h);
            Registro<ServicioInventario>.Actual.Agregar(p, 1);
        }

        private static void Quitar(HabilidadProtesis h)
        {
            Assert.IsTrue(Registro<ServicioInventario>.Actual.Quitar(CatalogoProtesisAtaque.Actual.De(h), 1));
        }

        private IEnumerator Preparar()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            habilidades = jugador.GetComponent<HabilidadesProtesis>();
            visibles = jugador.GetComponent<ProtesisVisiblesAPie>();
            Assert.IsNotNull(visibles, "HabilidadesProtesis agrega ProtesisVisiblesAPie");
            animador = jugador.GetComponentInChildren<Animator>();
            Assert.IsTrue(animador != null && animador.isHuman);
        }

        private static Bounds Limites(GameObject g)
        {
            Renderer[] r = g.GetComponentsInChildren<Renderer>(true);
            Bounds b = r[0].bounds;
            foreach (Renderer x in r.Skip(1)) { b.Encapsulate(x.bounds); }
            return b;
        }

        private void VerificarModelo(GameObject modelo, HumanBodyBones hueso)
        {
            Assert.IsNotNull(modelo, "El modelo está activo");
            Assert.IsTrue(modelo.activeInHierarchy);
            // T-1311: la pieza es una malla pesada a los huesos de PJ01 (cuelga del cuerpo, no de un hueso).
            Assert.IsTrue(modelo.transform.IsChildOf(animador.transform) || modelo.transform.parent == animador.GetBoneTransform(hueso), "Pegado al cuerpo");
            Bounds b = Limites(modelo);
            float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            Assert.That(mayor, Is.InRange(TamanoMinimo, TamanoMaximo), "Tamaño humano (m): " + mayor);
            Assert.Less(Vector3.Distance(b.center, animador.GetBoneTransform(hueso).position), DistanciaAlHueso, "Cerca de su hueso");
            Transform camara = controlador.Camara;
            Assert.IsFalse(b.Contains(camara.position), "No tapa la cámara");
            Assert.Greater(b.SqrDistance(camara.position), DistanciaMinimaCamara * DistanciaMinimaCamara, "Lejos de la cámara");
        }

        [UnityTest]
        public IEnumerator SinProtesis_NoHayModeloActivo()
        {
            yield return Preparar();
            yield return Esperar(0.3f);
            Assert.IsNull(visibles.ModeloLaser);
            Assert.IsNull(visibles.ModeloMisiles);
            Assert.IsEmpty(jugador.GetComponentsInChildren<Transform>(false).Where(t => t.name.StartsWith("IMP0")), "Ningún modelo de prótesis activo");
        }

        [UnityTest]
        public IEnumerator BrazoLaser_ConLaProtesis_SeVeEnElAntebrazoDerecho_YDesapareceSiSeQuita()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            VerificarModelo(visibles.ModeloLaser, HumanBodyBones.RightLowerArm);
            Assert.IsNull(visibles.ModeloMisiles, "La hombrera no está");
            Quitar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            Assert.IsNull(visibles.ModeloLaser, "Sin la prótesis ya no se ve");
        }

        [UnityTest]
        public IEnumerator HombreraDeMisiles_ConLaProtesis_SeVeEnElHombroIzquierdo()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Misiles);
            yield return Esperar(0.3f);
            VerificarModelo(visibles.ModeloMisiles, HumanBodyBones.Chest);
            Assert.IsNull(visibles.ModeloLaser, "El brazo no está");
        }

        [UnityTest]
        public IEnumerator BrazoLaser_SigueLaAnimacion_ElModeloSeMueveConElHueso()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            Transform hueso = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Vector3 relativo = hueso.InverseTransformPoint(visibles.PuntaLaser.position);
            Press(teclado.wKey);
            yield return Esperar(0.8f);
            Release(teclado.wKey);
            Assert.AreEqual(0f, Vector3.Distance(relativo, hueso.InverseTransformPoint(visibles.PuntaLaser.position)), 0.001f, "Sigue al hueso al caminar");
        }

        [UnityTest]
        public IEnumerator Laser_ElRayoSaleDeLaPuntaDelBrazo()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            Assert.IsNotNull(visibles.PuntaLaser, "El brazo tiene punta");
            Press(teclado.digit4Key);
            yield return null;
            yield return null;
            Release(teclado.digit4Key);
            yield return Esperar(0.3f);
            LineRenderer rayo = GameObject.Find("LaserBrazo").GetComponent<LineRenderer>();
            Assert.IsTrue(rayo.enabled, "Se ve el rayo");
            Assert.Less(Vector3.Distance(rayo.GetPosition(0), visibles.PuntaLaser.position), DistanciaPunta + 0.1f, "El rayo nace en la punta del brazo");
        }

        [UnityTest]
        public IEnumerator Capturas_CuerpoEnteroYDetalles()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            Dar(HabilidadProtesis.Misiles);
            yield return Esperar(0.5f);
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1")
            {
                Assert.IsNotNull(visibles.ModeloLaser);
                Assert.IsNotNull(visibles.ModeloMisiles);
                yield break;
            }
            Transform cuerpo = animador.transform;
            Vector3 centro = cuerpo.position + Vector3.up * 1f;
            yield return Foto("cuerpo-frente", centro + cuerpo.forward * DistanciaCuerpo, centro);
            yield return Foto("cuerpo-espalda", centro - cuerpo.forward * DistanciaCuerpo, centro);
            Vector3 brazo = visibles.ModeloLaser.transform.position;
            yield return Foto("brazo-laser-cerca", brazo + cuerpo.right * DistanciaDetalle + cuerpo.forward * DistanciaDetalle * 0.4f, brazo);
            Vector3 hombro = visibles.ModeloMisiles.transform.position;
            yield return Foto("hombrera-misiles-cerca", hombro - cuerpo.right * DistanciaDetalle + cuerpo.forward * DistanciaDetalle * 0.5f, hombro);
            yield return Foto("hombrera-misiles-arriba", hombro + Vector3.up * DistanciaDetalle + cuerpo.forward * 0.05f, hombro);
            yield return Foto("hombrera-misiles-espalda",hombro - cuerpo.right * DistanciaDetalle - cuerpo.forward * DistanciaDetalle * 0.7f, hombro);
        }

        [UnityTest]
        public IEnumerator Capturas_ModelosSueltos()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1")
            {
                yield break;
            }
            yield return Preparar();
            foreach (HabilidadProtesis h in new[] { HabilidadProtesis.Laser, HabilidadProtesis.Misiles })
            {
                GameObject modelo = UnityEngine.Object.Instantiate(CatalogoProtesisAtaque.Actual.De(h).Modelo, new Vector3(0f, 30f, 0f), Quaternion.identity);
                Bounds b = Limites(modelo);
                // El prefab trae la pose del hueso; se muestra el .fbx en su orientación propia.
                Transform interior = modelo.transform.Find("Modelo");
                interior.localRotation = Quaternion.identity;
                interior.localPosition = Vector3.zero;
                b = Limites(modelo);
                foreach ((string vista, Vector3 dir) in new[] { ("frente", Vector3.back), ("lado", Vector3.right), ("arriba", Vector3.up + Vector3.back * 0.01f) })
                {
                    yield return Foto("modelo-" + modelo.name + "-" + vista, b.center + dir.normalized * b.size.magnitude * 1.6f, b.center);
                }
                UnityEngine.Object.Destroy(modelo);
            }
        }

        private IEnumerator Foto(string nombre, Vector3 desde, Vector3 hacia)
        {
            var cam = new GameObject("CamProtesis").AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/t1018/" + nombre + ".png");
            UnityEngine.Object.Destroy(cam.gameObject);
        }
    }
}
