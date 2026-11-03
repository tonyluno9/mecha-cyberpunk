using System.Collections;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-544: la pantalla LED cambia de anuncio con el tiempo y parpadea sin apagarse.</summary>
    public class PantallaLedPruebas
    {
        [UnityTest]
        public IEnumerator Pantalla_CambiaDeAnuncio_YParpadeaSinApagarse()
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.SetActive(false);
            Camera cam = new GameObject("CamaraPrueba").AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0f, -3f);
            try
            {
                PantallaLed p = q.AddComponent<PantallaLed>();
                Texture2D[] anuncios = { new Texture2D(4, 4), new Texture2D(4, 4), new Texture2D(4, 4) };
                p.Configurar(anuncios, 0f);
                SerializedObject so = new SerializedObject(p);
                so.FindProperty("segundosMinimos").floatValue = 0.5f;
                so.FindProperty("segundosMaximos").floatValue = 0.6f;
                so.ApplyModifiedPropertiesWithoutUndo();
                q.SetActive(true);
                yield return null;
                int primero = p.Indice;
                MaterialPropertyBlock b = new MaterialPropertyBlock();
                float minimo = float.MaxValue;
                float fin = Time.time + 1.5f;
                while (Time.time < fin)
                {
                    yield return null;
                    q.GetComponent<Renderer>().GetPropertyBlock(b);
                    minimo = Mathf.Min(minimo, b.GetColor("_BaseColor").r);
                }
                Assert.AreNotEqual(primero, p.Indice, "Cambió de anuncio");
                Assert.Greater(minimo, 1f, "Parpadea pero no se apaga (brillo 1.6, parpadeo 12 %)");
            }
            finally
            {
                Object.Destroy(q);
                Object.Destroy(cam.gameObject);
            }
        }
    }
}
