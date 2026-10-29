using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Exteriores del casino y el mercado negro (T-094; los dos abren desde T-303 y T-320): ninguno tapa la Entrada ni invade la calle.
    /// Que abran y cierren por horario se prueba en LugaresPruebas, PuertasPruebas y MercadoNegroInteriorPruebas.
    /// </summary>
    public class CerradosPruebas
    {
        private const float SalienteMaximo = 1.3f;       // m desde la fachada (VestirCerrados)
        private const float DistanciaEntrada = 1.5f;
        private const float RadioCuerpo = 0.4f;
        private const float AlturaCuerpo = 1.8f;
        private const float Holgura = 0.05f;

        [TearDown]
        public void TearDown()
        {
            EscenaLugares.Descargar();
        }

        private static readonly IdLugar[] Decorados = { IdLugar.Casino, IdLugar.MercadoNegro };

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
        public IEnumerator Cerrados_NoTapanLaEntradaNiInvadenLaCalle()
        {
            yield return EscenaLugares.CargarCiudad();
            foreach (IdLugar id in Decorados)
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
    }
}
