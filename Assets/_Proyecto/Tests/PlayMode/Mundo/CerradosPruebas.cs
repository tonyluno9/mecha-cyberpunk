using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Exteriores cerrados del casino y el mercado negro (T-094): siguen cerrados a cualquier hora, no tapan la Entrada ni invaden la calle.</summary>
    public class CerradosPruebas
    {
        private const float SalienteMaximo = 1.3f;       // m desde la fachada (VestirCerrados)
        private const float DistanciaEntrada = 1.5f;
        private const float RadioCuerpo = 0.4f;
        private const float AlturaCuerpo = 1.8f;
        private const float AlturaOjos = 1.7f;
        private const float Holgura = 0.05f;
        private const float SegundosFisica = 0.3f;
        private const float InclinacionGrados = 8f;

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

        private static readonly IdLugar[] Cerrados = { IdLugar.Casino, IdLugar.MercadoNegro };

        private static Transform GrupoCerrado(IdLugar id)
        {
            foreach (LugarMundo lugar in Object.FindObjectsByType<LugarMundo>(FindObjectsSortMode.None))
            {
                if (lugar.Id == id)
                {
                    Transform grupo = lugar.transform.Find("Cerrado");
                    Assert.IsNotNull(grupo, id + " lleva el grupo Cerrado");
                    return grupo;
                }
            }
            Assert.Fail("No hay lugar " + id);
            return null;
        }

        [UnityTest]
        public IEnumerator Cerrados_SiguenCerradosAMediodiaYDeNoche()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = EscenaLugares.RelojPausado();
            foreach (float hora in new[] { EscenaLugares.HoraMediodia, EscenaLugares.HoraNoche })
            {
                reloj.AvanzarHasta(hora);
                yield return null;
                foreach (IdLugar id in Cerrados)
                {
                    Assert.IsFalse(EscenaLugares.Lugar(id).EstaAbierto, id + " sigue cerrado a las " + hora);
                }
            }
        }

        [UnityTest]
        public IEnumerator Cerrados_NoTapanLaEntradaNiInvadenLaCalle()
        {
            yield return EscenaLugares.CargarCiudad();
            foreach (IdLugar id in Cerrados)
            {
                Transform grupo = GrupoCerrado(id);
                Transform lugar = grupo.parent;
                Transform entrada = lugar.Find("Entrada");
                Vector3 pie = entrada.position;
                Assert.IsFalse(Physics.CheckCapsule(pie + Vector3.up * (RadioCuerpo + Holgura), pie + Vector3.up * (AlturaCuerpo - RadioCuerpo), RadioCuerpo, ~0, QueryTriggerInteraction.Ignore),
                    id + ": hay algo sobre la Entrada");
                float zFachada = entrada.localPosition.z - DistanciaEntrada;
                int piezas = 0;
                foreach (Renderer r in grupo.GetComponentsInChildren<Renderer>())
                {
                    piezas++;
                    Bounds b = r.bounds;
                    float zMax = float.MinValue;
                    foreach (Vector3 esquina in new[] { b.min, b.max, new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.max.y, b.min.z) })
                    {
                        zMax = Mathf.Max(zMax, lugar.InverseTransformPoint(esquina).z);
                    }
                    Assert.LessOrEqual(zMax, zFachada + SalienteMaximo + 0.7f, id + ": '" + r.name + "' se mete demasiado a la calle");
                }
                Assert.Greater(piezas, 8, id + " tiene su decoración");
            }
        }

        [UnityTest]
        public IEnumerator Cerrados_Capturas_DiaYNoche()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = EscenaLugares.RelojPausado();
            int numero = 1;
            foreach (IdLugar id in Cerrados)
            {
                CamaraLibre camara = EscenaLugares.ColocarFrenteA(id, out camaraObjeto);
                Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(id);
                float atras = id == IdLugar.Casino ? 9f : 3.2f;   // el callejón del mercado mide unos 4.5 m de ancho
                Vector3 desde = EscenaLugares.Lugar(id).PosicionEntrada - haciaPuerta * atras;
                desde.y = EscenaLugares.Lugar(id).PosicionEntrada.y + AlturaOjos;
                Quaternion mira = Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(-InclinacionGrados, 0f, 0f);
                foreach (float hora in new[] { EscenaLugares.HoraMediodia, EscenaLugares.HoraNoche })
                {
                    reloj.AvanzarHasta(hora);
                    camara.Colocar(new Pose(desde, mira));
                    yield return EscenaLugares.Calentar();
                    yield return EscenaLugares.Esperar(SegundosFisica);
                    CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-094-" + numero++ + ".png");
                }
                Object.DestroyImmediate(camaraObjeto);
                camaraObjeto = null;
            }
        }
    }
}
