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
    /// T-656 / T-657: el personaje se acomoda pegado a la puerta antes del clip, termina sentado y al bajar hace el camino contrario,
    /// y bajar nunca se traba (si la puerta del conductor está bloqueada sale por el otro lado, por atrás o por arriba).
    /// </summary>
    public class EntrarCarroT656Pruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const float MaxSaltoPorFrame = 0.6f;
        private const float MaxSeparacionAlCarro = 0.5f;
        private const float MaxDeltaParaMedirSalto = 0.05f;
        private readonly List<GameObject> carros = new List<GameObject>();
        private IMontable carro;
        private GameObject carroObj;

        public override void TearDown()
        {
            foreach (GameObject c in carros) { if (c != null) { Object.Destroy(c); } }
            carros.Clear();
            base.TearDown();
        }

        private IEnumerator PrepararCarro(Vector3 posicion, float yaw = 0f)
        {
            yield return Calentar();
            carroObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), posicion, Quaternion.Euler(0f, yaw, 0f));
            carros.Add(carroObj);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == carroObj) { carro = m; }
            }
            Assert.IsNotNull(carro, "El carro se registra");
            yield return Esperar(1f);
        }

        private float DistanciaAlCarro(Vector3 p)
        {
            float mejor = float.MaxValue;
            foreach (Collider c in carroObj.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger) continue;
                mejor = Mathf.Min(mejor, Vector3.Distance(c.ClosestPoint(p), p));
            }
            return mejor;
        }

        private IEnumerator Tecla()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
        }

        [UnityTest]
        public IEnumerator Subir_DesdeDosMetros_ElPersonajeLlegaPegadoALaPuertaAntesDelClipYTerminaSentado()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            Vector3 puerta = carro.PuntoSubida;
            Vector3 inicio = puerta + new Vector3(-2.0f, 0f, 0f);
            inicio.y = 0.05f;
            jugadorAPie.Colocar(new Pose(inicio, Quaternion.LookRotation(Vector3.left)));
            yield return Esperar(0.3f);
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            Animator a = visual.Animador;
            Transform raiz = visual.transform;
            CapturaMundoS1.Foto("s1-subir-carro-0-antes", carroObj.transform.position + new Vector3(-6f, 2.2f, -3.5f), carroObj.transform.position + new Vector3(-1.5f, 0.9f, 0f), 45f);

            yield return Tecla();
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
            Vector3 anterior = raiz.position;
            bool midiendo = false;
            float saltoMax = 0f;
            float distanciaCuandoEmpiezaElClip = -1f;
            float alturaCuandoEmpiezaElClip = float.MaxValue;
            bool capturado = false;
            float limite = Time.time + 6f;
            while (anim.Fase == AnimacionCarro.FaseCarro.Entrando && Time.time < limite)
            {
                yield return null;
                if (Time.deltaTime < MaxDeltaParaMedirSalto && midiendo)
                {
                    saltoMax = Mathf.Max(saltoMax, Vector3.Distance(anterior, raiz.position));
                }
                midiendo = true;
                anterior = raiz.position;
                bool enClip = a.GetCurrentAnimatorStateInfo(0).IsName("CarroEntrar") || (a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).IsName("CarroEntrar"));
                if (enClip && distanciaCuandoEmpiezaElClip < 0f)
                {
                    distanciaCuandoEmpiezaElClip = DistanciaAlCarro(raiz.position);
                    alturaCuandoEmpiezaElClip = raiz.position.y;
                }
                if (enClip && !capturado)
                {
                    capturado = true;
                    CapturaMundoS1.Foto("s1-subir-carro-1-en-la-puerta", carroObj.transform.position + new Vector3(-4.2f, 1.6f, -2.2f), raiz.position + Vector3.up * 0.9f, 40f);
                }
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase, "Terminó de entrar");
            Assert.GreaterOrEqual(distanciaCuandoEmpiezaElClip, 0f, "Jugó Carro_Entrar");
            Assert.LessOrEqual(distanciaCuandoEmpiezaElClip, MaxSeparacionAlCarro, "Al empezar el clip está pegado al carro (sin hueco)");
            Assert.LessOrEqual(alturaCuandoEmpiezaElClip, 0.15f, "Al empezar el clip tiene los pies en el piso (no flota)");
            Assert.Less(saltoMax, MaxSaltoPorFrame, "Sin saltos bruscos al acomodarse");
            yield return Esperar(0.4f);
            Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
            Assert.Less(Vector3.Distance(cadera, carro.Asiento.position), 0.6f, "Termina sentado en el asiento");
            if (CapturaMundoS1.Activa)
            {
                foreach (Renderer r in carroObj.GetComponentsInChildren<Renderer>())
                {
                    string n = r.name.ToLower();
                    if (!n.Contains("rueda") && !n.Contains("wheel") && !n.Contains("llanta")) { r.enabled = false; }
                }
                CapturaMundoS1.Foto("s1-carro-sentado", carroObj.transform.position + new Vector3(-3.4f, 1.5f, 0.6f), carroObj.transform.position + new Vector3(-0.4f, 1.0f, -0.1f), 40f);
            }
        }

        [UnityTest]
        public IEnumerator Bajar_ElPersonajeCaminaDelAsientoALaPuertaYAlPisoSinSaltos()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            yield return SubirYEsperar();
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Transform raiz = jugador.GetComponentInChildren<VisualPersonaje>(true).transform;

            yield return Tecla();
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "La E baja");
            Vector3 anterior = raiz.position;
            bool midiendo = false;
            float saltoMax = 0f;
            float distanciaMedia = -1f;
            float inicio = Time.time;
            float limite = Time.time + 6f;
            while (anim.Fase == AnimacionCarro.FaseCarro.Saliendo && Time.time < limite)
            {
                yield return null;
                if (Time.deltaTime < MaxDeltaParaMedirSalto && midiendo)
                {
                    saltoMax = Mathf.Max(saltoMax, Vector3.Distance(anterior, raiz.position));
                }
                midiendo = true;
                anterior = raiz.position;
                if (distanciaMedia < 0f && Time.time - inicio > 0.45f) { distanciaMedia = DistanciaAlCarro(raiz.position); }
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Fuera, anim.Fase);
            Assert.Less(saltoMax, MaxSaltoPorFrame, "Sin saltos al bajar");
            Assert.GreaterOrEqual(distanciaMedia, 0f);
            Assert.LessOrEqual(distanciaMedia, MaxSeparacionAlCarro, "Al bajar pasa pegado a la carrocería, no flota a un lado");
            Vector3 fin = raiz.position - jugador.transform.position;
            fin.y = 0f;
            Assert.Less(fin.magnitude, 0.1f, "Termina en el piso donde queda el jugador");
        }

        // ---- T-657: bajarse nunca se traba ----

        private GameObject Muro(Vector3 centro, Vector3 tamano)
        {
            GameObject m = Caja(centro, tamano);
            m.name = "MuroPrueba";
            return m;
        }

        private Vector3 LocalACarro(Vector3 local)
        {
            return carroObj.transform.TransformPoint(local);
        }

        // Donde cae la salida de atrás: un metro detrás de las cajas de choque del carro.
        private float ZTraseraDeLaSalida()
        {
            float minimo = float.MaxValue;
            foreach (BoxCollider c in carroObj.GetComponentsInChildren<BoxCollider>())
            {
                Bounds b = c.bounds;
                minimo = Mathf.Min(minimo, carroObj.transform.InverseTransformPoint(b.center).z - b.extents.z);
            }
            return minimo - 1f;
        }

        private Pose PuertaOpuesta()
        {
            Vector3 local = carroObj.transform.InverseTransformPoint(carro.PuntoSubida);
            local.x = -local.x;
            return new Pose(carroObj.transform.TransformPoint(local), Quaternion.identity);
        }

        private IEnumerator SubirYEsperar()
        {
            jugadorAPie.Colocar(new Pose(carro.PuntoSubida + new Vector3(-1f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            yield return Tecla();
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            float limite = Time.time + 6f;
            while (anim.Fase != AnimacionCarro.FaseCarro.Dentro && Time.time < limite) { yield return null; }
            yield return Esperar(0.3f);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
        }

        private Vector3 BajarYPos()
        {
            Assert.IsTrue(jugadorAPie.Bajar(), "Puede bajar");
            return jugador.transform.position;
        }

        [UnityTest]
        public IEnumerator Bajar_ConLaPuertaDelConductorBloqueada_SaleDelOtroLado()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            yield return SubirYEsperar();
            Muro(carro.PuntoSubida + new Vector3(-0.2f, 1f, 0f), new Vector3(0.4f, 2f, 4f));
            yield return Esperar(0.1f);

            Vector3 local = carroObj.transform.InverseTransformPoint(BajarYPos());
            Assert.Greater(local.x, 0.5f, "Sale por el lado del copiloto (derecha)");
        }

        [UnityTest]
        public IEnumerator Bajar_ConLosDosLadosBloqueados_SalePorAtras()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            yield return SubirYEsperar();
            Muro(carro.PuntoSubida + new Vector3(-0.2f, 1f, 0f), new Vector3(0.4f, 2f, 4f));
            Muro(PuertaOpuesta().position + new Vector3(0.2f, 1f, 0f), new Vector3(0.4f, 2f, 4f));
            yield return Esperar(0.1f);

            Vector3 local = carroObj.transform.InverseTransformPoint(BajarYPos());
            Assert.Less(local.z, -1.5f, "Sale por atrás del carro");
            Assert.Less(Mathf.Abs(local.x), 1.2f, "Y centrado");
        }

        [UnityTest]
        public IEnumerator Bajar_ConLadosYAtrasBloqueados_SaleDesdeArriba()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            yield return SubirYEsperar();
            Muro(carro.PuntoSubida + new Vector3(-0.2f, 1f, 0f), new Vector3(0.4f, 2f, 4f));
            Muro(PuertaOpuesta().position + new Vector3(0.2f, 1f, 0f), new Vector3(0.4f, 2f, 4f));
            Muro(LocalACarro(new Vector3(0f, 1f, ZTraseraDeLaSalida())), new Vector3(6f, 2f, 0.4f));
            yield return Esperar(0.1f);

            Vector3 local = carroObj.transform.InverseTransformPoint(BajarYPos());
            Assert.Greater(local.y, 0.7f, "Sale por arriba: queda sobre el techo");
            Assert.Less(Mathf.Abs(local.x) + Mathf.Abs(local.z), 2f, "Encima del carro");
        }

        [UnityTest]
        public IEnumerator Bajar_SinObstaculos_SaleJuntoALaPuertaDelConductor()
        {
            yield return PrepararCarro(new Vector3(0f, 0.3f, 6f));
            yield return SubirYEsperar();
            Vector3 local = carroObj.transform.InverseTransformPoint(BajarYPos());
            Assert.Less(local.x, -1f, "Sale por la izquierda (conductor)");
        }
    }
}
