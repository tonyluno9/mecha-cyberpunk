using System.Collections;
using System.IO;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-706: el aliado se sube a tu carro por la puerta del copiloto con animación (sin volar), va dentro mientras manejas y se baja contigo.
    /// Si el carro es una moto, o ya hay otro aliado en el asiento del copiloto, no sube y te sigue a pie. Jugador real y entrada simulada.
    /// </summary>
    public class AliadoCarroT706Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaAliado = "Assets/_Proyecto/IA/Prefabs/Aliado.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V09.prefab";
        private const string RutaMoto = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab";

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private GameObject jugadorObjeto;
        private readonly List<GameObject> aliados = new List<GameObject>();
        private readonly List<GameObject> extras = new List<GameObject>();
        private IJugador jugador;
        private IMontable carro;
        private Rigidbody cuerpoCarro;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            foreach (GameObject a in aliados)
            {
                Object.Destroy(a);
            }
            aliados.Clear();
            foreach (GameObject e in extras)
            {
                Object.Destroy(e);
            }
            extras.Clear();
            Object.Destroy(carroObjeto);
            Object.Destroy(jugadorObjeto);
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Con varias cargas seguidas un Press o un Release suelto a veces no se aplica: se repite hasta que la tecla quede como se pide.
        private IEnumerator Sostener(KeyControl tecla)
        {
            for (int i = 0; i < 8 && !tecla.isPressed; i++)
            {
                Press(tecla);
                yield return null;
            }
        }

        private IEnumerator Soltar(KeyControl tecla)
        {
            for (int i = 0; i < 8 && (i == 0 || tecla.isPressed); i++)
            {
                Release(tecla);
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        // El carro (o la moto), el jugador junto a la puerta del conductor y los aliados del lado del copiloto (uno solo a 10 m, para verlo caminar; dos a 4 m).
        private IEnumerator Escena(string rutaVehiculo, int cuantosAliados)
        {
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rutaVehiculo), new Vector3(0f, 0.5f, 10f), Quaternion.identity);
            carro = carroObjeto.GetComponent<IMontable>();
            cuerpoCarro = carroObjeto.GetComponent<Rigidbody>();
            jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            yield return Esperar(1.5f);
            jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador);
            Vector3 punto = carro.PuntoSubida;
            Vector3 lado = punto - carroObjeto.transform.position;
            lado.y = 0f;
            lado = Vector3.Project(lado, carroObjeto.transform.right).normalized;
            Vector3 posicion = punto + lado * 1.2f;
            posicion.y = 0.05f;
            Vector3 mirada = punto - posicion;
            mirada.y = 0f;
            jugador.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
            ladoDelCopiloto = -lado;
            CrearCamaraDeFoto();
            for (int i = 0; i < cuantosAliados; i++)
            {
                Vector3 desdeElOtroLado = carroObjeto.transform.position - lado * (cuantosAliados == 1 ? 10f : 4f + i * 1.5f) + carroObjeto.transform.forward * (i * 1.0f);
                desdeElOtroLado.y = 0.05f;
                aliados.Add(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaAliado), desdeElOtroLado, Quaternion.identity));
            }
            yield return Esperar(0.1f);
        }

        // Cuántos cuadros vio el aliado en cada fase y en cada estado de la animación, y lo más alto que estuvo.
        private Camera camaraFoto;
        private Vector3 ladoDelCopiloto;

        // Una cámara que ve el costado del copiloto (sin ninguna cámara que lo vea, Unity no avanza la animación de un personaje: culling).
        private void CrearCamaraDeFoto()
        {
            GameObject luz = new GameObject("LuzDePrueba");
            extras.Add(luz);
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject c = new GameObject("CamaraDePrueba");
            extras.Add(c);
            camaraFoto = c.AddComponent<Camera>();
            camaraFoto.clearFlags = CameraClearFlags.SolidColor;
            camaraFoto.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            c.transform.position = carroObjeto.transform.position + ladoDelCopiloto * 6f + carroObjeto.transform.forward * 1.5f + Vector3.up * 2.2f;
            c.transform.LookAt(carroObjeto.transform.position + Vector3.up * 0.8f);
        }

        private void Foto(string nombre, Vector3 punto)
        {
            camaraFoto.transform.position = punto + ladoDelCopiloto * 5.5f + carroObjeto.transform.forward * 1.2f + Vector3.up * 2f;
            camaraFoto.transform.LookAt(punto + Vector3.up * 0.8f);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camaraFoto.targetTexture = rt;
            camaraFoto.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camaraFoto.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", nombre), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
        }

        private sealed class Registro706
        {
            public bool VioAcercando;
            public bool VioEntrando;
            public bool VioCarroEntrar;
            public bool VioCarroSalir;
            public float MaximaAltura;
            public Vector3 PosicionAlEntrar;
            public string Historial = "";
            public float InicioFase;
            public bool FotoAcercando;
            public bool FotoSubiendo;
            public bool FotoBajando;
            public AliadoEnCarro.FaseCarro Ultima = AliadoEnCarro.FaseCarro.Fuera;
        }

        private IEnumerator Observar(AliadoAPie aliado, AliadoEnCarro enCarro, VisualPersonaje visual, float segundos, Registro706 r, System.Func<bool> parar)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin && !parar())
            {
                yield return null;
                if (enCarro.Fase != r.Ultima)
                {
                    r.Ultima = enCarro.Fase;
                    r.InicioFase = Time.time;
                    r.Historial += enCarro.Fase + "@" + Time.time.ToString("F2") + " pos " + aliado.transform.position.ToString("F1") + " dt " + Time.deltaTime.ToString("F3") + "; ";
                }
                float enFase = Time.time - r.InicioFase;
                if (enCarro.Fase == AliadoEnCarro.FaseCarro.Acercando && !r.FotoAcercando && enFase > 0.25f)
                {
                    r.FotoAcercando = true;
                    Foto("s1c-T-706-aliado-camina-a-la-puerta.png", Vector3.Lerp(aliado.transform.position, carroObjeto.transform.position, 0.35f));
                }
                if (enCarro.Fase == AliadoEnCarro.FaseCarro.Entrando && !r.FotoSubiendo && enFase > 0.5f)
                {
                    r.FotoSubiendo = true;
                    Foto("s1c-T-706-aliado-subiendo.png", carroObjeto.transform.position);
                }
                if (enCarro.Fase == AliadoEnCarro.FaseCarro.Saliendo && !r.FotoBajando && enFase > 0.6f)
                {
                    r.FotoBajando = true;
                    Foto("s1c-T-706-aliado-bajando.png", carroObjeto.transform.position);
                }
                if (enCarro.Fase == AliadoEnCarro.FaseCarro.Acercando)
                {
                    r.VioAcercando = true;
                }
                if (enCarro.Fase == AliadoEnCarro.FaseCarro.Entrando)
                {
                    if (!r.VioEntrando)
                    {
                        r.PosicionAlEntrar = aliado.transform.position;
                    }
                    r.VioEntrando = true;
                }
                if (!aliado.Abordo)
                {
                    r.MaximaAltura = Mathf.Max(r.MaximaAltura, aliado.transform.position.y);
                }
                AnimatorStateInfo estado = visual.Animador.GetCurrentAnimatorStateInfo(0);
                r.VioCarroEntrar |= estado.IsName("CarroEntrar");
                r.VioCarroSalir |= estado.IsName("CarroSalir");
            }
        }

        [UnityTest]
        public IEnumerator Aliado_SeSubePorElCopilotoManejasYBajaContigo()
        {
            yield return Escena(RutaCarro, 1);
            AliadoAPie aliado = aliados[0].GetComponent<AliadoAPie>();
            AliadoEnCarro enCarro = aliados[0].GetComponent<AliadoEnCarro>();
            VisualPersonaje visual = aliados[0].GetComponentInChildren<VisualPersonaje>(true);
            Assert.IsNotNull(enCarro, "El aliado lleva AliadoEnCarro");
            Registro706 r = new Registro706();
            float ladoConductor = Mathf.Sign(Vector3.Dot(carro.PuntoSubida - carroObjeto.transform.position, carroObjeto.transform.right));

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "E sube al carro");
            Assert.IsFalse(aliado.Abordo, "El aliado no aparece de golpe adentro");

            yield return Observar(aliado, enCarro, visual, 10f, r, () => aliado.Abordo);
            Assert.IsTrue(r.VioAcercando, "Camina hasta la puerta (historial " + r.Historial + ", fase " + enCarro.Fase + ", aliado " + aliado.transform.position + ", carro " + carroObjeto.transform.position + ", rapidez " + AliadoEnCarro.RapidezDe(carro) + ", puede " + enCarro.PuedeSubir(carro, true) + ", montable " + (AliadoEnCarro.MontableDelJugador() != null) + ", siguiendo " + aliado.Siguiendo + ", modo " + jugador.Modo + ")");
            Assert.IsTrue(r.VioEntrando, "Juega la subida");
            Assert.IsTrue(r.VioCarroEntrar, "Con la misma animación de subir del jugador (Carro_Entrar)");
            Assert.IsTrue(aliado.Abordo, "Termina dentro del carro");
            Assert.AreEqual(AliadoEnCarro.FaseCarro.Dentro, enCarro.Fase);
            float ladoAlEntrar = Mathf.Sign(Vector3.Dot(r.PosicionAlEntrar - carroObjeto.transform.position, carroObjeto.transform.right));
            Assert.AreEqual(-ladoConductor, ladoAlEntrar, "Sube por el lado del copiloto, no por el del conductor");
            Assert.Less(r.MaximaAltura, 1.2f, "Sube caminando, sin volar (altura máxima " + r.MaximaAltura.ToString("F2") + " m)");

            // Manejas con el aliado dentro: va contigo.
            yield return Sostener(teclado.wKey);
            yield return Esperar(2f);
            yield return Soltar(teclado.wKey);
            Assert.IsTrue(aliado.Abordo, "Sigue dentro mientras manejas");
            Assert.Less(Vector3.Distance(aliado.transform.position, carroObjeto.transform.position), 2.5f, "Va con el carro");

            // Frenas y bajas: el aliado baja contigo por su puerta.
            yield return Sostener(teclado.sKey);
            float limite = Time.time + 8f;
            while (cuerpoCarro.linearVelocity.magnitude > 0.8f && Time.time < limite)
            {
                yield return null;
            }
            yield return Soltar(teclado.sKey);
            yield return Esperar(0.3f);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del carro (rapidez " + cuerpoCarro.linearVelocity.magnitude.ToString("F2") + ")");
            yield return Observar(aliado, enCarro, visual, 4f, r, () => enCarro.Fase == AliadoEnCarro.FaseCarro.Fuera && !aliado.Abordo && r.VioCarroSalir);
            Assert.IsFalse(aliado.Abordo, "El aliado baja contigo");
            Assert.IsTrue(r.VioCarroSalir, "Con la animación de bajar (Carro_Salir)");
            Assert.AreEqual(AliadoEnCarro.FaseCarro.Fuera, enCarro.Fase);
            Vector3 d = aliado.transform.position - jugador.Raiz.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 6f, "Queda junto a ti");
            Assert.Less(aliado.transform.position.y, 0.4f, "Con los pies en el piso");
        }

        [UnityTest]
        public IEnumerator Moto_ElAliadoNoSubeYTeSigueAPie()
        {
            yield return Escena(RutaMoto, 1);
            AliadoAPie aliado = aliados[0].GetComponent<AliadoAPie>();
            AliadoEnCarro enCarro = aliados[0].GetComponent<AliadoEnCarro>();
            Assert.IsTrue(AliadoEnCarro.EsMoto(carro), "El modelo V08 es una moto");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "Subes a la moto");
            yield return Esperar(4f);
            Assert.IsFalse(aliado.Abordo, "En la moto no sube");
            Assert.AreEqual(AliadoEnCarro.FaseCarro.Fuera, enCarro.Fase);
            Assert.IsTrue(aliado.gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator DosAliados_SoloUnoSubeAlAsientoDelCopiloto()
        {
            yield return Escena(RutaCarro, 2);
            AliadoEnCarro a = aliados[0].GetComponent<AliadoEnCarro>();
            AliadoEnCarro b = aliados[1].GetComponent<AliadoEnCarro>();
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo);
            yield return Esperar(8f);
            int dentro = (aliados[0].GetComponent<AliadoAPie>().Abordo ? 1 : 0) + (aliados[1].GetComponent<AliadoAPie>().Abordo ? 1 : 0);
            Assert.AreEqual(1, dentro, "Un solo asiento de copiloto: sube uno");
            AliadoEnCarro fuera = a.Fase == AliadoEnCarro.FaseCarro.Dentro ? b : a;
            Assert.AreEqual(AliadoEnCarro.FaseCarro.Fuera, fuera.Fase, "El otro espera o te sigue a pie");
            Assert.IsTrue(fuera.gameObject.activeInHierarchy);
        }
    }
}
