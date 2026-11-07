using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-635: la ciudad trae nubes y estrellas con textura que cambian con la hora, ambiente por gradiente y sol con sombras.</summary>
    public class CieloTexturaT635Pruebas
    {
        private const float SegundosFisica = 0.3f;
        private const float AlturaOjos = 1.7f;

        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private CicloDiaNoche ciclo;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.RelojPausado().AvanzarHasta(hora);
            ciclo = UnityEngine.Object.FindFirstObjectByType<CicloDiaNoche>();
            Assert.IsNotNull(ciclo, "La ciudad trae CicloDiaNoche");
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosFisica);
        }

        private void MirarAlCielo(float arriba)
        {
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.Bar);
            Vector3 desde = EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada - haciaPuerta * 6f + Vector3.up * AlturaOjos;
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(haciaPuerta + Vector3.up * arriba, Vector3.up)));
        }

        [UnityTest]
        public IEnumerator Cielo_DeNoche_TraeEstrellasYNubesConBrilloDeCiudad()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Assert.IsNotNull(ciclo.CieloTextura, "Se crearon las capas del cielo");
            Assert.IsTrue(ciclo.CieloTextura.Activo);
            Assert.Greater(ciclo.CieloTextura.ColorEstrellas.a, 0.1f, "De noche se ven las estrellas (pocas: T-880, cielo liso)");
            Color nubes = ciclo.CieloTextura.ColorNubes;
            Assert.Greater(nubes.r, nubes.g, "Nubes con el brillo de la ciudad");
            Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode, "Ambiente por gradiente");
            MirarAlCielo(0.9f);
            yield return EscenaLugares.Esperar(SegundosFisica);
            if (Environment.GetEnvironmentVariable("CAPTURAR_T635") == "1")
            {
                CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-635-cielo-noche.png");
            }
        }

        [UnityTest]
        public IEnumerator Cielo_AMediodia_SinEstrellasConNubesBlancasYSolConSombras()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.AreEqual(0f, ciclo.CieloTextura.ColorEstrellas.a, 0.01f, "De día no hay estrellas");
            Assert.Greater(ciclo.CieloTextura.ColorNubes.b, 0.9f, "Nubes blancas");
            Light sol = GameObject.Find("LuzNoche").GetComponent<Light>();
            Assert.AreNotEqual(LightShadows.None, sol.shadows, "El sol proyecta sombras");
            Assert.Less(sol.color.b, sol.color.r, "Luz cálida");
            Assert.Less(RenderSettings.ambientGroundColor.grayscale, RenderSettings.ambientEquatorColor.grayscale, "Ambiente no plano");
            MirarAlCielo(0.5f);
            yield return EscenaLugares.Esperar(SegundosFisica);
            if (Environment.GetEnvironmentVariable("CAPTURAR_T635") == "1")
            {
                CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-635-cielo-dia.png");
            }
        }
    }
}
