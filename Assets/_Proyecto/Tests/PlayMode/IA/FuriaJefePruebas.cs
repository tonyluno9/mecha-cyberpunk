using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// Pruebas jugables de T-156: arena, MechaJugador y FlujoPelea reales con el jefe de prueba (150 de vida, 12 de daño).
    /// El jugador queda quieto y su vida se repone en cada golpe para medir daño y espera sin que caiga en KO.
    /// </summary>
    public class FuriaJefePruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabJefe = "Assets/_Proyecto/IA/Pruebas/RivalJefeDePrueba.prefab";
        private const string RutaPrefabTutorial = "Assets/_Proyecto/IA/Prefabs/RivalTutorial.prefab";
        private const string RutaConfigFuria = "Assets/_Proyecto/IA/Datos/ConfigFuriaJefe_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-156-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private const float EscalaTiempo = 4f;
        private const float EsperaMaxima = 15f;
        private const float LimiteSegundos = 120f;
        private const float DanioNormal = 12f;
        private const float DanioBajarALaMitad = 80f;
        private const float DanioMortal = 100000f;
        private const float TolDanio = 0.1f;
        private const int GolpesMedidos = 7;
        private const float MejoraMinimaEspera = 0.3f;
        private const float MargenColor = 0.05f;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ConfigFlujoPelea configPrueba;
        private ConfigFuriaJefe configFuria;
        private VidaMecha vidaJugador;
        private DirectorPelea director;
        private float escalaOriginal;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            errores.Clear();
            escalaOriginal = Time.timeScale;
            configFuria = AssetDatabase.LoadAssetAtPath<ConfigFuriaJefe>(RutaConfigFuria);
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarError;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            if (configPrueba != null)
            {
                Object.Destroy(configPrueba);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private IEnumerator Escenario()
        {
            yield return Calentar();
            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            GameObject jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador), inicio.position, inicio.rotation);
            creados.Add(jugador);
            vidaJugador = jugador.GetComponent<VidaMecha>();

            configPrueba = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configPrueba, "segundosPorNumero", 0.3f);
            FijarFloat(configPrueba, "pausaDespuesKO", 0.3f);
            FijarFloat(configPrueba, "segundosTextoPelea", 0.3f);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlujo), padre.transform);
            creados.Add(flujo);
            creados.Add(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configPrueba;
            soDirector.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJefe);
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configPrueba;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            yield return null;
            yield return null;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        private static void Danar(IPeleador peleador, float cantidad)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(
                new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private FuriaJefe Furia() => ((Component)director.Rival).GetComponent<FuriaJefe>();

        // Junta los golpes que recibe el jugador (daño y momento) y le repone la vida en cada uno.
        private void EscucharGolpes(List<float> danios, List<float> momentos)
        {
            EventosPelea.ImpactoRecibido += (objetivo, info, r) =>
            {
                if (objetivo == director.Jugador && r.DanioAplicado > 0f)
                {
                    danios.Add(r.DanioAplicado);
                    momentos.Add(Time.time);
                    vidaJugador.Restaurar();
                }
            };
        }

        private IEnumerator HastaGolpes(List<float> danios, int cantidad)
        {
            float limite = Time.time + LimiteSegundos;
            while (danios.Count < cantidad && Time.time < limite)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(danios.Count, cantidad, "No llegaron los golpes esperados");
        }

        private static float EsperaMedia(List<float> momentos, int desde, int cantidad)
        {
            // cantidad golpes consecutivos desde el índice dan cantidad - 1 esperas.
            return (momentos[desde + cantidad - 1] - momentos[desde]) / (cantidad - 1);
        }

        [UnityTest]
        public IEnumerator Furia_BajaALaMitad_PegaMasYEsperaMenosYAvisaUnaVez()
        {
            yield return Escenario();
            Time.timeScale = EscalaTiempo;
            FuriaJefe furia = Furia();
            int avisos = 0;
            furia.FuriaIniciada += () => avisos++;
            List<float> danios = new List<float>();
            List<float> momentos = new List<float>();
            EscucharGolpes(danios, momentos);

            yield return HastaGolpes(danios, GolpesMedidos);
            Assert.IsFalse(furia.EnFuria, "Con la vida llena no hay furia");
            int antes = danios.Count;
            float esperaAntes = EsperaMedia(momentos, 0, antes);

            Danar(director.Rival, DanioBajarALaMitad);
            yield return null;
            yield return null;
            Assert.IsTrue(furia.EnFuria, "A la mitad entra en furia");
            // Se descartan los golpes que ya venían preparados antes del cambio.
            int inicioFuria = danios.Count + 1;
            yield return HastaGolpes(danios, inicioFuria + GolpesMedidos);
            Danar(director.Rival, 1f);
            yield return null;

            for (int i = 0; i < antes; i++)
            {
                Assert.AreEqual(DanioNormal, danios[i], TolDanio, "Antes de la furia pega normal");
            }
            for (int i = inicioFuria; i < inicioFuria + GolpesMedidos; i++)
            {
                Assert.AreEqual(DanioNormal * configFuria.MultiplicadorDanio, danios[i], TolDanio, "En furia pega 1.25 veces");
            }
            float esperaDespues = EsperaMedia(momentos, inicioFuria, GolpesMedidos);
            Assert.LessOrEqual(esperaDespues, esperaAntes * (1f - MejoraMinimaEspera),
                "La espera antes " + esperaAntes + " y en furia " + esperaDespues);
            Assert.AreEqual(1, avisos, "FuriaIniciada se avisa una vez");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Furia_EnFuria_ElCuerpoPulsaEnRojo()
        {
            yield return Escenario();
            FuriaJefe furia = Furia();
            PresentacionRival presentacion = furia.GetComponent<PresentacionRival>();
            Renderer cuerpo = presentacion.Cuerpo;
            Color colorBase = cuerpo.sharedMaterial.GetColor(IdColorBase);

            Danar(director.Rival, DanioBajarALaMitad);
            float maximoRojo = 0f;
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            float fin = Time.time + 2f;
            while (Time.time < fin)
            {
                yield return null;
                cuerpo.GetPropertyBlock(bloque);
                // El parpadeo de daño también pinta el cuerpo: solo cuenta el pulso de la furia.
                if (!presentacion.Parpadeando && !bloque.isEmpty)
                {
                    maximoRojo = Mathf.Max(maximoRojo, bloque.GetColor(IdColorBase).r);
                }
            }

            Assert.IsTrue(furia.EnFuria);
            Assert.Greater(maximoRojo, colorBase.r + MargenColor, "El cuerpo se pone rojo");
            Assert.Greater(maximoRojo, configFuria.ColorFuria.r - 0.2f);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Furia_Revancha_DevuelveMultiplicadoresYQuitaElColor()
        {
            yield return Escenario();
            FuriaJefe furia = Furia();
            RivalBasico rival = furia.GetComponent<RivalBasico>();
            Renderer cuerpo = furia.GetComponent<PresentacionRival>().Cuerpo;
            Danar(director.Rival, DanioBajarALaMitad);
            yield return null;
            yield return null;
            Assert.IsTrue(furia.EnFuria);
            Assert.AreEqual(configFuria.MultiplicadorDanio, rival.MultiplicadorDanio, 1e-4f);
            Assert.AreEqual(configFuria.MultiplicadorEspera, rival.MultiplicadorEspera, 1e-4f);
            Assert.AreEqual(configFuria.MultiplicadorVelocidad, rival.MultiplicadorVelocidad, 1e-4f);

            Danar(director.Rival, DanioMortal);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.IsTrue(furia.EnFuria, "Sigue en furia hasta la revancha");
            vidaJugador.Restaurar();
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            yield return null;

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.IsFalse(furia.EnFuria);
            Assert.AreEqual(1f, rival.MultiplicadorDanio);
            Assert.AreEqual(1f, rival.MultiplicadorEspera);
            Assert.AreEqual(1f, rival.MultiplicadorVelocidad);
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            cuerpo.GetPropertyBlock(bloque);
            Assert.IsTrue(bloque.isEmpty, "El color de furia se fue");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Furia_ConPiedadEnElMismoObjeto_RegistraErrorYQuedaApagada()
        {
            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            creados.Add(padre);
            GameObject objeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabTutorial), padre.transform);
            Assert.IsNotNull(objeto.GetComponent<PiedadRival>());
            FuriaJefe furia = objeto.AddComponent<FuriaJefe>();
            SerializedObject so = new SerializedObject(furia);
            so.FindProperty("config").objectReferenceValue = configFuria;
            so.ApplyModifiedPropertiesWithoutUndo();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("FuriaJefe y PiedadRival no van juntos"));
            padre.SetActive(true);
            yield return null;

            Assert.IsFalse(furia.enabled, "FuriaJefe se apaga");
            Assert.IsFalse(furia.EnFuria);
        }

        [UnityTest]
        public IEnumerator Captura_JefeEnFuria_CuerpoRojoVistoDesdeLaCabina()
        {
            yield return Escenario();
            FuriaJefe furia = Furia();
            PresentacionRival presentacion = furia.GetComponent<PresentacionRival>();
            Danar(director.Rival, DanioBajarALaMitad);
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            float limite = Time.time + 3f;
            bool rojo = false;
            while (!rojo && Time.time < limite)
            {
                yield return null;
                presentacion.Cuerpo.GetPropertyBlock(bloque);
                rojo = !presentacion.Parpadeando && !bloque.isEmpty && bloque.GetColor(IdColorBase).r > configFuria.ColorFuria.r * 0.9f;
            }
            Assert.IsTrue(rojo, "El cuerpo llegó al rojo");
            // WaitForEndOfFrame no corre en batchmode: se renderiza la cámara del jugador a una textura y se guarda.
            Camera camara = Camera.main;
            Assert.IsNotNull(camara, "Falta la cámara de la cabina");
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, 24);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = destino;
            camara.Render();
            camara.targetTexture = anterior;
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            Object.Destroy(destino);
            Assert.IsTrue(File.Exists(RutaCaptura), "Se guardó la captura");
        }
    }
}
