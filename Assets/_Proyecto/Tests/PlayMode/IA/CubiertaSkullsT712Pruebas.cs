using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-712: los SKULLS se agachan detrás de una cobertura (muro, caja, carro): al verse corren a ella, se agachan (estado AgachadoQuieto de Cobertura_Entrar),
    /// asoman (se paran y apuntan), disparan y se esconden otra vez. Con un muro bajo de por medio y el jugador real.
    /// </summary>
    public class CubiertaSkullsT712Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private GameObject muro;
        private Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // dispara pero no acierta: el jugador sigue vivo
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.DestroyImmediate(grupo);
            Object.DestroyImmediate(muro);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // Un muro bajo (1.4 m, no tapa a uno de pie) entre el jugador y el SKULL, y un SKULL solo del otro lado.
        private PandilleroAPie Armar(float alto, int cuantos = 1)
        {
            muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.name = "MuroBajo";
            muro.transform.position = new Vector3(0f, alto / 2f, 8f);
            muro.transform.localScale = new Vector3(3f, alto, 0.6f);
            grupo = new GameObject("GrupoSkullsPrueba");
            grupo.SetActive(false);
            grupo.transform.position = new Vector3(0f, 0f, 15f);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            GameObject primero = null;
            for (int i = 0; i < cuantos; i++)
            {
                GameObject p = Object.Instantiate(molde, grupo.transform);
                p.transform.position = new Vector3(i * 1.5f - (cuantos - 1) * 0.75f, 0.05f, 15f);
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
                primero = primero != null ? primero : p;
            }
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            grupo.SetActive(true);
            return primero.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Hasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.time + segundos;
            while (!condicion())
            {
                Assert.Less(Time.time, fin, "Tiempo agotado: " + que);
                yield return null;
            }
        }

        // Altura real de la cabeza (m sobre el suelo) con la animación puesta: bounds de la malla horneada (los bounds del modelo no valen animados).
        private static float AlturaCabeza(PandilleroAPie p)
        {
            float max = 0f;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in p.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.BakeMesh(malla, true);
                Matrix4x4 m = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
                foreach (Vector3 v in malla.vertices)
                {
                    max = Mathf.Max(max, m.MultiplyPoint3x4(v).y);
                }
            }
            Object.DestroyImmediate(malla);
            return max - p.transform.position.y;
        }

        private static string EstadoBase(PandilleroAPie p)
        {
            Animator a = p.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true).Animador;
            return a.GetCurrentAnimatorStateInfo(0).IsName("AgachadoQuieto") ? "AgachadoQuieto" : (a.GetCurrentAnimatorStateInfo(0).IsName("AgachadoCaminar") ? "AgachadoCaminar" : "otro");
        }

        [UnityTest]
        public IEnumerator Skull_CorreALaCobertura_SeAgacha_Asoma_Dispara_YSeEsconde()
        {
            PandilleroAPie skull = Armar(1.4f);
            PistolaPandillero pistola = skull.GetComponent<PistolaPandillero>();
            yield return Hasta(() => skull.Alerta && skull.TieneCubierta, "ve al jugador y elige la cobertura", 15f);
            Vector3 cubierta = skull.PuntoCubierta;
            Assert.Greater(cubierta.z, muro.transform.position.z, "La cobertura queda del lado del SKULL, el contrario al jugador");
            Assert.Less(Vector3.Distance(new Vector3(cubierta.x, 0f, cubierta.z), new Vector3(0f, 0f, muro.transform.position.z)), 4f, "Y pegada al muro");

            int ciclos = 0;
            bool agachadoVisto = false;
            bool parado = false;
            bool dispararAgachado = false;
            bool disparoAsomado = false;
            float alturaAgachado = 99f;
            bool capturadoAgachado = false;
            bool capturadoAsomado = false;
            ReglaTacticaPandillero.Modo anterior = skull.ModoTactico;
            int disparosPrevios = pistola.Disparos;
            int disparosAlAgacharse = -1;
            float limite = Time.time + 60f;
            while (Time.time < limite && ciclos < 2)
            {
                ReglaTacticaPandillero.Modo modo = skull.ModoTactico;
                if (skull.EstaAgachado)
                {
                    Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, modo, "Solo está agachado cubriéndose");
                    Assert.Less(Vector3.Distance(new Vector3(skull.transform.position.x, 0f, skull.transform.position.z), new Vector3(cubierta.x, 0f, cubierta.z)), 1.1f,
                        "Y agachado ya llegó a su puesto");
                    if (!agachadoVisto && EstadoBase(skull) != "otro")
                    {
                        agachadoVisto = true;
                    }
                    disparosAlAgacharse = disparosAlAgacharse < 0 ? pistola.Disparos : disparosAlAgacharse;
                    if (pistola.Disparos > disparosAlAgacharse)
                    {
                        dispararAgachado = true;
                    }
                    if (alturaAgachado > 50f && EstadoBase(skull) == "AgachadoQuieto")
                    {
                        alturaAgachado = AlturaCabeza(skull);
                    }
                    if (!capturadoAgachado && EstadoBase(skull) == "AgachadoQuieto" && Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
                    {
                        capturadoAgachado = true;
                        yield return Capturar(skull, "s1c-T-712-agachado.png");
                    }
                }
                if (!skull.EstaAgachado)
                {
                    disparosAlAgacharse = -1;
                }
                if (modo == ReglaTacticaPandillero.Modo.Asomado)
                {
                    parado |= !skull.EstaAgachado;
                    if (pistola.Disparos > disparosPrevios)
                    {
                        disparoAsomado = true;
                    }
                    if (!capturadoAsomado && skull.Estado == EstadoPandillero.Preparando && Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
                    {
                        capturadoAsomado = true;
                        Debug.Log("[T-712] altura de la cabeza asomado: " + AlturaCabeza(skull).ToString("F2") + " m");
                        yield return Capturar(skull, "s1c-T-712-asoma.png");
                    }
                }
                if (anterior == ReglaTacticaPandillero.Modo.Asomado && modo == ReglaTacticaPandillero.Modo.Cubrirse)
                {
                    ciclos++;
                }
                if (modo != ReglaTacticaPandillero.Modo.Asomado)
                {
                    disparosPrevios = pistola.Disparos;
                }
                anterior = modo;
                yield return null;
            }
            Assert.IsTrue(agachadoVisto, "Se agacha: el Animator juega AgachadoQuieto (Cobertura_Entrar)");
            Assert.LessOrEqual(alturaAgachado, muro.transform.localScale.y, "Agachado la cabeza queda tras el muro (" + alturaAgachado.ToString("F2") + " m)");
            Assert.IsTrue(parado, "Al asomarse se para");
            Assert.IsTrue(disparoAsomado, "Dispara asomado");
            Assert.IsFalse(dispararAgachado, "Agachado no dispara");
            Assert.GreaterOrEqual(ciclos, 2, "Asoma, dispara y se esconde, más de una vez");
        }

        [UnityTest]
        public IEnumerator UnMuroMasBajoQueLaCabeza_NoSirveDeCobertura_NoSeAgachaDetras()
        {
            PandilleroAPie skull = Armar(1.0f);
            yield return Hasta(() => skull.Alerta, "ve al jugador", 15f);
            bool agachado = false;
            float fin = Time.time + 8f;
            while (Time.time < fin)
            {
                Assert.IsFalse(skull.TieneCubierta, "Un muro de 1 m no lo tapa agachado: no lo elige");
                agachado |= skull.EstaAgachado;
                yield return null;
            }
            Assert.IsFalse(agachado, "Sin cobertura que lo tape no se agacha en campo abierto");
        }

        [UnityTest]
        public IEnumerator DosSkulls_ConLaMismaCobertura_NoSeEncimanAlAgacharse()
        {
            Armar(1.4f, 2);
            List<PandilleroAPie> dos = grupo.GetComponentsInChildren<PandilleroAPie>().ToList();
            PandilleroAPie primero = dos[0];
            PandilleroAPie segundo = dos[1];
            yield return Hasta(() => dos.All(s => s.TieneCubierta), "los dos eligen cobertura", 20f);
            Assert.GreaterOrEqual(Vector3.Distance(primero.PuntoCubierta, segundo.PuntoCubierta), PuestosDeCubierta.Separacion - 0.01f, "Cada uno tiene su puesto");
        }

        private IEnumerator Capturar(PandilleroAPie skull, string archivo)
        {
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            camara.farClipPlane = 200f;
            Vector3 punto = skull.transform.position + Vector3.up * 0.9f;
            objeto.transform.position = punto + new Vector3(3.6f, 0.5f, -1.2f);
            objeto.transform.LookAt(punto);
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
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(objeto);
        }
    }
}
