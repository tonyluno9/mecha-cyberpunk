using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-960: hojas de capturas de los clips PJ01_* de Blender sobre Visual_PJ01 (con o sin los implantes IMP01 e IMP02 de nivel 1), de frente y de lado, para compararlas con
    /// las vistas aprobadas de imageassets/blender/_PENDIENTE_BLENDER/_REVISION/*_vista_*.png. Escribe docs/capturas/pj01-&lt;hoja&gt;-frente.png y -lado.png y deja en el log,
    /// por pose, el punto más bajo y el más alto de la malla (para ver si atraviesa el piso o flota).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CapturasClipsPJ01.&lt;Hoja&gt;
    /// </summary>
    public static class CapturasClipsPJ01
    {
        public struct Pose
        {
            public string Clip;
            public float Fraccion;
            public bool Implantes;

            public Pose(string clip, float fraccion, bool implantes = false)
            {
                Clip = clip;
                Fraccion = fraccion;
                Implantes = implantes;
            }
        }

        private const float Separacion = 1.4f;
        private const string Prefab = "PJ01";

        public static void Locomocion()
        {
            Hoja("locomocion", new[]
            {
                new Pose("PJ01_Loco_Idle", 0.3f), new Pose("PJ01_Loco_Idle_ConImplante", 0.3f, true),
                new Pose("PJ01_Loco_CaminarAtras", 0.25f), new Pose("PJ01_Loco_CaminarAtras_ConImplante", 0.25f, true),
                new Pose("PJ01_Loco_CaminarDer", 0.25f), new Pose("PJ01_Loco_CaminarIzq_ConImplante", 0.25f, true),
            });
            Hoja("giros-frenar", new[]
            {
                new Pose("PJ01_Loco_GirarDer", 0.5f), new Pose("PJ01_Loco_GirarIzq_ConImplante", 0.5f, true),
                new Pose("PJ01_Loco_Frenar", 0.3f), new Pose("PJ01_Loco_Frenar_ConImplante", 0.3f, true),
                new Pose("PJ01_Loco_CaminarIzq", 0.25f), new Pose("PJ01_Loco_CaminarDer_ConImplante", 0.25f, true),
            });
        }

        /// <summary>Clips de T-749 (ya revisados en el juego) junto a los nuevos: si la escala de importación de los nuevos no cuadra, la altura no coincide.</summary>
        public static void Escala()
        {
            Hoja("escala", new[]
            {
                new Pose("PJ01_Pistola_Idle", 0.3f), new Pose("PJ01_Caminar_SinImplante", 0f), new Pose("PJ01_Loco_AgachadoIdle", 0.3f),
                new Pose("PJ01_Loco_Idle", 0.3f), new Pose("PJ01_Loco_CaminarAtras", 0f), new Pose("PJ01_Loco_Frenar", 1f),
            });
        }

        public static void Salto()
        {
            Hoja("salto", new[]
            {
                new Pose("PJ01_Loco_SaltoInicio", 0.5f), new Pose("PJ01_Loco_SaltoInicio", 1f), new Pose("PJ01_Loco_SaltoAire", 0.4f),
                new Pose("PJ01_Loco_SaltoAire", 1f), new Pose("PJ01_Loco_SaltoAterrizaje", 0.2f), new Pose("PJ01_Loco_Caida", 0.4f),
            });
        }

        public static void GolpesMuertes()
        {
            Hoja("golpes", new[]
            {
                new Pose("PJ01_Punos_Guardia", 0.3f), new Pose("PJ01_Punos_GolpeIzq", 0.4f), new Pose("PJ01_Punos_GolpeDer", 0.4f),
                new Pose("PJ01_Punos_Fuerte", 0.5f), new Pose("PJ01_Dano_Levantarse_BocaArriba", 0.5f), new Pose("PJ01_Dano_Levantarse_BocaAbajo", 0.5f),
            });
            Hoja("muertes", new[]
            {
                new Pose("PJ01_Muerte_Adelante", 0.5f), new Pose("PJ01_Muerte_Adelante", 1f), new Pose("PJ01_Muerte_Atras", 0.5f),
                new Pose("PJ01_Muerte_Atras", 1f), new Pose("PJ01_Muerte_Desmayo", 0.5f), new Pose("PJ01_Muerte_Desmayo", 1f),
            });
        }

        public static void RifleInteracciones()
        {
            Hoja("rifle", new[]
            {
                new Pose("PJ01_Rifle_Idle", 0.3f), new Pose("PJ01_Rifle_Apuntar", 0.3f), new Pose("PJ01_Rifle_Caminar", 0.25f),
                new Pose("PJ01_Rifle_ApuntarCaminar", 0.25f), new Pose("PJ01_Rifle_DisparoCaminar", 0.2f), new Pose("PJ01_Rifle_Recargar", 0.5f),
            });
            Hoja("cobertura-inter", new[]
            {
                new Pose("PJ01_Cobertura_Entrar", 1f), new Pose("PJ01_Cobertura_Disparo", 0.5f), new Pose("PJ01_Inter_AbrirPuerta", 0.5f),
                new Pose("PJ01_Inter_Boton", 0.5f), new Pose("PJ01_Inter_Recoger", 0.5f), new Pose("PJ01_Inter_VerTelefono", 0.5f),
            });
        }

        public static void CarroMoto()
        {
            Hoja("carro-moto", new[]
            {
                new Pose("PJ01_Carro_Entrar", 0.5f), new Pose("PJ01_Carro_Manejar", 0.5f), new Pose("PJ01_Carro_Salir", 0.5f),
                new Pose("PJ01_Moto_Subir", 0.5f), new Pose("PJ01_Moto_Manejar", 0.5f), new Pose("PJ01_Moto_Bajar", 0.5f),
            });
        }

        /// <summary>Una hoja de frente y otra de lado; cada pose en su lugar, de izquierda a derecha.</summary>
        public static void Hoja(string nombre, Pose[] poses)
        {
            CapturaPersonajes.EscenaDePrueba();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(Prefab));
            var instancias = new List<GameObject>();
            for (int i = 0; i < poses.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancias.Add(inst);
                PonerImplantes(inst, poses[i].Implantes);
                Animator a = inst.GetComponentInChildren<Animator>();
                a.enabled = false;
                AnimationClip clip = ConstructorControladorBase.Clip(poses[i].Clip);
                if (clip == null)
                {
                    continue;
                }
                clip.SampleAnimation(a.gameObject, clip.length * poses[i].Fraccion);
                Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
                inst.transform.position += new Vector3(i * Separacion - cadera.x, 0f, -cadera.z);
                Medir(inst, poses[i]);
            }
            float centro = (poses.Length - 1) * Separacion * 0.5f;
            Camera c = CapturaPersonajes.ColocarCamara(new Vector3(centro, 0.1f, 0f), Vector3.back, 11f, 1.0f, 32f);
            CapturaPersonajes.Guardar(c, "docs/capturas/pj01-" + nombre + "-frente.png");
            // De lado: los personajes en fila hacia el fondo se taparían; se giran 90° (miran a la izquierda de la cámara) en su sitio.
            foreach (GameObject inst in instancias)
            {
                inst.transform.RotateAround(new Vector3(inst.transform.position.x, 0f, 0f), Vector3.up, -90f);
            }
            CapturaPersonajes.Guardar(c, "docs/capturas/pj01-" + nombre + "-lado.png");
            Debug.Log("[Personaje] T-960: hoja pj01-" + nombre + " lista.");
        }

        private static void PonerImplantes(GameObject inst, bool puestos)
        {
            ImplantesVisiblesPJ01 imp = inst.GetComponentInChildren<ImplantesVisiblesPJ01>(true);
            if (imp == null)
            {
                return;
            }
            imp.enabled = false;
            foreach (ImplantesVisiblesPJ01.Pieza p in imp.Piezas)
            {
                if (p.objeto != null)
                {
                    bool ver = puestos && p.nivel == 1 && (p.espacio == Core.EspacioImplante.Piernas || p.espacio == Core.EspacioImplante.Brazos);
                    p.objeto.SetActive(ver);
                }
            }
        }

        /// <summary>Punto más bajo y más alto de la malla horneada (m, sobre el piso en y = 0).</summary>
        public static Vector2 AlturaMalla(GameObject inst)
        {
            float bajo = float.MaxValue;
            float alto = float.MinValue;
            var malla = new Mesh();
            foreach (SkinnedMeshRenderer r in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                // Con useScale la malla ya trae la escala: solo falta la posición y el giro del renderer.
                r.BakeMesh(malla, true);
                Matrix4x4 m = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
                foreach (Vector3 v in malla.vertices)
                {
                    float y = m.MultiplyPoint3x4(v).y;
                    bajo = Mathf.Min(bajo, y);
                    alto = Mathf.Max(alto, y);
                }
            }
            Object.DestroyImmediate(malla);
            return new Vector2(bajo, alto);
        }

        private static void Medir(GameObject inst, Pose p)
        {
            Vector2 h = AlturaMalla(inst);
            Debug.Log("[Personaje] T-960 medida " + p.Clip + " @" + p.Fraccion.ToString("F2") + ": bajo " + h.x.ToString("F3") + " m, alto " + h.y.ToString("F3") + " m.");
        }
    }
}
