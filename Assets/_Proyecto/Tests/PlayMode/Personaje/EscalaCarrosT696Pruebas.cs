using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-696: escala de los carros contra el jugador de 1.80 m. Mide largo, ancho y alto de cada modelo (renderers, sin partículas) y toma una foto
    /// con el jugador de pie al lado (s1t-T-696-escala-*.png con CAPTURAR_S1=1).
    /// </summary>
    public class EscalaCarrosT696Pruebas : BaseJugadorAPie
    {
        private const string Carpeta = "Assets/_Proyecto/Vehiculos/Prefabs/";
        private GameObject vehiculo;

        public static readonly string[] Carros = { "CarroViejo", "Carro_V01a", "Carro_V01b", "Carro_V02", "Carro_V03", "Carro_V04", "Carro_V05", "Carro_V06", "Carro_V09", "Carro_V10" };

        public override void TearDown()
        {
            Object.Destroy(vehiculo);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Medicion([ValueSource(nameof(Carros))] string nombre)
        {
            yield return Calentar();
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab"), new Vector3(3f, 0f, 0f), Quaternion.identity);
            yield return Esperar(0.8f);
            Bounds b = default;
            bool hay = false;
            foreach (Renderer r in vehiculo.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || !r.enabled) continue;
                b = hay ? Unir(b, r.bounds) : r.bounds;
                hay = true;
            }
            Debug.Log("[T696] " + nombre + " largo " + b.size.z.ToString("F2") + " ancho " + b.size.x.ToString("F2") + " alto " + b.size.y.ToString("F2"));
            jugador.transform.position = new Vector3(3f - 1.9f, 0.05f, 1.2f);
            jugador.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return Esperar(0.5f);
            CapturaS1.Foto("s1t-T-696-escala-" + nombre, vehiculo.transform, new Vector3(-7f, 1.0f, 0f), new Vector3(0f, 0.9f, 0f), 40f, 1000, 600);
        }

        private static Bounds Unir(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }
    }
}
