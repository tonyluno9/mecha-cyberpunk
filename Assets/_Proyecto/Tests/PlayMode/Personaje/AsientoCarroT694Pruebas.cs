using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-694: sentado en un carro, la cabeza del conductor queda a la altura de los ojos del modelo (se ve bien por la ventana):
    /// por encima de la línea de la carrocería y por debajo del techo. Todos los carros (V02 a V10 sin motos, la patrulla V05 y el viejo).
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-694-*.png (de lado y desde atrás).
    /// </summary>
    public class AsientoCarroT694Pruebas : BaseJugadorAPie
    {
        private const string Carpeta = "Assets/_Proyecto/Vehiculos/Prefabs/";
        private const string RutaConfigCamara = "Assets/_Proyecto/Vehiculos/Datos/ConfigCamaraCarro_Viejo.asset";
        private const float MargenCabezaSobreCinturon = 0.12f;
        private const float MargenCabezaBajoTecho = 0.08f;
        private const float TopeDeLaCabeza = 0.12f;
        private GameObject vehiculo;

        public override void TearDown()
        {
            Object.Destroy(vehiculo);
            base.TearDown();
        }

        public static readonly string[] Carros = { "CarroViejo", "Carro_V01a", "Carro_V01b", "Carro_V02", "Carro_V03", "Carro_V04", "Carro_V05", "Carro_V06", "Carro_V09", "Carro_V10" };

        [UnityTest]
        public IEnumerator Sentado_LaCabezaSeVePorLaVentana([ValueSource(nameof(Carros))] string nombre)
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab"), new Vector3(0f, 0f, 3f), Quaternion.identity);
            yield return Esperar(0.8f);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == vehiculo) { montable = m; }
            }
            Assert.IsNotNull(montable, nombre + " se registra");
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            Assert.IsTrue(jugadorAPie.Subir(montable), "Sube");
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            float fin = Time.time + 8f;
            while (anim.Fase != AnimacionCarro.FaseCarro.Dentro && Time.time < fin) { yield return null; }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase);
            yield return Esperar(0.6f);

            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            Transform raiz = vehiculo.transform;
            Bounds cuerpo = default;
            bool hay = false;
            foreach (BoxCollider c in vehiculo.GetComponentsInChildren<BoxCollider>())
            {
                if (c.isTrigger) continue;
                cuerpo = hay ? Unir(cuerpo, c.bounds) : c.bounds;
                hay = true;
            }
            float techo = cuerpo.max.y - raiz.position.y;
            float cinturon = 0f;
            foreach (BoxCollider c in vehiculo.GetComponentsInChildren<BoxCollider>())
            {
                if (!c.isTrigger && c.name == "Carroceria") cinturon = c.bounds.max.y - raiz.position.y;
            }
            float alturaCabeza = raiz.InverseTransformPoint(cabeza.position).y;
            float ojos = montable.Asiento.localPosition.y + AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Vehiculos.ConfigCamaraCarro>(RutaConfigCamara).AlturaOjos;
            Debug.Log("[T694] " + nombre + " cabeza " + alturaCabeza.ToString("F2") + " ojos " + ojos.ToString("F2") + " cinturon " + cinturon.ToString("F2") + " techo " + techo.ToString("F2"));

            // Fotos de lado (lado del conductor) y desde atrás, con la cabeza visible.
            jugadorAPie.Camara.enabled = false;
            Vector3 centro = new Vector3(montable.Asiento.localPosition.x, ojos * 0.85f, 0f);
            CapturaS1.Foto("s1t-T-694-" + nombre + "-lado", raiz, new Vector3(-4.5f, ojos * 0.95f, 0f), centro, 32f, 900, 600);
            CapturaS1.Foto("s1t-T-694-" + nombre + "-cerca", raiz, new Vector3(-2.6f, alturaCabeza + 0.1f, 0.4f), new Vector3(montable.Asiento.localPosition.x, alturaCabeza - 0.05f, 0f), 30f, 900, 600);
            CapturaS1.Foto("s1t-T-694-" + nombre + "-atras", raiz, new Vector3(montable.Asiento.localPosition.x * 0.4f, ojos * 1.15f, -5.5f), centro, 32f, 900, 600);

            Assert.Greater(alturaCabeza, cinturon + MargenCabezaSobreCinturon, nombre + ": la cabeza queda por encima de la línea de la carrocería");
            Assert.Less(alturaCabeza + TopeDeLaCabeza, techo - MargenCabezaBajoTecho, nombre + ": la cabeza no pasa del techo");
            // A media ventana: ni pegada a la carrocería ni al techo.
            float fraccion = (alturaCabeza - cinturon) / (techo - cinturon);
            Assert.That(fraccion, Is.InRange(0.1f, 0.5f), nombre + ": la cabeza va a media ventana (fracción " + fraccion.ToString("F2") + ")");
        }

        private static Bounds Unir(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }
    }
}
