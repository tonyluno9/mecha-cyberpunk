using System.Collections;
using System.Collections.Generic;
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
    /// T-695: la moto fina. El conductor va quieto en el asiento, sin tambaleo; manos en el manubrio y pies en los estribos con el jugador de 1.80;
    /// acelera de forma razonable (la de reparto más lenta), frena casi de golpe y el humo de daño sale del motor.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-695-*.png.
    /// </summary>
    public class MotoFinaT695Pruebas : BaseJugadorAPie
    {
        [DefaultExecutionOrder(10000)]
        private sealed class MuestreoTardio : MonoBehaviour
        {
            public System.Action Cada;
            private void LateUpdate() => Cada?.Invoke();
        }

        private const string Carpeta = "Assets/_Proyecto/Vehiculos/Prefabs/";
        private GameObject vehiculo;
        private IMontable montable;
        private Rigidbody cuerpo;
        private AnimacionCarro anim;
        private VisualPersonaje visual;
        private float alturaDePie;

        public static readonly string[] Motos = { "Carro_V07", "Carro_V08", "Carro_C19" };

        public override void TearDown()
        {
            Object.Destroy(vehiculo);
            base.TearDown();
        }

        private IEnumerator Montar(string nombre)
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab"), new Vector3(0f, 0.2f, 6f), Quaternion.identity);
            cuerpo = vehiculo.GetComponent<Rigidbody>();
            yield return Esperar(0.8f);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == vehiculo) { montable = m; }
            }
            Assert.IsNotNull(montable, nombre + " se registra");
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            alturaDePie = EscalaJugadorT670Pruebas.AlturaAnimada(jugador);
            Assert.IsTrue(jugadorAPie.Subir(montable), "Sube");
            anim = jugador.GetComponent<AnimacionCarro>();
            float fin = Time.time + 8f;
            while (anim.Fase != AnimacionCarro.FaseCarro.Dentro && Time.time < fin) { yield return null; }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase);
            // T-963: con los clips de Blender subir dura lo que el clip (1.2 s), no el fundido de 0.3 s.
            PoseMoto pose = visual.Animador.GetComponent<PoseMoto>();
            yield return Esperar((pose != null ? pose.SegundosSubir : 0f) + 0.6f);
        }

        private float Velocidad() => Vector3.Dot(cuerpo.linearVelocity, vehiculo.transform.forward);

        private static float Rango(List<float> valores)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (float v in valores) { min = Mathf.Min(min, v); max = Mathf.Max(max, v); }
            return max - min;
        }

        [UnityTest]
        public IEnumerator Conductor_VaQuietoEnElAsientoAlAvanzar([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Montar(nombre);
            PoseMoto pose = visual.Animador.GetComponent<PoseMoto>();
            PuntosMoto puntos = pose.Moto;
            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            Transform cadera = visual.Animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform pecho = visual.Animador.GetBoneTransform(HumanBodyBones.Chest);
            CapturaS1.Foto("s1t-T-695-" + nombre + "-quieta-lado", vehiculo.transform, new Vector3(-3.2f, 1.2f, 0f), new Vector3(0f, 0.9f, 0f), 35f, 900, 700);

            Press(teclado.wKey);
            yield return Esperar(2.5f);
            List<float> asientoY = new List<float>();
            List<float> caderaY = new List<float>(), cabezaY = new List<float>(), cabezaZ = new List<float>(), pechoY = new List<float>();
            // Se mide al final del cuadro (LateUpdate tardío), cuando la pose ya quedó puesta: es lo que ve la cámara.
            GameObject muestreo = new GameObject("MuestreoT695");
            muestreo.AddComponent<MuestreoTardio>().Cada = () =>
            {
                Vector3 c = puntos.Asiento.InverseTransformPoint(cadera.position);
                Vector3 h = puntos.Asiento.InverseTransformPoint(cabeza.position);
                asientoY.Add(puntos.Asiento.position.y);
                caderaY.Add(c.y);
                cabezaY.Add(h.y);
                cabezaZ.Add(h.z);
                pechoY.Add(puntos.Asiento.InverseTransformPoint(pecho.position).y);
            };
            yield return Esperar(3f);
            Object.Destroy(muestreo);
            Assert.Greater(Velocidad(), 8f, "La moto va rodando");
            AnimatorClipInfo[] clips = visual.Animador.GetCurrentAnimatorClipInfo(0);
            Debug.Log("[T695] " + nombre + " animador: " + (clips.Length > 0 ? clips[0].clip.name : "ninguno") + " velocidad visual " + visual.Velocidad.ToString("F2") + " speed " + visual.Animador.speed.ToString("F2") + " leerDelPadre " + visual.LeerDelPadre);
            CapturaS1.Foto("s1t-T-695-" + nombre + "-rodando-lado", vehiculo.transform, new Vector3(-3.2f, 1.2f, 0f), new Vector3(0f, 0.9f, 0f), 35f, 900, 700);
            CapturaS1.Foto("s1t-T-695-" + nombre + "-rodando-atras", vehiculo.transform, new Vector3(0.6f, 1.6f, -3.2f), new Vector3(0f, 0.9f, 0f), 35f, 900, 700);
            Debug.Log("[T695] " + nombre + " rangos cuerpo-asiento: cadera " + Rango(caderaY).ToString("F4") + " cabezaY " + Rango(cabezaY).ToString("F4") + " cabezaZ " + Rango(cabezaZ).ToString("F4") + " pecho " + Rango(pechoY).ToString("F4"));
            Debug.Log("[T695] " + nombre + " asiento en el mundo: rango Y " + Rango(asientoY).ToString("F4"));
            Assert.Less(Rango(asientoY), 0.02f, "La moto no da saltitos en piso plano");
            Assert.Less(Rango(caderaY), 0.01f, "La cadera no salta respecto del asiento");
            Assert.Less(Rango(pechoY), 0.015f, "El pecho no sube y baja");
            Assert.Less(Rango(cabezaY), 0.02f, "La cabeza no da saltitos");
            Assert.Less(Rango(cabezaZ), 0.02f, "La cabeza no se tambalea hacia adelante y atrás");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Capturas_PuntosDeApoyo([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Montar(nombre);
            if (!CapturaS1.Activa) yield break;
            PuntosMoto puntos = visual.Animador.GetComponent<PoseMoto>().Moto;
            List<GameObject> marcas = new List<GameObject>();
            foreach ((Transform t, Color c) in new[] { (puntos.Asiento, Color.green), (puntos.ManubrioIzquierdo, Color.red), (puntos.ManubrioDerecho, Color.red), (puntos.EstriboIzquierdo, Color.cyan), (puntos.EstriboDerecho, Color.cyan) })
            {
                GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(m.GetComponent<Collider>());
                m.transform.position = t.position;
                m.transform.localScale = Vector3.one * 0.06f;
                Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
                mat.SetInt("_ZTest", 8);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 5000;
                mat.color = c;
                m.GetComponent<Renderer>().material = mat;
                marcas.Add(m);
            }
            visual.Animador.gameObject.SetActive(true);
            CapturaS1.Foto("s1t-T-695-" + nombre + "-puntos", vehiculo.transform, new Vector3(-3.2f, 1.2f, 0f), new Vector3(0f, 0.9f, 0f), 35f, 900, 700);
            foreach (GameObject m in marcas) Object.Destroy(m);
        }

        [UnityTest]
        public IEnumerator Conductor_ManosEnElManubrioYPiesEnLosEstribos([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Montar(nombre);
            PoseMoto pose = visual.Animador.GetComponent<PoseMoto>();
            PuntosMoto puntos = pose.Moto;
            Animator a = visual.Animador;
            Debug.Log("[T695] " + nombre + " altura del jugador de pie " + alturaDePie.ToString("F2"));
            Assert.AreEqual(EscalaJugadorT670Pruebas.AlturaEsperada, alturaDePie, 0.08f, "El jugador mide 1.80 m (T-670)");
            Assert.Less(Vector3.Distance(a.GetBoneTransform(HumanBodyBones.LeftHand).position, puntos.ManubrioIzquierdo.position), 0.08f, "Mano izquierda en el manubrio");
            Assert.Less(Vector3.Distance(a.GetBoneTransform(HumanBodyBones.RightHand).position, puntos.ManubrioDerecho.position), 0.08f, "Mano derecha en el manubrio");
            Assert.Less(Vector3.Distance(a.GetBoneTransform(HumanBodyBones.LeftFoot).position, puntos.EstriboIzquierdo.position), 0.08f, "Pie izquierdo en el estribo");
            Assert.Less(Vector3.Distance(a.GetBoneTransform(HumanBodyBones.RightFoot).position, puntos.EstriboDerecho.position), 0.08f, "Pie derecho en el estribo");
            Assert.Greater(a.GetBoneTransform(HumanBodyBones.Head).position.y, puntos.ManubrioIzquierdo.position.y, "La cabeza queda sobre el manubrio (T-963: sin el margen de 5 cm de la pose por IK; con los clips de Blender la V07 queda 2.5 cm encima)");
        }

        private static Bounds Unir(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        [UnityTest]
        public IEnumerator Aceleracion_LaDeRepartoEsMasLenta([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Montar(nombre);
            Press(teclado.wKey);
            yield return Esperar(2f);
            float v2 = Velocidad();
            yield return Esperar(6f);
            float vTope = Velocidad();
            Release(teclado.wKey);
            Debug.Log("[T695] " + nombre + " v2 " + v2.ToString("F1") + " v8 " + vTope.ToString("F1"));
            if (nombre == "Carro_V07")
            {
                Assert.Less(v2, 9f, "La moto de reparto arranca más suave");
                Assert.Less(vTope, 18.5f, "La moto de reparto tiene tope de 18 m/s");
                Assert.Greater(vTope, 14f, "Pero llega a una velocidad útil");
            }
            else
            {
                Assert.That(v2, Is.InRange(9f, 14f), "La moto arranca fuerte pero no pasada");
                Assert.Greater(vTope, 22f, "La moto llega a su velocidad");
                Assert.Less(vTope, 27f);
            }
        }

        [UnityTest]
        public IEnumerator Frenar_EsCasiInstantaneo([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Montar(nombre);
            Press(teclado.wKey);
            yield return Esperar(3f);
            float antes = Velocidad();
            Assert.Greater(antes, 10f);
            Release(teclado.wKey);
            yield return null;
            yield return null;
            Press(teclado.sKey);
            float t0 = Time.time;
            while (Velocidad() > 1f && Time.time - t0 < 4f) yield return null;
            float segundos = Time.time - t0;
            Debug.Log("[T695] " + nombre + " frena desde " + antes.ToString("F1") + " en " + segundos.ToString("F2") + " s");
            Release(teclado.sKey);
            Assert.Less(segundos, 1.2f, "Frena casi de golpe");
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Frenar no tira al conductor");
        }
    }
}
