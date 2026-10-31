using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// Peatones sobre Barrio.prefab (T-331): las rutas de banqueta no cruzan ningún collider del barrio, la cuenta viva cambia con la franja
    /// (reloj de prueba en el registro de Core) y un peatón se detiene a 2 m del jugador (jugador de prueba en el registro). Instancia el prefab, no lo escribe.
    /// </summary>
    public class PeatonesBarrioPruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        private const string RutaPrefabPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        private const float MitadManzana = 40f;
        private const float AlturaBanqueta = 0.2f;
        private const float PasoMuestreo = 0.5f;
        private const float RadioCuerpo = 0.25f;
        private const float AlturaInferior = 0.4f;
        private const float AlturaSuperior = 1.7f;
        private const int FramesDeEspera = 3;
        private const float MargenParada = 0.3f;

        private sealed class RelojDoble : IRelojMundo
        {
            public bool Noche;
            public float Hora => Noche ? 22f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => Noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => Noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private RelojDoble reloj;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            reloj = new RelojDoble();
            Registro<IRelojMundo>.Agregar(reloj);
            config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            Assert.IsNotNull(config, "Falta ConfigPeatones_Trafico.asset");
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(barrio);
            Registros.LimpiarTodos();
        }

        private List<Vector3[]> RutasDeLasManzanas()
        {
            List<Vector3> centros = barrio.transform.Cast<Transform>().Where(t => t.name.StartsWith("Manzana_")).Select(t => t.localPosition).ToList();
            return RutasBanqueta.DeManzanas(centros, MitadManzana - config.MargenBanqueta, AlturaBanqueta, config.RecorteCierres);
        }

        private GrupoPeatones ColgarGrupo()
        {
            Transform colgado = barrio.transform.Find("Peatones");
            if (colgado != null)
            {
                return colgado.GetComponent<GrupoPeatones>();
            }
            GameObject objeto = new GameObject("Peatones");
            objeto.transform.SetParent(barrio.transform, false);
            GrupoPeatones grupo = objeto.AddComponent<GrupoPeatones>();
            grupo.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPeaton).GetComponent<PeatonMundo>(), RutasDeLasManzanas());
            return grupo;
        }

        [Test]
        public void Rutas_NoAtraviesanNingunCollider_DelBarrio()
        {
            List<Vector3[]> rutas = RutasDeLasManzanas();
            Assert.AreEqual(6, rutas.Count);
            List<string> problemas = new List<string>();
            foreach (Vector3[] ruta in rutas)
            {
                for (int i = 0; i < ruta.Length - 1; i++)
                {
                    float largo = Vector3.Distance(ruta[i], ruta[i + 1]);
                    for (float d = 0f; d <= largo; d += PasoMuestreo)
                    {
                        Vector3 punto = barrio.transform.TransformPoint(Vector3.Lerp(ruta[i], ruta[i + 1], d / largo));
                        Collider[] choques = Physics.OverlapCapsule(punto + Vector3.up * AlturaInferior, punto + Vector3.up * AlturaSuperior, RadioCuerpo);
                        foreach (Collider c in choques) { problemas.Add(c.name + "@" + punto.x.ToString("F0") + "," + punto.z.ToString("F0")); }
                    }
                }
            }
            Assert.IsEmpty(problemas.Distinct().ToArray(), "Puntos de ruta que chocan");
        }

        [Test]
        public void Barrio_TraeElGrupoDePeatones()
        {
            Transform grupo = barrio.transform.Find("Peatones");
            Assert.IsNotNull(grupo, "Barrio.prefab no tiene el grupo Peatones");
            Assert.IsNotNull(grupo.GetComponent<GrupoPeatones>());
        }

        [UnityTest]
        public IEnumerator EnSesentaSegundos_NingunPeaton_SaleDeLaBanqueta_NiAtraviesaEdificios()
        {
            reloj.Noche = true;
            GrupoPeatones grupo = ColgarGrupo();
            List<Vector3[]> rutas = RutasDeLasManzanas();
            List<string> problemas = new List<string>();
            int muestras = 0;
            float fin = Time.time + 60f;
            while (Time.time < fin)
            {
                yield return null;
                foreach (PeatonMundo p in grupo.Peatones)
                {
                    if (!p.gameObject.activeSelf) { continue; }
                    muestras++;
                    Vector3 local = barrio.transform.InverseTransformPoint(p.transform.position);
                    if (DistanciaARutas(local, rutas) > 0.05f) { problemas.Add(p.name + " fuera de ruta @" + local.x.ToString("F1") + "," + local.z.ToString("F1")); }
                    if (Mathf.Abs(local.y - AlturaBanqueta) > 0.05f) { problemas.Add(p.name + " fuera de la banqueta (y=" + local.y.ToString("F2") + ")"); }
                    Collider[] choques = Physics.OverlapCapsule(p.transform.position + Vector3.up * AlturaInferior, p.transform.position + Vector3.up * AlturaSuperior, RadioCuerpo);
                    foreach (Collider c in choques) { problemas.Add(p.name + " dentro de " + c.name); }
                }
            }
            Debug.Log("[T-331] muestras en 60 s: " + muestras + ", problemas distintos: " + problemas.Distinct().Count());
            Assert.Greater(muestras, 1000);
            Assert.IsEmpty(problemas.Distinct().Take(10).ToArray());
        }

        private static float DistanciaARutas(Vector3 punto, List<Vector3[]> rutas)
        {
            float mejor = float.MaxValue;
            foreach (Vector3[] ruta in rutas)
            {
                for (int i = 0; i < ruta.Length - 1; i++)
                {
                    Vector3 ab = ruta[i + 1] - ruta[i];
                    float t = Mathf.Clamp01(Vector3.Dot(punto - ruta[i], ab) / ab.sqrMagnitude);
                    mejor = Mathf.Min(mejor, Vector3.Distance(punto, ruta[i] + ab * t));
                }
            }
            return mejor;
        }

        [UnityTest]
        public IEnumerator ConTreintaPeatones_NingunCuadro_PasaDeCienMilisegundos()
        {
            reloj.Noche = true;
            GrupoPeatones grupo = ColgarGrupo();
            List<Vector3[]> rutas = RutasDeLasManzanas();
            PeatonMundo prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPeaton).GetComponent<PeatonMundo>();
            System.Random azar = new System.Random(7);
            int extra = Mathf.Max(0, 30 - config.Cantidad(true));
            for (int i = 0; i < extra; i++)
            {
                PeatonMundo p = Object.Instantiate(prefab, grupo.transform);
                p.Preparar(rutas[i % rutas.Count], config, new System.Random(azar.Next()), () => null, i);
            }
            yield return null;
            int activos = grupo.transform.Cast<Transform>().Count(t => t.gameObject.activeSelf);
            Assert.GreaterOrEqual(activos, 30);
            float peor = 0f;
            float fin = Time.realtimeSinceStartup + 5f;
            float anterior = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
                float ahora = Time.realtimeSinceStartup;
                peor = Mathf.Max(peor, ahora - anterior);
                anterior = ahora;
            }
            Debug.Log("[T-331] peor cuadro con " + activos + " peatones: " + (peor * 1000f).ToString("F1") + " ms");
            Assert.Less(peor, 0.1f);
        }

        [UnityTest]
        public IEnumerator Capturas_DelBarrioConPeatones()
        {
            reloj.Noche = false; // T-447: de día hay 14 peatones (de noche solo 6, pocos para las capturas)
            GrupoPeatones grupo = ColgarGrupo();
            GameObject luzObjeto = new GameObject("LuzCaptura");
            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 0.8f;
            luzObjeto.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject camObjeto = new GameObject("CamCaptura");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
            cam.farClipPlane = 300f;
            for (int i = 0; i < 2; i++)
            {
                yield return new WaitForSeconds(6f);
                PeatonMundo cercano = grupo.Peatones.Where(p => p.gameObject.activeSelf && !p.Detenido).ElementAt(i * 7);
                Vector3 donde = cercano.transform.position;
                // La primera posición con la vista libre de muros entre la cámara y el peatón.
                Vector3[] desplazamientos = { new Vector3(5f, 2.5f, -6f), new Vector3(-5f, 2.5f, 6f), new Vector3(6f, 2.5f, 5f), new Vector3(-6f, 2.5f, -5f) };
                Vector3 objetivo = donde + Vector3.up * 1f;
                camObjeto.transform.position = donde + desplazamientos[i];
                foreach (Vector3 d in desplazamientos)
                {
                    if (!Physics.Linecast(donde + d, objetivo))
                    {
                        camObjeto.transform.position = donde + d;
                        break;
                    }
                }
                camObjeto.transform.LookAt(objetivo);
                yield return null;
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D img = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                img.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs/capturas/T-331-" + (i + 1) + ".png")), img.EncodeToPNG());
                Object.Destroy(img);
                Object.Destroy(rt);
            }
            Object.Destroy(camObjeto);
            Object.Destroy(luzObjeto);
        }

        [UnityTest]
        public IEnumerator Cuenta_CambiaConLaFranja()
        {
            GrupoPeatones grupo = ColgarGrupo();
            for (int i = 0; i < FramesDeEspera; i++)
            {
                yield return null;
            }
            Assert.AreEqual(config.Cantidad(false), grupo.Vivos);
            reloj.Noche = true;
            yield return null;
            Assert.AreEqual(config.Cantidad(true), grupo.Vivos);
            Assert.Greater(config.Cantidad(false), config.Cantidad(true), "T-447: de día hay más gente que de noche");
        }

        [UnityTest]
        public IEnumerator Peaton_SeDetieneAlAcercarseElJugador_YSigueCuandoSeAleja()
        {
            reloj.Noche = true;
            GrupoPeatones grupo = ColgarGrupo();
            yield return null;
            yield return null;
            PeatonMundo peaton = grupo.Peatones[0];
            GameObject jugadorObjeto = new GameObject("JugadorDePrueba");
            JugadorDoble jugador = new JugadorDoble { Raiz = jugadorObjeto.transform };
            Registro<IJugador>.Agregar(jugador);
            jugadorObjeto.transform.position = peaton.transform.position + Vector3.right * (config.DistanciaParada - MargenParada);
            yield return null;
            yield return null;
            Vector3 quieto = peaton.transform.position;
            yield return null;
            Assert.IsTrue(peaton.Detenido);
            Assert.AreEqual(quieto, peaton.transform.position);
            jugadorObjeto.transform.position += Vector3.right * 20f;
            yield return null;
            Assert.IsFalse(peaton.Detenido);
            Object.Destroy(jugadorObjeto);
        }
    }
}
