using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-715: los guardias de Nova hacen daño de verdad. En alerta y con el jugador cerca, avisan (luz roja y brazo atrás), y recién al llegar el golpe le bajan la
    /// vida; alejarse durante el aviso lo esquiva; cada tercer golpe es la barrida (más fuerte). Con el jugador real y el guardia real.
    /// </summary>
    public class GuardiaNovaDanioT715Pruebas : BaseJugadorAPie
    {
        private const string RutaGuardia = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab";
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<float> impactos = new List<float>();

        public override void Setup()
        {
            EventosRuido.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.Setup();
            impactos.Clear();
            EventosPelea.ImpactoRecibido += AlImpacto;
        }

        public override void TearDown()
        {
            EventosPelea.ImpactoRecibido -= AlImpacto;
            foreach (GameObject c in creados)
            {
                if (c != null)
                {
                    Object.DestroyImmediate(c);
                }
            }
            creados.Clear();
            EventosRuido.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void AlImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (info.Tipo == TipoDanio.Golpe)
            {
                impactos.Add(info.Cantidad);
            }
        }

        private GuardiaNova CrearGuardia(float distancia)
        {
            GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaGuardia), new Vector3(0f, 0f, distancia), Quaternion.LookRotation(Vector3.back));
            creados.Add(go);
            GuardiaNova nova = go.GetComponent<GuardiaNova>() ?? go.AddComponent<GuardiaNova>();
            nova.ConfigurarGestos(60f, 1.5f);
            return nova;
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 5f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EnAlerta_AvisaConLuzRoja_Y_ConectaDespues_BajandoLaVida()
        {
            yield return Calentar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float maxima = vida.Actual;
            GuardiaNova nova = CrearGuardia(1.8f);
            Assert.IsFalse(nova.AvisandoGolpe);
            EventosRuido.PublicarDisparo(nova.transform.position); // alerta: oyó un disparo
            yield return Hasta(() => nova.AvisandoGolpe, "empieza el aviso del golpe");
            float inicio = Time.time;
            Assert.IsNotNull(nova.LuzAviso, "Hay una luz de aviso");
            Assert.IsTrue(nova.LuzAviso.enabled, "La luz roja se ve durante el aviso");
            Assert.Greater(nova.LuzAviso.color.r, 0.9f);
            Assert.Less(nova.LuzAviso.color.g, 0.3f);
            Assert.AreEqual(0, nova.Impactos, "En el aviso todavía no hay golpe");
            Assert.AreEqual(maxima, vida.Actual, 0.001f, "Ni daño");
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Esperar(0.35f);
                yield return Capturar(nova, "s1c-T-715-aviso.png");
            }
            yield return Hasta(() => nova.Impactos >= 1, "el golpe conecta");
            Assert.GreaterOrEqual(Time.time - inicio, 0.45f, "Hubo un aviso de más de medio segundo antes del daño");
            Assert.IsFalse(nova.LuzAviso.enabled, "La luz se apaga al golpear");
            Assert.Less(vida.Actual, maxima, "El golpe le bajó la vida al jugador");
            Assert.AreEqual(maxima - 12f, vida.Actual, 0.01f, "El puñetazo hace 12");
            Assert.AreEqual(12f, impactos[0], 0.001f);
        }

        [UnityTest]
        public IEnumerator AlejarseDuranteElAviso_EsquivaElGolpe()
        {
            yield return Calentar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float maxima = vida.Actual;
            GuardiaNova nova = CrearGuardia(1.8f);
            EventosRuido.PublicarDisparo(nova.transform.position);
            yield return Hasta(() => nova.AvisandoGolpe, "empieza el aviso");
            jugador.transform.position = new Vector3(0f, 0.05f, -6f); // se aleja antes del impacto
            Physics.SyncTransforms();
            yield return Esperar(1.2f);
            Assert.AreEqual(0, nova.Impactos, "Alejarse a tiempo esquiva el golpe");
            Assert.AreEqual(maxima, vida.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator ElTercerGolpe_EsLaBarrida_ConMasDanio()
        {
            yield return Calentar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            GuardiaNova nova = CrearGuardia(1.8f);
            EventosRuido.PublicarDisparo(nova.transform.position);
            yield return Hasta(() => nova.Impactos >= 3, "tres golpes", 12f);
            Assert.AreEqual(12f, impactos[0], 0.001f);
            Assert.AreEqual(12f, impactos[1], 0.001f);
            Assert.AreEqual(20f, impactos[2], 0.001f, "El tercero es la barrida");
            Assert.Greater(nova.DanioHecho, 40f, "Daño total hecho por el guardia");
            Assert.IsFalse(vida.EstaKO);
        }

        private IEnumerator Capturar(GuardiaNova nova, string archivo)
        {
            GameObject objeto = new GameObject("CamaraCaptura");
            creados.Add(objeto);
            Camera camara = objeto.AddComponent<Camera>();
            camara.farClipPlane = 200f;
            Vector3 centro = nova.transform.position + Vector3.up * 1.1f;
            objeto.transform.position = centro + new Vector3(2.6f, 0.4f, 1.0f);
            objeto.transform.LookAt(centro);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
        }
    }
}
