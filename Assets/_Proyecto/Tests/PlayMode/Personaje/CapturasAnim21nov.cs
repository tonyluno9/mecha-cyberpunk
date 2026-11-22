using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// Capturas de las animaciones del 21-nov: cada escenario maneja al personaje con entrada simulada o llamadas al runtime y guarda una tira horizontal de 6 a 8 cuadros
    /// (640x360 cada uno, cámara propia en 3/4 a 3-5 m) en docs/capturas/_anim21nov/nn_escenario.png, con un .txt al lado con el tiempo de cada cuadro.
    /// Ninguna falla por la imagen: solo comprueban que el archivo exista. Correr con la categoría Capturas.
    /// </summary>
    [Category("Capturas")]
    public class CapturasAnim21nov : BaseJugadorAPie
    {
        private const string Carpeta = "docs/capturas/_anim21nov";
        private const int Ancho = 640;
        private const int Alto = 360;
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string RutaPrefabCarro = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab";
        private const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        private const string RutaE16 = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E16_RobotPesadoNova_a.prefab";

        private readonly List<GameObject> creados = new List<GameObject>();
        private VisualPersonaje visual;
        private PistolaAPie pistola;

        public override void Setup()
        {
            base.Setup();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            pistola = jugador.GetComponent<PistolaAPie>();
        }

        public override void TearDown()
        {
            foreach (GameObject g in creados)
            {
                if (g != null) { Object.Destroy(g); }
            }
            creados.Clear();
            foreach (ConductorRobado c in Object.FindObjectsByType<ConductorRobado>(FindObjectsSortMode.None))
            {
                Object.Destroy(c.gameObject);
            }
            CarroTrafico.Activos.Clear();
            base.TearDown();
        }

        // ---------- Tira de cuadros ----------

        private class Tira
        {
            public readonly List<Texture2D> Cuadros = new List<Texture2D>();
            public readonly List<string> Notas = new List<string>();
            public float Inicio = -1f;
            public float Distancia = 4f;
            public float Giro = 40f; // grados respecto del frente del personaje: 3/4 de frente

            public void Capturar(Transform foco, string nota)
            {
                if (Inicio < 0f) { Inicio = Time.time; }
                var go = new GameObject("CamAnim21nov");
                Camera c = go.AddComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
                c.fieldOfView = 45f;
                c.nearClipPlane = 0.05f;
                Vector3 centro = foco.position + Vector3.up * 1f;
                Vector3 dir = Quaternion.Euler(0f, Giro, 0f) * foco.forward;
                go.transform.position = centro + dir.normalized * Distancia + Vector3.up * 0.5f;
                go.transform.rotation = Quaternion.LookRotation(centro - go.transform.position, Vector3.up);
                var rt = new RenderTexture(Ancho, Alto, 24);
                c.targetTexture = rt;
                c.Render();
                RenderTexture.active = rt;
                var foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
                foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
                foto.Apply();
                RenderTexture.active = null;
                Cuadros.Add(foto);
                Notas.Add((Time.time - Inicio).ToString("F2") + " s " + nota);
                Object.Destroy(go);
                Object.Destroy(rt);
            }

            // Pega los cuadros en una sola imagen horizontal y escribe el .txt con los tiempos.
            public string Guardar(string nombre)
            {
                Directory.CreateDirectory(Carpeta);
                var grande = new Texture2D(Ancho * Mathf.Max(1, Cuadros.Count), Alto, TextureFormat.RGB24, false);
                for (int i = 0; i < Cuadros.Count; i++)
                {
                    grande.SetPixels(i * Ancho, 0, Ancho, Alto, Cuadros[i].GetPixels());
                    Object.Destroy(Cuadros[i]);
                }
                grande.Apply();
                string ruta = Carpeta + "/" + nombre + ".png";
                File.WriteAllBytes(ruta, grande.EncodeToPNG());
                Object.Destroy(grande);
                var sb = new StringBuilder();
                for (int i = 0; i < Notas.Count; i++) { sb.AppendLine("cuadro " + (i + 1) + ": " + Notas[i]); }
                File.WriteAllText(Carpeta + "/" + nombre + ".txt", sb.ToString());
                Cuadros.Clear();
                return ruta;
            }
        }

        // Toma "cuantos" cuadros separados "cada" segundos, apuntando al foco.
        private static IEnumerator Cuadros(Tira tira, Transform foco, int cuantos, float cada, string nota = "")
        {
            for (int i = 0; i < cuantos; i++)
            {
                tira.Capturar(foco, nota);
                yield return Esperar(cada);
            }
        }

        private static void Listo(Tira tira, string nombre)
        {
            string ruta = tira.Guardar(nombre);
            Assert.IsTrue(File.Exists(ruta), "No se guardó " + ruta);
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
        }

        private IEnumerator Correr()
        {
            Press(teclado.wKey);
            yield return null; // dos Press en el mismo cuadro: el segundo estado del teclado tapaba la W
            Press(teclado.leftShiftKey);
        }

        private void SoltarCarrera()
        {
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
        }

        private IEnumerator ConPistola()
        {
            yield return Calentar();
            yield return ElegirEnRueda(1);
            yield return Esperar(0.3f);
        }

        private IEnumerator ConGranada(int cuantas)
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Granada, 0), cuantas);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Granada));
            yield return Esperar(0.3f);
        }

        // ---------- 01 a 06: jugador a pie ----------

        [UnityTest]
        public IEnumerator Escenario01_GolpearYCorrer()
        {
            var tira = new Tira();
            yield return Calentar();
            yield return Esperar(0.3f);
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 3, 0.3f, "golpea");
            yield return Correr();
            yield return Cuadros(tira, jugador.transform, 5, 0.2f, "corre");
            SoltarCarrera();
            Listo(tira, "01_golpear_correr");
        }

        [UnityTest]
        public IEnumerator Escenario02_CorrerYGolpear()
        {
            var tira = new Tira();
            yield return Calentar();
            yield return Esperar(0.3f);
            yield return Correr();
            yield return Esperar(1f);
            yield return Cuadros(tira, jugador.transform, 2, 0.2f, "corre");
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 6, 0.15f, "golpea");
            SoltarCarrera();
            Listo(tira, "02_correr_golpear");
        }

        [UnityTest]
        public IEnumerator Escenario03_DispararYCorrer_Pistola()
        {
            var tira = new Tira();
            yield return ConPistola();
            Press(raton.rightButton);
            yield return Esperar(1f);
            yield return Cuadros(tira, jugador.transform, 1, 0.1f, "apunta");
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 2, 0.2f, "dispara");
            Release(raton.rightButton);
            yield return Correr();
            yield return Cuadros(tira, jugador.transform, 5, 0.25f, "corre");
            SoltarCarrera();
            Listo(tira, "03_disparar_correr_pistola");
        }

        [UnityTest]
        public IEnumerator Escenario04_CorrerYDispararSinArma_SacarApuntarTiro()
        {
            var tira = new Tira();
            yield return ConPistola();
            pistola.SacarAntesDeDisparar = true;
            yield return Correr();
            yield return Esperar(0.8f);
            SoltarCarrera();
            yield return Esperar(0.3f);
            tira.Capturar(jugador.transform, "antes del clic");
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 7, 0.17f, "saca, apunta, tira");
            Listo(tira, "04_correr_disparar_sin_arma");
        }

        [UnityTest]
        public IEnumerator Escenario05_RecargarYCorrer()
        {
            var tira = new Tira();
            yield return ConPistola();
            Press(raton.rightButton);
            yield return Esperar(1f);
            yield return Clic();
            yield return Esperar(0.2f);
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Cuadros(tira, jugador.transform, 3, 0.2f, "recarga");
            Release(raton.rightButton);
            yield return Correr();
            yield return Cuadros(tira, jugador.transform, 5, 0.2f, "corre");
            SoltarCarrera();
            Listo(tira, "05_recargar_correr");
        }

        [UnityTest]
        public IEnumerator Escenario06_GranadaDePieYAgachado()
        {
            var tira = new Tira();
            yield return ConGranada(3);
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 4, 0.4f, "granada de pie");
            yield return Esperar(1f);
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(0.8f);
            yield return Clic();
            yield return Cuadros(tira, jugador.transform, 4, 0.4f, "granada agachado");
            Listo(tira, "06_granada_de_pie_y_agachado");
        }

        // ---------- 07 y 08: explosión ----------

        [UnityTest]
        public IEnumerator Escenario07_MorirPorExplosion()
        {
            var tira = new Tira();
            yield return Calentar();
            yield return Esperar(0.3f);
            tira.Capturar(jugador.transform, "de pie");
            visual.RecibirExplosion(-jugador.transform.forward, 100f, 100f, true);
            visual.Caido = true;
            yield return Cuadros(tira, jugador.transform, 7, 0.25f, "muere");
            Listo(tira, "07_morir_por_explosion");
            visual.Caido = false;
        }

        [UnityTest]
        public IEnumerator Escenario08_DerriboPorExplosionYLevantarse()
        {
            var tira = new Tira();
            yield return Calentar();
            yield return Esperar(0.3f);
            tira.Capturar(jugador.transform, "de pie");
            visual.RecibirExplosion(-jugador.transform.forward, 40f, 100f, false);
            yield return Cuadros(tira, jugador.transform, 7, 0.6f, "derribo y levantarse");
            Listo(tira, "08_derribo_por_explosion");
        }

        // ---------- 09 a 11: carro, escalera y barda ----------

        [UnityTest]
        public IEnumerator Escenario09_RobarElCarro()
        {
            var tira = new Tira { Giro = 90f, Distancia = 5f };
            yield return Calentar();
            ConfigTraficoCalle configTrafico = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico);
            var centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCarro).GetComponent<CarroTrafico>();
            CarroTrafico carro = Object.Instantiate(molde);
            creados.Add(carro.gameObject);
            carro.Preparar(configTrafico, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(3), () => jugador.transform.position);
            yield return Esperar(0.3f);
            carro.AlChocar(5f, carro.transform.position);
            yield return Esperar(0.5f);
            IMontable montable = carro.GetComponent<IMontable>();
            Assert.IsTrue(montable.PuedeSubir);
            jugador.transform.position = montable.PuntoSubida - carro.transform.right * 0.3f;
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(jugadorAPie.Subir(montable));
            // La cámara de lado mira al jugador junto a la puerta; 8 cuadros en los ~7 s del robo.
            yield return Cuadros(tira, visual.transform, 8, 0.9f, "robo");
            Listo(tira, "09_robo_del_carro");
        }

        [UnityTest]
        public IEnumerator Escenario10_SubirEscalera()
        {
            Assert.Ignore("Sin montaje posible: las escaleras de mano (AnimacionEscalera) solo existen en la escena Ciudad_Prologo (EscalerasTechosT1101Pruebas); no se arman en una prueba suelta.");
            yield break;
        }

        [UnityTest]
        public IEnumerator Escenario11_SaltarBarda()
        {
            var tira = new Tira { Giro = 90f, Distancia = 4f };
            yield return Calentar();
            Assert.IsNotNull(ConfigAnimacionParkour.Actual, "Falta ConfigAnimacionParkour");
            GameObject barda = Caja(new Vector3(0f, 0.5f, 3f), new Vector3(4f, 1f, 0.2f));
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = new Vector3(0f, 0.05f, 2.4f);
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
            yield return Esperar(0.5f);
            tira.Capturar(jugador.transform, "frente a la barda");
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Cuadros(tira, jugador.transform, 7, 0.2f, "salta");
            Listo(tira, "11_saltar_barda");
        }

        // ---------- 12 y 13: NPC y robot ----------

        [UnityTest]
        public IEnumerator Escenario12_NpcPlaticaYHuye()
        {
            var tira = new Tira { Distancia = 3.5f };
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPeaton);
            Assert.IsNotNull(prefab, RutaPeaton);
            GameObject npc = Object.Instantiate(prefab, new Vector3(3f, 0.05f, 3f), Quaternion.identity);
            creados.Add(npc);
            foreach (Animator a in npc.GetComponentsInChildren<Animator>(true)) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            npc.SendMessage("AlPlaticarCivil", 0, SendMessageOptions.DontRequireReceiver);
            yield return Esperar(0.8f);
            yield return Cuadros(tira, npc.transform, 2, 0.3f, "platica");
            npc.SendMessage("AlAsustarCivil", SendMessageOptions.DontRequireReceiver);
            yield return Cuadros(tira, npc.transform, 6, 0.12f, "se asusta (mezcla de 0.2 s)");
            Listo(tira, "12_npc_huyendo");
        }

        [UnityTest]
        public IEnumerator Escenario13_E16CaeDelCieloYMuere()
        {
            var tira = new Tira { Distancia = 5f };
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaE16);
            Assert.IsNotNull(prefab, RutaE16);
            GameObject e16 = Object.Instantiate(prefab, new Vector3(4f, 0.05f, 4f), Quaternion.identity);
            creados.Add(e16);
            foreach (Animator a in e16.GetComponentsInChildren<Animator>(true)) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            yield return null;
            CaidaNova caida = CaidaNova.Iniciar(e16, false);
            Assert.IsNotNull(caida, "Falta ConfigClipsNovaNpc (clips de caída)");
            yield return Cuadros(tira, e16.transform, 2, 0.3f, "cae");
            caida.Aterrizar();
            yield return Cuadros(tira, e16.transform, 2, 0.4f, "aterriza");
            VisualPersonaje v = e16.GetComponentInChildren<VisualPersonaje>(true);
            if (v != null) { v.Caido = true; }
            yield return Cuadros(tira, e16.transform, 3, 0.5f, "muere");
            Listo(tira, "13_e16_cae_y_muere");
        }

        // ---------- 14: golpes del jugador ----------

        [UnityTest]
        public IEnumerator Escenario14_GolpesDerIzqYProtesis()
        {
            var tira = new Tira();
            yield return Calentar();
            yield return Esperar(0.3f);
            for (int i = 0; i < 3; i++)
            {
                yield return Clic();
                yield return Cuadros(tira, jugador.transform, 2, 0.25f, "golpe " + (i + 1));
                yield return Esperar(0.15f);
            }
            Listo(tira, "14_golpes_der_izq_protesis");
        }
    }
}
