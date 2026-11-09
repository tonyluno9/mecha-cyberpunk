using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-942: las cuatro armas del banco de Tino se ven en 3D con sus piezas (las de una sola malla se parten en Cargador, Canon y resto).
    /// Cada pieza elegida se separa (sin cruzar el pedestal) y su centro se mueve; el escaneo corre en las tres. Con CAPTURAR_UI=1 guarda
    /// docs/capturas/ui-banco-escopeta.png, ui-banco-fusil.png y ui-banco-francotirador.png con la pieza separada.
    /// </summary>
    public class BancoTodasArmasT942Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        private const float PedestalBajo = 0.42f;
        private static readonly string[] Nombres = { "escopeta", "fusil", "francotirador" };

        private static IReadOnlyDictionary<PiezaArma, EstadoPieza> Estados()
        {
            return new Dictionary<PiezaArma, EstadoPieza>
            {
                { PiezaArma.Cargador, EstadoPieza.Disponible },
                { PiezaArma.Canon, EstadoPieza.Disponible },
                { PiezaArma.Mira, EstadoPieza.Disponible }
            };
        }

        [UnityTest]
        public IEnumerator Banco_ArmaConUnaSolaMalla_SePartePorPiezasYCadaUnaSeSepara([ValueSource(nameof(Nombres))] string nombre)
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            EntradaArmaMenu entrada = config.Armas[Array.IndexOf(Nombres, nombre) + 1];
            Assert.IsNotNull(entrada.Modelo, nombre + " tiene su modelo 3D");
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.Mostrar(entrada, config);
            yield return null;
            Assert.IsTrue(vista.ConModelo);
            foreach (PiezaArma pieza in new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira })
            {
                Assert.IsTrue(vista.TienePieza(pieza), nombre + " tiene " + pieza);
            }
            // El cargador cuelga por debajo del cañón y el cañón queda hacia un extremo (más lejos del centro que el cargador).
            Assert.IsTrue(vista.CentroPieza(PiezaArma.Cargador, out Vector3 cargador));
            Assert.IsTrue(vista.CentroPieza(PiezaArma.Canon, out Vector3 canon));
            Assert.Less(cargador.y, canon.y, "El cargador queda más abajo que el cañón");

            foreach (PiezaArma pieza in new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira })
            {
                vista.CentroPieza(pieza, out Vector3 antes);
                vista.ElegirPieza(pieza, Estados());
                yield return new WaitForSecondsRealtime(0.9f);
                Assert.Greater(vista.Separacion(pieza), 0.9f, nombre + ": " + pieza + " se separa");
                vista.CentroPieza(pieza, out Vector3 despues);
                Assert.Greater((despues - antes).magnitude, 0.02f, nombre + ": " + pieza + " se mueve");
                Assert.Greater(despues.y, config.PosicionSet.y - PedestalBajo, nombre + ": " + pieza + " no cruza el pedestal");
                if (pieza == PiezaArma.Cargador && Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1")
                {
                    Guardar(vista, "docs/capturas/ui-banco-" + nombre + ".png");
                }
            }
            vista.ElegirPieza(PiezaArma.Cargador, Estados());
            bool termino = false;
            vista.Escanear(() => termino = true);
            Assert.IsTrue(vista.Escaneando, nombre + ": el escaneo arranca");
            float limite = Time.realtimeSinceStartup + 10f;
            while (!termino && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsTrue(termino, nombre + ": el escaneo termina");
            vista.Ocultar();
            Object.Destroy(go);
        }

        // Guarda lo que ve la cámara del holograma sobre un fondo oscuro.
        private static void Guardar(VistaHologramaArma vista, string ruta)
        {
            RenderTexture rt = vista.Textura;
            RenderTexture.active = rt;
            Texture2D leida = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            leida.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            Color[] pixeles = leida.GetPixels();
            Color fondo = new Color(0.02f, 0.05f, 0.08f, 1f);
            for (int i = 0; i < pixeles.Length; i++)
            {
                Color p = pixeles[i];
                pixeles[i] = new Color(fondo.r + p.r, fondo.g + p.g, fondo.b + p.b, 1f);
            }
            leida.SetPixels(pixeles);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, leida.EncodeToPNG());
            Object.Destroy(leida);
        }
    }
}
