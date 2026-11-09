using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-749: medidas de la pose de PJ01 con la pistola en la mano. Se miden igual en el cuerpo que se mueve en Unity (Humanoid, con el clip PJ01_*) y en la copia Generic
    /// del mismo FBX (<see cref="MechaCyberpunk.Arte.Editor.ImportadorClipsPJ01.CarpetaReferencia"/>), que es lo que se ve en Blender: los huesos tal cual vienen, sin retargeting.
    /// Todo va en el espacio del cuerpo (la raíz del modelo).
    /// </summary>
    public readonly struct PoseMedida
    {
        /// <summary>Giro del cañón a un lado (positivo a la derecha) y cabeceo (positivo hacia arriba), en grados, contra el frente del cuerpo.</summary>
        public readonly float Giro, Cabeceo;
        /// <summary>Ladeo de la corredera: lo que el eje X de la pistola se aparta de la horizontal (grados).</summary>
        public readonly float Ladeo;
        public readonly Vector3 CanonDir;
        public readonly Vector3 Pistola, PalmaIzquierda, ManoDerecha, ManoIzquierda;

        /// <summary>
        /// La pistola va en la mano derecha con el agarre de Tony (AgarresArmas) puesto en el hueso R_Hand de PJ01. Los clips de Blender traen otros ejes en ese hueso, así que la mano se mide
        /// por geometría (muñeca, nudillo del medio, índice y meñique) y la pistola se pone donde estaría con el agarre de Tony en el cuerpo de PJ01 en pose T
        /// (<see cref="MedidaPoseClipsPJ01.Calibracion"/>): sirve igual para el cuerpo Humanoid de Unity y para los huesos de Blender.
        /// </summary>
        public PoseMedida(Transform raiz, System.Func<string, Transform> hueso)
        {
            Transform manoDer = hueso("R_Hand"), manoIzq = hueso("L_Hand"), dedoIzq = hueso("L_Mid1");
            Quaternion marco = MedidaPoseClipsPJ01.MarcoDeMano(hueso("R_Hand").position, hueso("R_Mid1").position, hueso("R_Index1").position, hueso("R_Pinky1").position);
            Quaternion rot = Quaternion.Inverse(raiz.rotation) * marco * MedidaPoseClipsPJ01.Calibracion.GiroPistola;
            CanonDir = rot * Vector3.forward;
            Giro = Mathf.Atan2(CanonDir.x, CanonDir.z) * Mathf.Rad2Deg;
            Cabeceo = Mathf.Asin(Mathf.Clamp(CanonDir.y, -1f, 1f)) * Mathf.Rad2Deg;
            Ladeo = Mathf.Asin(Mathf.Clamp((rot * Vector3.right).y, -1f, 1f)) * Mathf.Rad2Deg;
            Pistola = raiz.InverseTransformPoint(manoDer.position + marco * MedidaPoseClipsPJ01.Calibracion.PosicionPistola);
            PalmaIzquierda = raiz.InverseTransformPoint(Vector3.Lerp(manoIzq.position, dedoIzq.position, 0.5f));
            ManoDerecha = raiz.InverseTransformPoint(manoDer.position);
            ManoIzquierda = raiz.InverseTransformPoint(manoIzq.position);
        }

        /// <summary>Distancia (m) de la palma de la mano izquierda a la empuñadura (donde va la palma derecha).</summary>
        public float IzquierdaALaEmpunadura => Vector3.Distance(PalmaIzquierda, Pistola);
        public float EntreManos => Vector3.Distance(ManoDerecha, ManoIzquierda);

        /// <summary>Hacia dónde mira la pelvis (grados a un lado del frente del cuerpo) y el pecho (Spine02): la línea de los muslos / hombros girada 90 grados.</summary>
        public static float Rumbo(Transform raiz, Vector3 izquierda, Vector3 derecha)
        {
            Vector3 d = raiz.InverseTransformPoint(derecha) - raiz.InverseTransformPoint(izquierda); // de izquierda a derecha
            Vector3 f = Vector3.Cross(Vector3.up, d); // al frente
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        public override string ToString() =>
            "giro " + Giro.ToString("F1") + " cabeceo " + Cabeceo.ToString("F1") + " ladeo " + Ladeo.ToString("F1") +
            " | izq a empunadura " + (IzquierdaALaEmpunadura * 100f).ToString("F1") + " cm, entre manos " + (EntreManos * 100f).ToString("F1") + " cm";
    }

    public static class MedidaPoseClipsPJ01
    {
        /// <summary>Lo mismo que ImportadorClipsPJ01.GiroCuerpoClips: los FBX miran a -Z.</summary>
        public const float GiroCuerpoClips = 180f;
        public const string RutaPJ01 = "Assets/Arte/Personajes/PJ01/PJ01.fbx";
        public const string CarpetaClips = "Assets/Arte/Personajes/PJ01/Clips/";
        public const string CarpetaReferencia = "Assets/Arte/Personajes/PJ01/ClipsReferencia/";

        /// <summary>Marco de la mano por geometría: adelante hacia los dedos, arriba la normal de la palma.</summary>
        public static Quaternion MarcoDeMano(Vector3 muneca, Vector3 nudilloMedio, Vector3 nudilloIndice, Vector3 nudilloMenique)
        {
            Vector3 f = (nudilloMedio - muneca).normalized;
            Vector3 ancho = nudilloIndice - nudilloMenique;
            return Quaternion.LookRotation(f, Vector3.Cross(f, ancho).normalized);
        }

        /// <summary>Dónde queda la pistola (giro y posición) en el marco geométrico de la mano derecha de PJ01 en pose T, con el agarre de Tony en R_Hand.</summary>
        public static class Calibracion
        {
            public static readonly Quaternion GiroPistola;
            public static readonly Vector3 PosicionPistola;

            static Calibracion()
            {
                GameObject pj = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPJ01));
                try
                {
                    Transform mano = Buscar(pj.transform, "R_Hand");
                    Quaternion marco = MarcoDeMano(mano.position, Buscar(pj.transform, "R_Mid1").position, Buscar(pj.transform, "R_Index1").position, Buscar(pj.transform, "R_Pinky1").position);
                    GiroPistola = Quaternion.Inverse(marco) * mano.rotation * Quaternion.Euler(AgarresArmas.GiroPJ01);
                    PosicionPistola = Quaternion.Inverse(marco) * (mano.TransformPoint(AgarresArmas.PosicionPJ01) - mano.position);
                }
                finally { Object.DestroyImmediate(pj); }
            }
        }

        public static Transform Buscar(Transform raiz, string nombre)
        {
            if (raiz.name == nombre) { return raiz; }
            for (int i = 0; i < raiz.childCount; i++)
            {
                Transform t = Buscar(raiz.GetChild(i), nombre);
                if (t != null) { return t; }
            }
            return null;
        }

        public static AnimationClip Clip(string ruta)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { return c; }
            }
            return null;
        }

        public static AnimationClip ClipHumanoide(string nombre) => Clip(CarpetaClips + nombre + ".fbx");
        public static AnimationClip ClipReferencia(string nombre) => Clip(CarpetaReferencia + nombre + ".fbx");

        /// <summary>Copia limpia de PJ01 (Humanoid, sin controlador) con un clip Humanoid evaluado a mano; <see cref="Posar"/> lo lleva a un tiempo.</summary>
        public sealed class CuerpoHumanoide
        {
            public GameObject Raiz;
            public Animator Animador;
            public PlayableGraph Grafo;
            public AnimationClipPlayable Reproductor;

            public CuerpoHumanoide(AnimationClip clip, Vector3 posicion)
            {
                Raiz = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPJ01), posicion, Quaternion.identity);
                Animador = Raiz.GetComponent<Animator>() ?? Raiz.AddComponent<Animator>();
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaPJ01)) { if (o is Avatar a) { Animador.avatar = a; } }
                Animador.applyRootMotion = false;
                Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Animador.runtimeAnimatorController = null;
                Grafo = PlayableGraph.Create("T749");
                Grafo.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                AnimationPlayableOutput salida = AnimationPlayableOutput.Create(Grafo, "salida", Animador);
                Reproductor = AnimationClipPlayable.Create(Grafo, clip);
                Reproductor.SetApplyFootIK(false);
                salida.SetSourcePlayable(Reproductor);
            }

            public void Posar(float tiempo)
            {
                Reproductor.SetTime(tiempo);
                Grafo.Evaluate(0f);
            }

            public Transform Hueso(string nombre) => Buscar(Raiz.transform, nombre);

            public PoseMedida Medir() => new PoseMedida(Raiz.transform, Hueso);

            public void Destruir()
            {
                if (Grafo.IsValid()) { Grafo.Destroy(); }
                Object.DestroyImmediate(Raiz);
            }
        }

        /// <summary>La copia Generic del mismo clip (lo que se ve en Blender), muestreada a mano.</summary>
        public sealed class CuerpoBlender
        {
            /// <summary>El cuerpo cuelga de un nodo sin giro: es el espacio del cuerpo (el nodo raíz del FBX trae un giro de 90 grados propio que se conserva).</summary>
            public GameObject Marco;
            public GameObject Raiz;
            private readonly AnimationClip clip;

            public CuerpoBlender(string nombre, Vector3 posicion)
            {
                string ruta = CarpetaReferencia + nombre + ".fbx";
                Marco = new GameObject("MarcoBlender");
                Marco.transform.position = posicion;
                // SampleAnimation reescribe la raíz del clip: el giro al frente del cuerpo va en un nodo intermedio.
                var giro = new GameObject("GiroFrente").transform;
                giro.SetParent(Marco.transform, false);
                giro.localRotation = Quaternion.Euler(0f, GiroCuerpoClips, 0f);
                Raiz = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), giro);
                clip = Clip(ruta);
            }

            public float Largo => clip.length;

            public void Posar(float tiempo) => clip.SampleAnimation(Raiz, tiempo);

            public Transform Hueso(string nombre) => Buscar(Raiz.transform, nombre);

            public PoseMedida Medir() => new PoseMedida(Marco.transform, Hueso);

            public void Destruir() => Object.DestroyImmediate(Marco);
        }

        /// <summary>Articulaciones que se comparan entre el cuerpo de Unity y el de Blender (su posición en el espacio del cuerpo; no depende de los ejes de cada hueso).</summary>
        public static readonly string[] HuesosComparados =
        {
            "Hip", "Waist", "Spine02", "Head", "L_Upperarm", "L_Forearm", "L_Hand", "L_Mid1", "R_Upperarm", "R_Forearm", "R_Hand", "R_Mid1", "L_Calf", "R_Calf", "L_Foot", "R_Foot",
        };

        /// <summary>Lo que se aparta (cm) cada articulación entre dos cuerpos; devuelve el máximo y el texto.</summary>
        public static float CompararHuesos(Transform raizA, Transform raizB, System.Func<string, Transform> a, System.Func<string, Transform> b, out string texto)
        {
            float max = 0f;
            var sb = new System.Text.StringBuilder();
            foreach (string n in HuesosComparados)
            {
                float cm = Vector3.Distance(raizA.InverseTransformPoint(a(n).position), raizB.InverseTransformPoint(b(n).position)) * 100f;
                if (cm > max) { max = cm; }
                sb.Append(n + "=" + cm.ToString("F1") + " ");
            }
            texto = sb.ToString();
            return max;
        }

        public static void Anotar(string linea)
        {
            Debug.Log("T749 " + linea);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.AppendAllText("Logs/t749-medidas.txt", linea + "\n");
        }
    }
}
