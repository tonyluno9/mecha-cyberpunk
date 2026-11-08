using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Efectos;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using BarrilExplosivo = MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo;
using EfectosExplosion = MechaCyberpunk.Vehiculos.Efectos.EfectosExplosion;
using InstaladorBarriles = MechaCyberpunk.Vehiculos.Efectos.InstaladorBarriles;
using InstanciaExplosion = MechaCyberpunk.Vehiculos.Efectos.InstanciaExplosion;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-745: combate y efectos con entrada simulada. Disparar al dron lo derriba y baja la búsqueda; un barril baleado explota y daña; el trazo de la bala
    /// termina en el impacto; las explosiones usan partículas del Particle Pack con materiales URP. Capturas con CAPTURAR_S1=1 (docs/capturas/s1e-T-745-*.png).
    /// </summary>
    public class EfectosCombateT745Pruebas : BaseJugadorAPie
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public float Enfriado;
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;

            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes)
                {
                    NivelCambio?.Invoke(Nivel);
                }
            }

            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) { Enfriado += cantidad; Poner(Valor - cantidad); }
            public void Borrar() => Poner(0f);
        }

        private sealed class BlancoDoble : MonoBehaviour, IRecibeDanio
        {
            public float DanioTotal;
            public TipoDanio UltimoTipo;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                DanioTotal += info.Cantidad;
                UltimoTipo = info.Tipo;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private readonly List<GameObject> propios = new List<GameObject>();
        private GameObject origenFotos;

        public override void Setup()
        {
            base.Setup();
            origenFotos = new GameObject("OrigenFotos");
            propios.Add(origenFotos);
            GameObject sol = new GameObject("SolPrueba");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.2f;
            sol.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            propios.Add(sol);
        }

        public override void TearDown()
        {
            foreach (GameObject g in propios)
            {
                if (g != null)
                {
                    Object.Destroy(g);
                }
            }
            propios.Clear();
            foreach (GameObject raiz in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (raiz != null && (raiz.name == "EfectoExplosion" || raiz.name == "LlamasSuelo" || raiz.name == "ExplosionCarro" || raiz.name == "TrazadorBala"))
                {
                    Object.Destroy(raiz);
                }
            }
            base.TearDown();
        }

        private T Propio<T>(T objeto) where T : Object
        {
            if (objeto is GameObject g)
            {
                propios.Add(g);
            }
            return objeto;
        }

        private IEnumerator EquiparPistolaYApuntar()
        {
            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            float limite = Time.time + 3f;
            while (jugador.GetComponent<ArmasAPie>().ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(ArmaAPie.Pistola, jugador.GetComponent<ArmasAPie>().ArmaActiva, "La pistola quedó en la mano");
            Press(raton.rightButton);
            yield return Esperar(0.9f);
        }

        private IEnumerator Disparar()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.25f);
        }

        // El punto del mundo que mira la cámara del jugador, a esa distancia.
        private Vector3 PuntoDeMira(float metros) => controlador.Camara.position + controlador.Camara.forward * metros;

        private void Foto(string nombre, Vector3 camara, Vector3 mira, float fov = 45f)
        {
            origenFotos.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            CapturaS1.Foto(nombre, origenFotos.transform, camara, mira, fov, 1280, 720);
        }

        private static void AfirmarUrp(GameObject raiz, string quien)
        {
            ParticleSystemRenderer[] renders = raiz.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Assert.Greater(renders.Length, 0, quien + " tiene partículas");
            foreach (ParticleSystemRenderer r in renders)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null)
                    {
                        continue;
                    }
                    Assert.IsTrue(m.shader != null && (m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name.StartsWith("Shader Graphs/")),
                        quien + "/" + r.name + ": " + m.name + " usa " + m.shader.name + " (no es URP: saldría rosa)");
                    Assert.AreNotEqual("Hidden/InternalErrorShader", m.shader.name);
                }
            }
        }

        private static int ParticulasVivas(GameObject raiz)
        {
            int total = 0;
            foreach (ParticleSystem p in raiz.GetComponentsInChildren<ParticleSystem>(true))
            {
                total += p.particleCount;
            }
            return total;
        }

        // ---------- dron ----------

        [UnityTest]
        public IEnumerator DispararAlDron_LoDerribaExplotaEnChicoYBajaLaBusqueda()
        {
            yield return Calentar();
            Registros.LimpiarTodos();
            ConfigPersecucion config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            CalorDoble calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            GestorPersecucion gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
            Propio(objeto);
            calor.Sumar(30f, MotivoCalor.Mision);
            float limite = Time.time + 4f;
            while (gestor.Drones == 0 && Time.time < limite)
            {
                yield return null;
            }
            Assert.Greater(gestor.Drones, 0, "El gestor mandó un dron");
            DronMundo dron = gestor.GetComponentInChildren<DronMundo>();
            dron.enabled = false; // quieto en la mira mientras se le dispara
            yield return EquiparPistolaYApuntar();
            dron.transform.position = PuntoDeMira(8f);
            Physics.SyncTransforms();
            Vector3 donde = dron.transform.position;
            Foto("s1e-T-745-dron-antes", donde + new Vector3(3f, 0.5f, -5f), donde, 40f);

            int tiros = 0;
            while (!dron.EstaCayendo && tiros < 10)
            {
                dron.transform.position = PuntoDeMira(8f);
                Physics.SyncTransforms();
                yield return Disparar();
                tiros++;
            }
            Assert.IsTrue(dron.EstaCayendo, "El dron cae tras " + tiros + " balazos (vida " + dron.Vida + ")");
            Assert.AreEqual(config.CalorAlDerribarDron, calor.Enfriado, 0.01f, "Al derribarlo baja la búsqueda");
            dron.enabled = true;
            yield return Esperar(0.1f);
            if (dron != null)
            {
                Foto("s1e-T-745-dron-cayendo", dron.transform.position + new Vector3(3f, 1f, -5f), dron.transform.position, 40f);
            }
            InstanciaExplosion antesDeExplotar = EfectosExplosion.Ultima;
            InstanciaExplosion explosion = null;
            limite = Time.time + 5f;
            while (explosion == null && Time.time < limite)
            {
                if (EfectosExplosion.Ultima != antesDeExplotar)
                {
                    explosion = EfectosExplosion.Ultima;
                }
                yield return null;
            }
            Assert.IsNotNull(explosion, "Al estrellarse explota");
            Vector3 centro = explosion.Raiz.transform.position;
            yield return Esperar(0.35f);
            Foto("s1e-T-745-dron-explota", centro + new Vector3(4f, 2f, -6f), centro + Vector3.up * 0.8f, 45f);
            Assert.Greater(ParticulasVivas(explosion.Raiz), 0, "La explosión del dron tiene partículas del pack");
            AfirmarUrp(explosion.Raiz, "ExplosionDron");
            Assert.IsNotNull(explosion.Fuego);
            Assert.Less(centro.y, 1.5f, "Explotó al llegar al suelo");
            Object.Destroy(config);
        }

        // ---------- barril ----------

        private GameObject CrearBarril(Vector3 centro)
        {
            GameObject barril = Propio(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
            barril.name = "Barril_T745";
            barril.transform.position = centro;
            barril.transform.localScale = new Vector3(0.7f, 0.55f, 0.7f);
            return barril;
        }

        [UnityTest]
        public IEnumerator DispararUnBarril_ExplotaYHaceDanioYEmpuja()
        {
            yield return Calentar();
            yield return EquiparPistolaYApuntar();
            Vector3 mira = PuntoDeMira(9f);
            GameObject barril = CrearBarril(mira);
            GameObject cerca = Propio(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            cerca.transform.position = mira + new Vector3(2f, 0f, 0.5f);
            BlancoDoble victimaCerca = cerca.AddComponent<BlancoDoble>();
            GameObject lejos = Propio(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            lejos.transform.position = mira + new Vector3(25f, 0f, 0f);
            BlancoDoble victimaLejos = lejos.AddComponent<BlancoDoble>();
            GameObject caja = Propio(GameObject.CreatePrimitive(PrimitiveType.Cube));
            caja.transform.position = mira + new Vector3(-2f, 0f, 0.4f);
            Rigidbody cuerpo = caja.AddComponent<Rigidbody>();
            cuerpo.useGravity = false;
            Assert.GreaterOrEqual(InstaladorBarriles.Instalar(SceneManager.GetActiveScene()), 1, "El barril se marcó como explosivo");
            BarrilExplosivo comp = barril.GetComponent<BarrilExplosivo>();
            Assert.IsNotNull(comp);
            Physics.SyncTransforms();
            Vector3 cajaAntes = caja.transform.position;

            int tiros = 0;
            while (!comp.Exploto && tiros < 6)
            {
                yield return Disparar();
                tiros++;
            }
            Assert.IsTrue(comp.Exploto, "El barril explota al dispararle (" + tiros + " tiros)");
            Assert.IsNotNull(comp.Explosion);
            yield return Esperar(0.3f);
            Foto("s1e-T-745-barril-explota", mira + new Vector3(5f, 2.5f, -7f), mira, 50f);
            Assert.Greater(victimaCerca.DanioTotal, 5f, "El daño por área alcanza al de al lado");
            Assert.AreEqual(TipoDanio.Explosion, victimaCerca.UltimoTipo, "Es daño de explosión (el jugador a pie se prende con él)");
            Assert.AreEqual(0f, victimaLejos.DanioTotal, 0.001f, "El lejano no se entera");
            Assert.Less(cuerpo.transform.position.x, cajaAntes.x - 0.2f, "La onda empuja la caja");
            Assert.IsFalse(barril.GetComponent<Collider>().enabled, "El barril desaparece");
            Assert.Greater(ParticulasVivas(comp.Explosion.Raiz), 0);
            AfirmarUrp(comp.Explosion.Raiz, "ExplosionBarril");
            GameObject llamas = GameObject.Find("LlamasSuelo");
            Assert.IsNotNull(llamas, "Deja llamas en el suelo");
            AfirmarUrp(llamas, "LlamasBarril");
            Propio(llamas);
        }

        [UnityTest]
        public IEnumerator ExplosionDeBarril_EncadenaAlBarrilVecino()
        {
            yield return Calentar();
            GameObject uno = CrearBarril(new Vector3(0f, 0.5f, 10f));
            GameObject dos = CrearBarril(new Vector3(1.5f, 0.5f, 10f));
            InstaladorBarriles.Instalar(SceneManager.GetActiveScene());
            Physics.SyncTransforms();
            uno.GetComponent<BarrilExplosivo>().RecibirDanio(new InfoDanio(100f, TipoDanio.Disparo, uno.transform.position, Vector3.forward, null));
            Assert.IsTrue(dos.GetComponent<BarrilExplosivo>().Exploto, "El vecino cercano revienta en cadena");
            yield return null;
        }

        // ---------- trazo ----------

        [UnityTest]
        public IEnumerator Trazo_TerminaDondePegaLaBala_YNoQuedaFlotando()
        {
            yield return Calentar();
            yield return EquiparPistolaYApuntar();
            Vector3 mira = PuntoDeMira(15f);
            GameObject pared = Propio(GameObject.CreatePrimitive(PrimitiveType.Cube));
            pared.transform.position = new Vector3(mira.x, 2f, mira.z);
            pared.transform.localScale = new Vector3(14f, 6f, 0.5f);
            pared.transform.rotation = Quaternion.Euler(0f, controlador.Camara.eulerAngles.y, 0f);
            Physics.SyncTransforms();
            int antes = TrazadorBala.Creados;

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float limite = Time.time + 1f;
            while (TrazadorBala.Creados == antes && Time.time < limite)
            {
                yield return null;
            }
            Assert.Greater(TrazadorBala.Creados, antes, "El disparo deja un trazo");
            Assert.IsTrue(TrazadorBala.UltimoPego, "El trazo termina en un impacto");
            Vector3 destino = TrazadorBala.UltimoDestino;
            Vector3 cercaDePared = pared.GetComponent<Collider>().ClosestPoint(destino);
            Assert.Less(Vector3.Distance(destino, cercaDePared), 0.05f, "El destino del trazo está sobre la pared");
            LineRenderer linea = TrazadorBala.UltimaLinea;
            float tope = Time.time + 0.5f;
            bool llego = false;
            while (Time.time < tope && TrazadorBala.Activos > 0)
            {
                Assert.LessOrEqual(Vector3.Distance(linea.GetPosition(1), destino), Vector3.Distance(linea.GetPosition(0), destino) + 0.01f);
                if (!llego && Vector3.Distance(linea.GetPosition(1), destino) < 0.05f)
                {
                    llego = true;
                    Vector3 dir = (destino - linea.GetPosition(0)).normalized;
                    Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized;
                    Foto("s1e-T-745-trazo-pared", destino - dir * 6f + lado * 5f + Vector3.up * 0.6f, destino - dir * 2.5f, 45f);
                }
                yield return null;
            }
            Assert.IsTrue(llego, "La cabeza del trazo llegó al punto de impacto");
            Assert.AreEqual(0, TrazadorBala.Activos, "Pasado un instante no queda ningún trazo flotando");
            Assert.IsFalse(linea.enabled);
        }

        [UnityTest]
        public IEnumerator Trazo_SinImpacto_SeDesvaneceAlAlcanceMaximo()
        {
            yield return Calentar();
            yield return EquiparPistolaYApuntar();
            int antes = TrazadorBala.Creados;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float limite = Time.time + 1f;
            while (TrazadorBala.Creados == antes && Time.time < limite)
            {
                yield return null;
            }
            Assert.Greater(TrazadorBala.Creados, antes);
            Assert.IsFalse(TrazadorBala.UltimoPego, "No pegó en nada");
            LineRenderer linea = TrazadorBala.UltimaLinea;
            float inicio = linea.startColor.a;
            Assert.Greater(inicio, 0.9f);
            float minimo = inicio;
            float tope = Time.time + 1f;
            while (Time.time < tope && TrazadorBala.Activos > 0)
            {
                minimo = Mathf.Min(minimo, linea.startColor.a);
                yield return null;
            }
            Assert.Less(minimo, 0.6f, "Se va desvaneciendo");
            Assert.AreEqual(0, TrazadorBala.Activos, "Se acaba solo");
        }

        // ---------- explosiones del pack ----------

        [UnityTest]
        public IEnumerator Explosion_InstanciaParticulasDelPack_ConMaterialesUrp([Values(2.5f, 5f, 9f)] float radio)
        {
            yield return Calentar();
            Assert.IsTrue(EfectosExplosion.Disponible, "Hay catálogo de efectos");
            InstanciaExplosion e = EfectosExplosion.Explosion(new Vector3(0f, 0.2f, 6f), radio);
            yield return Esperar(0.4f);
            Assert.IsNotNull(e.Fuego);
            Assert.IsNotNull(e.Humo, "humo");
            Assert.IsNotNull(e.Chispas, "chispas");
            Assert.IsNotNull(e.Chamusco, "mancha en el suelo");
            Assert.Greater(e.Fuego.particleCount + e.Humo.particleCount, 0, "hay partículas vivas");
            AfirmarUrp(e.Raiz, "Explosion r=" + radio);
            Assert.IsTrue(e.Luz.enabled || e.Luz.intensity < 14f);
            ParticleSystemRenderer[] renders = e.Raiz.GetComponentsInChildren<ParticleSystemRenderer>(true);
            bool delPack = false;
            foreach (ParticleSystemRenderer r in renders)
            {
                string ruta = r.sharedMaterial != null ? AssetDatabase.GetAssetPath(r.sharedMaterial) : "";
                delPack |= ruta.Contains("UnityTechnologies/ParticlePack");
            }
            Assert.IsTrue(delPack, "Usa materiales del Unity Particle Pack");
        }

        [UnityTest]
        public IEnumerator CarroQueReviente_UsaHumoYFuegoDelPack_YSaleLaExplosion()
        {
            yield return Calentar();
            GameObject carro = Propio(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0.3f, 12f), Quaternion.Euler(0f, 200f, 0f)));
            DanioCarro danio = carro.GetComponent<DanioCarro>();
            yield return Esperar(0.4f);
            Assert.IsNotNull(danio.Humo, "el humo del carro es un prefab del pack");
            Assert.IsNotNull(danio.Fuego);
            AfirmarUrp(danio.Humo.gameObject, "HumoCarro");
            AfirmarUrp(danio.Fuego.gameObject, "FuegoCarro");
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 0.6f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
            yield return Esperar(1.2f);
            Assert.Greater(danio.Humo.particleCount, 0, "el carro averiado echa humo");
            Foto("s1e-T-745-carro-humo", new Vector3(5f, 2.5f, 5f), carro.transform.position + Vector3.up, 50f);
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
            yield return Esperar(0.5f);
            Assert.IsNotNull(danio.UltimaExplosion);
            AfirmarUrp(danio.UltimaExplosion.Efectos.Raiz, "ExplosionCarro");
            Assert.Greater(ParticulasVivas(danio.UltimaExplosion.Efectos.Raiz), 0);
            Foto("s1e-T-745-carro-explosion-humo", new Vector3(7f, 3f, 4f), carro.transform.position + Vector3.up * 1.2f, 55f);
            yield return Esperar(1.5f);
            Foto("s1e-T-745-carro-humo-despues", new Vector3(7f, 3f, 4f), carro.transform.position + Vector3.up * 2f, 55f);
        }
    }
}
