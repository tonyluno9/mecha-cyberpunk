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
            foreach (PiezaArma pieza in Piezas(entrada))
            {
                Assert.IsTrue(vista.TienePieza(pieza), nombre + " tiene " + pieza);
            }
            Assert.AreEqual(entrada.TieneMira, vista.TienePieza(PiezaArma.Mira), nombre + ": solo las armas con mira tienen mira en el holograma");
            // El cargador cuelga por debajo del cañón y el cañón queda hacia un extremo (más lejos del centro que el cargador).
            Assert.IsTrue(vista.CentroPieza(PiezaArma.Cargador, out Vector3 cargador));
            Assert.IsTrue(vista.CentroPieza(PiezaArma.Canon, out Vector3 canon));
            Assert.Less(cargador.y, canon.y, "El cargador queda más abajo que el cañón");

            foreach (PiezaArma pieza in Piezas(entrada))
            {
                vista.CentroPieza(pieza, out Vector3 antes);
                vista.ElegirPieza(pieza, Estados());
                yield return new WaitForSecondsRealtime(0.9f);
                Assert.Greater(vista.Separacion(pieza), 0.9f, nombre + ": " + pieza + " se separa");
                vista.CentroPieza(pieza, out Vector3 despues);
                Assert.Greater((despues - antes).magnitude, 0.02f, nombre + ": " + pieza + " se mueve");
                Assert.Greater(despues.y, config.PosicionSet.y - PedestalBajo, nombre + ": " + pieza + " no cruza el pedestal");
                if (Environment.GetEnvironmentVariable("CAPTURAR_T970") == "1" && NombreCaptura(nombre, pieza) != null)
                {
                    Guardar(vista, "docs/capturas/t970/" + NombreCaptura(nombre, pieza) + ".png");
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

        // T-971: la pistola trae su Mira real de Blender (4 submallas): se separa hacia arriba y nada se arma con mallas simples.
        [UnityTest]
        public IEnumerator Banco_Pistola_TieneLaMiraRealYSeSeparaHaciaArriba()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            EntradaArmaMenu entrada = config.Armas[0];
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.Mostrar(entrada, config);
            yield return null;
            Assert.IsTrue(vista.TienePieza(PiezaArma.Mira), "La pistola tiene mira con la malla real");
            foreach (string inventado in new[] { "MiraHolografica", "CargadorHolografico", "CanonHolografico" })
            {
                Assert.IsNull(GameObject.Find(inventado), inventado + " ya no se crea");
            }
            vista.CentroPieza(PiezaArma.Mira, out Vector3 antes);
            vista.ElegirPieza(PiezaArma.Mira, Estados());
            yield return new WaitForSecondsRealtime(0.9f);
            vista.CentroPieza(PiezaArma.Mira, out Vector3 despues);
            Assert.Greater(vista.Separacion(PiezaArma.Mira), 0.9f, "La mira se separa");
            Assert.Greater(despues.y - antes.y, 0.02f, "La mira sale hacia arriba");
            int submallas = 0;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Mira")
                {
                    submallas = r.sharedMaterials.Length;
                }
            }
            Assert.GreaterOrEqual(submallas, 4, "La mira es una malla de 4 submallas con el material del holograma en todas");
            if (Environment.GetEnvironmentVariable("CAPTURAR_T971") == "1")
            {
                Guardar(vista, "docs/capturas/t971/banco-pistola-mira.png");
            }
            vista.Ocultar();
            Object.Destroy(go);
        }

        // T-971: la escopeta no lleva mira: el holograma no la arma.
        [UnityTest]
        public IEnumerator Banco_Escopeta_SinMira_NoSeInventa()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.Mostrar(config.Armas[1], config);
            yield return null;
            Assert.IsFalse(vista.TienePieza(PiezaArma.Mira));
            Assert.IsNull(GameObject.Find("MiraHolografica"));
            if (Environment.GetEnvironmentVariable("CAPTURAR_T971") == "1")
            {
                Guardar(vista, "docs/capturas/t971/banco-escopeta-sinmira.png");
            }
            vista.Ocultar();
            Object.Destroy(go);
        }

        // T-970: las piezas que el arma sí tiene (la escopeta no lleva mira).
        private static PiezaArma[] Piezas(EntradaArmaMenu entrada)
        {
            return entrada.TieneMira ? new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira } : new[] { PiezaArma.Cargador, PiezaArma.Canon };
        }

        // Las capturas que pide T-970 (la escopeta llama bomba a su cargador); null si esa pieza no se captura.
        private static string NombreCaptura(string arma, PiezaArma pieza)
        {
            switch (arma + "-" + pieza)
            {
                case "escopeta-Cargador": return "banco-escopeta-bomba";
                case "fusil-Cargador": return "banco-fusil-cargador";
                case "fusil-Mira": return "banco-fusil-mira";
                case "francotirador-Canon": return "banco-francotirador-canon";
                case "francotirador-Mira": return "banco-francotirador-mira";
                default: return null;
            }
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
