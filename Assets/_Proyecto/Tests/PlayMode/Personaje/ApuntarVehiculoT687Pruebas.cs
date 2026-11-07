using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-687: manejando un carro o una moto, el clic derecho deja apuntar libre con el ratón desde la cabeza del conductor, la pistola sale
    /// y el clic izquierdo dispara hacia los lados; al soltar vuelve la cámara del vehículo y se sigue manejando.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-687-*.png.
    /// </summary>
    public class ApuntarVehiculoT687Pruebas : BaseJugadorAPie
    {
        private sealed class Victima : MonoBehaviour, IRecibeDanio
        {
            public float Recibido;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Recibido += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject vehiculo;
        private GameObject victimaObjeto;
        private IMontable montable;
        private ApuntarDesdeVehiculo apuntar;

        public override void TearDown()
        {
            Object.Destroy(vehiculo);
            Object.Destroy(victimaObjeto);
            base.TearDown();
        }

        private IEnumerator Subir(string prefab, Vector3 posicion)
        {
            yield return Calentar();
            jugador.GetComponentInChildren<VisualPersonaje>(true).Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), posicion, Quaternion.identity);
            yield return Esperar(0.8f);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == vehiculo) { montable = m; }
            }
            Assert.IsNotNull(montable, "El vehículo se registra");
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            Assert.IsTrue(jugadorAPie.Subir(montable), "Sube");
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            float fin = Time.time + 8f;
            while (anim.Fase != AnimacionCarro.FaseCarro.Dentro && Time.time < fin) { yield return null; }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase);
            yield return Esperar(0.5f);
            apuntar = jugador.GetComponent<ApuntarDesdeVehiculo>();
            Assert.IsNotNull(apuntar, "El jugador trae ApuntarDesdeVehiculo");
            // El jugador usa la pistola en el vehículo.
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().TieneArma(ArmaAPie.Pistola), "Lleva la pistola");
        }

        private IEnumerator Mover(float x, float y, int cuadros)
        {
            for (int i = 0; i < cuadros; i++)
            {
                Set(raton.delta, new Vector2(x, y));
                yield return null;
            }
            Set(raton.delta, Vector2.zero);
            yield return null;
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
        }

        private IEnumerator ApuntarYDisparar(float distanciaCamara, float maximaApuntando)
        {
            Transform camara = jugadorAPie.Camara.transform;
            Vector3 camaraAntes = camara.position;
            Assert.IsFalse(apuntar.Apuntando, "Sin clic derecho no apunta");
            Assert.Greater(Vector3.Distance(camaraAntes, montable.Asiento.position), distanciaCamara, "La cámara del vehículo va detrás");

            Press(raton.rightButton);
            yield return Esperar(0.4f);
            Assert.IsTrue(apuntar.Apuntando, "Con el clic derecho apunta libre");
            Assert.Less(Vector3.Distance(camara.position, montable.Asiento.position), maximaApuntando, "La cámara pasa al conductor (cabeza en la moto, detrás del hombro en el carro)");
            Assert.AreEqual(0f, Vector3.Angle(camara.forward, apuntar.Direccion), 0.5f, "La cámara mira donde apunta");
            CapturaS1.FotoCamara("s1t-T-687-" + vehiculo.name.Split('(')[0] + "-vista", jugadorAPie.Camara);

            // Gira el ratón a la derecha unos 90° (0.12 °/px): la mira se va hacia el lado del vehículo.
            float yawAntes = apuntar.Yaw;
            yield return Mover(75f, 0f, 10);
            float girado = Mathf.DeltaAngle(yawAntes, apuntar.Yaw);
            Assert.That(girado, Is.InRange(70f, 110f), "El ratón gira la mira libremente: " + girado);
            Vector3 lado = apuntar.Direccion;
            lado.y = 0f;

            // Una víctima justo en esa dirección, a la altura del asiento.
            victimaObjeto = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            victimaObjeto.transform.position = montable.Asiento.position + lado.normalized * 9f;
            victimaObjeto.transform.position = new Vector3(victimaObjeto.transform.position.x, montable.Asiento.position.y + 0.3f, victimaObjeto.transform.position.z);
            Victima victima = victimaObjeto.AddComponent<Victima>();
            Physics.SyncTransforms();
            yield return Esperar(0.2f);
            CapturaS1.FotoMira("s1t-T-687-" + vehiculo.name.Split('(')[0] + "-lado", jugador.transform, montable.Asiento.position + Vector3.up * 0.8f, new Vector3(1f, 0.5f, 0.3f), 3.2f, 60f, 900);
            yield return Clic();
            yield return Esperar(0.3f);
            Assert.Greater(victima.Recibido, 0f, "El disparo sale hacia el lado donde apunta la cámara");

            Release(raton.rightButton);
            yield return Esperar(1.2f);
            Assert.IsFalse(apuntar.Apuntando, "Al soltar deja de apuntar");
            Assert.Greater(Vector3.Distance(camara.position, montable.Asiento.position), distanciaCamara, "Vuelve la cámara del vehículo");
        }

        [UnityTest]
        public IEnumerator Carro_ClicDerecho_ApuntaLibreYDisparaALosLados()
        {
            yield return Subir("Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab", new Vector3(0f, 0f, 3f));
            yield return ApuntarYDisparar(2.4f, 4f);
        }

        [UnityTest]
        public IEnumerator Moto_ClicDerecho_ApuntaConUnaMano()
        {
            yield return Subir("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V07.prefab", new Vector3(0f, 0.3f, 6f));
            Assert.IsTrue(apuntar != null);
            yield return ApuntarYDisparar(1.5f, 2.2f);
        }

        [UnityTest]
        public IEnumerator Manejar_SigueFuncionandoApuntandoYSinApuntar()
        {
            yield return Subir("Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab", new Vector3(0f, 0f, 3f));
            Rigidbody cuerpo = vehiculo.GetComponent<Rigidbody>();
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            Assert.Greater(cuerpo.linearVelocity.magnitude, 0.5f, "Acelera sin apuntar");
            Press(raton.rightButton);
            yield return Esperar(0.5f);
            Assert.IsTrue(apuntar.Apuntando);
            Assert.Greater(cuerpo.linearVelocity.magnitude, 0.5f, "Sigue avanzando mientras apunta");
            Release(raton.rightButton);
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Press(teclado.sKey);
            yield return Esperar(2.5f);
            Release(teclado.sKey);
            Assert.Less(Vector3.Dot(cuerpo.transform.InverseTransformDirection(cuerpo.linearVelocity), Vector3.forward), 0.3f, "La reversa sigue funcionando");
            // Solo se baja con el vehículo casi parado.
            bool bajo = false;
            float fin = Time.time + 8f;
            while (!bajo && Time.time < fin)
            {
                bajo = jugadorAPie.Bajar();
                yield return null;
            }
            Assert.IsTrue(bajo, "Se baja del vehículo");
            yield return Esperar(2f);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.IsFalse(apuntar.Apuntando);
        }
    }
}
