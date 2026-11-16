using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1109: el guardia pesado de Nova (E12) con el jugador real. De frente los puños y las balas entran a 15% (TipoBloqueo.Normal), por la espalda
    /// completos; romper el escudo lo aturde y lo vuelve castigable; sin provocarlo no ataca; provocado, el empujón conecta 18 a 1.5 m y cada tercer
    /// ataque es un mazazo de 28. Con T1109_CAPTURAS=1 escribe docs/capturas/t-1109/guardia-bloqueando.png.
    /// </summary>
    public class GuardiaPesadoT1109Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaGuardia = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab";
        private const string CarpetaFotos = "docs/capturas/t-1109";
        // El guardia mira al jugador (-Z): un golpe del jugador viaja a +Z, y -Direccion coincide con su frente.
        private static readonly Quaternion MiraAlJugador = Quaternion.Euler(0f, 180f, 0f);

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private VidaAPie vidaJugador;
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vidaJugador = jugador.GetComponent<VidaAPie>();
        }

        public override void TearDown()
        {
            foreach (GameObject e in extras)
            {
                Object.DestroyImmediate(e);
            }
            extras.Clear();
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private ConductaGuardiaPesado Crear(Vector3 posicion, Quaternion rotacion)
        {
            GameObject g = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaGuardia), posicion, rotacion);
            extras.Add(g);
            Physics.SyncTransforms();
            return g.GetComponent<ConductaGuardiaPesado>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static InfoDanio Golpe(PandilleroAPie p, float cantidad, Vector3 direccion, TipoDanio tipo = TipoDanio.Golpe)
        {
            return new InfoDanio(cantidad, tipo, p.transform.position + Vector3.up * 1.2f, direccion, null);
        }

        private IEnumerator Puno(PandilleroAPie guardia, float[] resultado)
        {
            float antes = guardia.Actual;
            Press(raton.leftButton);
            yield return null;
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.7f);
            resultado[0] = antes - guardia.Actual;
        }

        [UnityTest]
        public IEnumerator Datos_VidaDoscientosVeinteYVelocidadDosPuntoDos()
        {
            yield return null;
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 20f), MiraAlJugador);
            yield return null;
            PandilleroAPie p = c.GetComponent<PandilleroAPie>();
            Assert.AreEqual(220f, p.Maxima, 0.001f);
            Assert.AreEqual(2.2f, p.Datos.Velocidad, 0.001f);
            Assert.IsNotNull(c.Escudo.Placa, "el escudo tiene su placa visible");
        }

        [UnityTest]
        public IEnumerator Punos_DeFrente_QuitanMenosDeQuinceXCientoQueDeEspalda()
        {
            yield return Esperar(0.6f);
            float[] espalda = new float[1];
            float[] frente = new float[1];
            ConductaGuardiaPesado porDetras = Crear(new Vector3(0f, 0.05f, 1.6f), Quaternion.identity); // mira +Z: el jugador le pega por la espalda
            yield return Esperar(0.2f);
            yield return Puno(porDetras.GetComponent<PandilleroAPie>(), espalda);
            Assert.Greater(espalda[0], 0f, "el puño por la espalda entra");
            Object.DestroyImmediate(porDetras.gameObject);
            yield return Esperar(0.5f);

            ConductaGuardiaPesado deFrente = Crear(new Vector3(0f, 0.05f, 1.6f), MiraAlJugador);
            yield return Esperar(0.2f);
            yield return Puno(deFrente.GetComponent<PandilleroAPie>(), frente);
            Assert.Greater(frente[0], 0f, "el puño de frente entra reducido");
            Assert.LessOrEqual(frente[0], espalda[0] * 0.15f + 0.01f, "de frente <= 15% (" + frente[0] + " contra " + espalda[0] + ")");
        }

        [UnityTest]
        public IEnumerator Balas_DeFrente_SeReducenIgualQueLosPunos()
        {
            yield return Esperar(0.6f);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            armas.Equipar(ArmaAPie.Pistola);
            float limite = Time.time + 3f;
            while (armas.ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva);
            yield return Esperar(0.3f);
            float miraX = jugador.GetComponent<ControladorAPie>().Camara.position.x;
            // Como en T-677: un pedestal deja la mira a mitad del cuerpo del guardia (la cámara va sobre el hombro, más alta que su cabeza).
            float alturaPlataforma = jugador.GetComponent<ControladorAPie>().Camara.position.y - 1.0f;
            GameObject plataforma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(plataforma);
            plataforma.transform.position = new Vector3(miraX, alturaPlataforma / 2f, 12f);
            plataforma.transform.localScale = new Vector3(2f, alturaPlataforma, 2f);

            ConductaGuardiaPesado porDetras = Crear(new Vector3(miraX, alturaPlataforma + 0.05f, 12f), Quaternion.identity);
            PandilleroAPie p = porDetras.GetComponent<PandilleroAPie>();
            float antes = p.Actual;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.4f);
            float espalda = antes - p.Actual;
            Assert.Greater(espalda, 0f, "la bala por la espalda entra");
            Object.DestroyImmediate(porDetras.gameObject);
            yield return Esperar(0.5f);

            ConductaGuardiaPesado deFrente = Crear(new Vector3(miraX, alturaPlataforma + 0.05f, 12f), MiraAlJugador);
            p = deFrente.GetComponent<PandilleroAPie>();
            antes = p.Actual;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.4f);
            float frente = antes - p.Actual;
            Assert.Greater(frente, 0f);
            Assert.LessOrEqual(frente, espalda * 0.15f + 0.01f, "la bala de frente entra a 15% (" + frente + " contra " + espalda + ")");
            Assert.AreEqual(TipoBloqueo.Normal, p.RecibirDanio(Golpe(p, 5f, Vector3.forward, TipoDanio.Disparo)).Bloqueo);
        }

        [UnityTest]
        public IEnumerator Resultado_DeFrenteEsNormalYPorLaEspaldaNinguno()
        {
            yield return null;
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 15f), MiraAlJugador);
            yield return null;
            PandilleroAPie p = c.GetComponent<PandilleroAPie>();
            float antes = p.Actual;
            ResultadoDanio de = p.RecibirDanio(Golpe(p, 20f, Vector3.forward));
            Assert.AreEqual(TipoBloqueo.Normal, de.Bloqueo);
            Assert.AreEqual(3f, antes - p.Actual, 0.01f, "de frente entra el 15% de 20");
            antes = p.Actual;
            ResultadoDanio atras = p.RecibirDanio(Golpe(p, 20f, Vector3.back));
            Assert.AreEqual(TipoBloqueo.Ninguno, atras.Bloqueo);
            Assert.AreEqual(20f, antes - p.Actual, 0.01f, "por la espalda entra completo");
        }

        [UnityTest]
        public IEnumerator RomperElEscudo_LoAturdeYLoVuelveCastigable()
        {
            yield return null;
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 15f), MiraAlJugador);
            yield return null;
            PandilleroAPie p = c.GetComponent<PandilleroAPie>();
            int rotos = 0;
            c.Escudo.Roto += () => rotos++;
            p.RecibirDanio(Golpe(p, 100f, Vector3.forward));
            Assert.AreEqual(0, rotos);
            p.RecibirDanio(Golpe(p, 60f, Vector3.forward));
            Assert.AreEqual(1, rotos, "150 de daño bruto rompen el escudo");
            yield return null;
            Assert.IsFalse(c.Escudo.Activo);
            Assert.IsFalse(c.Escudo.Placa.gameObject.activeSelf, "la placa se esconde");
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Aturdido, c.Regla.Actual);
            Assert.IsTrue(p.Castigable, "aturdido queda castigable");
            float antes = p.Actual;
            p.RecibirDanio(Golpe(p, 10f, Vector3.forward));
            Assert.AreEqual(15f, antes - p.Actual, 0.01f, "sin escudo y castigable: 10 x1.5");
            yield return Esperar(1.4f);
            Assert.IsFalse(p.Castigable, "a los 1.2 s deja de estar aturdido");
            Assert.IsFalse(c.Escudo.Activo, "el escudo sigue caído");
            yield return Esperar(4f);
            Assert.IsTrue(c.Escudo.Activo, "a los 5 s el escudo vuelve completo");
            Assert.AreEqual(150f, c.Escudo.Regla.Aguante, 0.001f);
            Assert.IsTrue(c.Escudo.Placa.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SinProvocarlo_ElJugadorACincoMetros_NoRecibeDanioEnCincoSegundos()
        {
            yield return Esperar(0.3f);
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 3f), MiraAlJugador);
            float antes = vidaJugador.Actual;
            yield return Esperar(5f);
            Assert.AreEqual(antes, vidaJugador.Actual, 0.001f, "no ataca si no lo provocan");
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Guardia, c.Regla.Actual);
            Assert.IsFalse(c.Provocado);
        }

        [UnityTest]
        public IEnumerator Provocado_ElEmpujonConecta18A1ymedio_YElTerceroEsMazazoDe28()
        {
            yield return Esperar(0.3f);
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 1.5f), MiraAlJugador);
            yield return null;
            c.Provocar();
            List<float> danios = new List<float>();
            float ultima = vidaJugador.Actual;
            float fin = Time.time + 8f;
            while (danios.Count < 3 && Time.time < fin)
            {
                yield return null;
                float ahora = vidaJugador.Actual;
                if (ahora < ultima - 0.001f)
                {
                    danios.Add(ultima - ahora);
                }
                ultima = ahora;
            }
            Assert.AreEqual(3, danios.Count, "tres ataques conectaron");
            Assert.AreEqual(18f, danios[0], 0.01f, "empujón");
            Assert.AreEqual(18f, danios[1], 0.01f, "empujón");
            Assert.AreEqual(28f, danios[2], 0.01f, "mazazo");
        }

        [UnityTest]
        public IEnumerator Provocado_SiElJugadorEstaAMasDeTreintaMetros_VuelveAGuardia()
        {
            yield return null;
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 35f), MiraAlJugador);
            yield return null;
            c.Provocar();
            Assert.IsTrue(c.Provocado);
            yield return Esperar(0.3f);
            Assert.IsFalse(c.Provocado, "a más de 30 m deja de pelear");
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Guardia, c.Regla.Actual);
        }

        [UnityTest]
        public IEnumerator Giro_RodearloPorLaEspaldaLoDejaSinEscudoDeFrente()
        {
            yield return Esperar(0.3f);
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 6f), MiraAlJugador);
            yield return null;
            c.Provocar();
            yield return Esperar(0.5f);
            // Gira a 90 grados por segundo como máximo: en medio segundo no pasa de unos 45.
            Vector3 aJugador = jugador.transform.position - c.transform.position;
            aJugador.y = 0f;
            float angulo = Vector3.Angle(c.transform.forward, aJugador);
            Assert.Less(angulo, 5f, "ya lo mira de frente");
            // Se le da la vuelta de golpe: tarda en girar, no se le queda de frente al instante.
            jugador.GetComponent<CharacterController>().enabled = false;
            jugador.transform.position = new Vector3(6f, 0.05f, 6f);
            jugador.GetComponent<CharacterController>().enabled = true;
            Physics.SyncTransforms();
            yield return Esperar(0.3f);
            aJugador = jugador.transform.position - c.transform.position;
            aJugador.y = 0f;
            float despues = Vector3.Angle(c.transform.forward, aJugador);
            Assert.Greater(despues, 40f, "en 0.3 s a 90 grados por segundo todavía no lo alcanza (" + despues + ")");
        }

        [UnityTest]
        public IEnumerator Capturas_GuardiaBloqueandoConChispa()
        {
            if (Environment.GetEnvironmentVariable("T1109_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1109/ con T1109_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            yield return Esperar(0.3f);
            ConductaGuardiaPesado c = Crear(new Vector3(0f, 0.05f, 4f), MiraAlJugador);
            yield return Esperar(0.4f);
            PandilleroAPie p = c.GetComponent<PandilleroAPie>();
            GameObject go = new GameObject("CamaraFoto");
            extras.Add(go);
            foreach (Camera otra in Camera.allCameras) otra.enabled = false;
            Camera camara = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            camara.farClipPlane = 100f;
            go.transform.position = c.transform.position + new Vector3(-1.4f, 1.7f, -2.6f);
            go.transform.LookAt(c.transform.position + Vector3.up * 1.1f);
            p.RecibirDanio(Golpe(p, 8f, Vector3.forward));
            p.RecibirDanio(Golpe(p, 8f, Vector3.forward)); // un segundo golpe: la chispa se ve en este cuadro
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
            File.WriteAllBytes(CarpetaFotos + "/guardia-bloqueando.png", foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }
    }
}
