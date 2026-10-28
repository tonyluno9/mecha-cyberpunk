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
    /// <summary>Marcos y neón de las entradas (T-311): están en los cuatro destinos, no tapan la Entrada ni la puerta, no invaden la calle y cuestan poco.</summary>
    public class EntradasNeonPruebas
    {
        private const string NombreGrupo = "EntradaNeon";      // VestirEntradas
        private const float SalienteMaximo = 0.9f;             // m desde la fachada (VestirEntradas)
        private const float DistanciaEntrada = 1.5f;
        private const float RadioCuerpo = 0.4f;
        private const float AlturaCuerpo = 1.8f;
        private const float AlturaOjos = 1.7f;
        private const float Holgura = 0.05f;
        private const float SegundosFisica = 0.3f;
        private const float InclinacionGrados = 8f;
        private const float DistanciaCaptura = 6f;
        private const float DistanciaCapturaCallejon = 3.2f;    // el callejón del mercado mide unos 4.5 m de ancho
        private const int LucesMaximasTotal = 3;
        private const int MaterialesMaximos = 6;

        private static readonly IdLugar[] Destinos = { IdLugar.Casa, IdLugar.Clandestino, IdLugar.Casino, IdLugar.MercadoNegro };

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

        private static Transform Grupo(IdLugar id)
        {
            foreach (LugarMundo lugar in Object.FindObjectsByType<LugarMundo>(FindObjectsSortMode.None))
            {
                if (lugar.Id == id)
                {
                    Transform grupo = lugar.transform.Find(NombreGrupo);
                    Assert.IsNotNull(grupo, id + " lleva el grupo " + NombreGrupo);
                    return grupo;
                }
            }
            Assert.Fail("No hay lugar " + id);
            return null;
        }

        [UnityTest]
        public IEnumerator Entradas_NoTapanLaEntradaNiLaPuertaNiInvadenLaCalle()
        {
            yield return EscenaLugares.CargarCiudad();
            foreach (IdLugar id in Destinos)
            {
                Transform grupo = Grupo(id);
                Transform lugar = grupo.parent;
                Transform entrada = lugar.Find("Entrada");
                Vector3 pie = entrada.position;
                Assert.IsFalse(Physics.CheckCapsule(pie + Vector3.up * (RadioCuerpo + Holgura), pie + Vector3.up * (AlturaCuerpo - RadioCuerpo), RadioCuerpo, ~0, QueryTriggerInteraction.Ignore),
                    id + ": hay algo sobre la Entrada");
                Assert.AreEqual(0, grupo.GetComponentsInChildren<Collider>().Length, id + ": la decoración no lleva collider");
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
                    Assert.LessOrEqual(zMax, zFachada + SalienteMaximo + Holgura, id + ": '" + r.name + "' se mete demasiado a la calle");
                }
                Assert.GreaterOrEqual(piezas, 8, id + " tiene su marco y su neón");
            }
        }

        [UnityTest]
        public IEnumerator Entradas_CostoDeLucesYMaterialesRazonable()
        {
            yield return EscenaLugares.CargarCiudad();
            int luces = 0;
            var materiales = new System.Collections.Generic.HashSet<Material>();
            foreach (IdLugar id in Destinos)
            {
                Transform grupo = Grupo(id);
                foreach (Light luz in grupo.GetComponentsInChildren<Light>())
                {
                    luces++;
                    Assert.AreEqual(LightShadows.None, luz.shadows, id + ": las luces de entrada no proyectan sombras");
                }
                foreach (Renderer r in grupo.GetComponentsInChildren<Renderer>())
                {
                    materiales.Add(r.sharedMaterial);
                }
            }
            Assert.LessOrEqual(luces, LucesMaximasTotal, "pocas luces nuevas en total");
            Assert.LessOrEqual(materiales.Count, MaterialesMaximos, "pocos materiales distintos (se comparten entre destinos)");
        }

        [UnityTest]
        public IEnumerator Entradas_CasinoAbreDeNocheYMercadoSigueCerrado()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casino).EstaAbierto, "el casino abre de noche (T-303)");
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "el mercado negro sigue cerrado");
        }

        [UnityTest]
        public IEnumerator Entradas_Capturas_DiaYNoche()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = EscenaLugares.RelojPausado();
            foreach (IdLugar id in Destinos)
            {
                CamaraLibre camara = EscenaLugares.ColocarFrenteA(id, out camaraObjeto);
                Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(id);
                Vector3 desde = EscenaLugares.Lugar(id).PosicionEntrada - haciaPuerta * (id == IdLugar.MercadoNegro ? DistanciaCapturaCallejon : DistanciaCaptura);
                desde.y = EscenaLugares.Lugar(id).PosicionEntrada.y + AlturaOjos;
                Quaternion mira = Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(-InclinacionGrados, 0f, 0f);
                foreach (float hora in new[] { EscenaLugares.HoraMediodia, EscenaLugares.HoraNoche })
                {
                    reloj.AvanzarHasta(hora);
                    camara.Colocar(new Pose(desde, mira));
                    yield return EscenaLugares.Calentar();
                    yield return EscenaLugares.Esperar(SegundosFisica);
                    CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-311-" + id + (hora == EscenaLugares.HoraMediodia ? "-dia" : "-noche") + ".png");
                }
                Object.DestroyImmediate(camaraObjeto);
                camaraObjeto = null;
            }
        }
    }
}
