using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1522 (build 20-nov): corrige la armería de Ciudad_Prologo tras T-1503.
    /// T-1503 volteó los racks I18 de la PARED (de yaw -90 a +90) y dejó igual las vitrinas I17 de ENFRENTE del armero (yaw +90), que estaban al revés.
    /// <see cref="Aplicar"/>: (1) regresa los dos I18 a su rotación y posición de antes de T-1503 (yaw -90, z 23.99955 y 27.99955);
    /// (2) gira 180 grados en Y, sobre su centro, las dos I17 (quedan en yaw -90); (3) acerca al Vendedor (armero) a la pared del fondo, mirando a la puerta.
    /// El mostrador E (MostradorMercadoNegro, 1.1 m delante del armero original) no se mueve. Todo es idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.ArmeriaT1522.Diagnostico  y  ...ArmeriaT1522.Aplicar
    /// </summary>
    public static class ArmeriaT1522
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizTienda = "T931_Armero";
        /// <summary>Cara interior del muro del fondo (MuroFondo: x -29.8, grosor 0.18).</summary>
        public const float CaraMuro = -29.71f;
        /// <summary>Hacia dónde está la puerta de la armería (+X) y el punto de la puerta.</summary>
        public static readonly Vector3 PuntoPuerta = new Vector3(-25.6f, 0f, 26.25f);
        /// <summary>Holgura entre el frente del rack de la pared y la espalda del armero (medio cuerpo 0.3 + 0.1).</summary>
        public const float Holgura = 0.4f;
        public const float XMaxArmero = -28.5f;

        // Rotaciones objetivo (yaw en grados).
        public const float YawI18Pared = -90f;
        public const float YawI17Enfrente = -90f;

        [MenuItem("Mecha Cyberpunk/Mundo/T-1522 Armeria: diagnostico")]
        public static void Diagnostico()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Transform armero = Armero();
            Log("Armero", armero);
            foreach (Transform t in Mostradores())
            {
                Vector3 f = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
                Bounds b = Limites(t);
                string aArmero = armero == null ? "?" : Dot(f, armero.position - b.center).ToString("F2");
                Debug.Log("[T-1522] " + t.name + " pos=" + t.position.ToString("F3") + " rot=" + t.eulerAngles.ToString("F1") + " bounds.centro=" + b.center.ToString("F3") + " tam=" + b.size.ToString("F3")
                    + " frente(+Z)=" + f.ToString("F2") + " dot(frente, hacia armero)=" + aArmero + " dot(frente, hacia puerta)=" + Dot(f, PuntoPuerta - b.center).ToString("F2")
                    + " (>0: el +Z mira hacia ese punto)");
            }
        }

        [MenuItem("Mecha Cyberpunk/Mundo/T-1522 Armeria: aplicar")]
        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int tocados = 0;
            foreach (Transform t in Mostradores())
            {
                if (t.name == "I18")
                {
                    // Estado de antes de T-1503 (escena en cf6ecc0): yaw -90 y z = 23.99955 / 27.99955.
                    if (!YawIgual(t, YawI18Pared))
                    {
                        Vector3 antesP = t.position;
                        Vector3 antesR = t.eulerAngles;
                        float z = t.position.z > 26f ? 27.99955f : 23.99955f;
                        t.SetPositionAndRotation(new Vector3(-29.52f, t.position.y, z), Quaternion.Euler(0f, YawI18Pared, 0f));
                        EditorUtility.SetDirty(t);
                        Registrar("pared regresada", t, antesP, antesR);
                        tocados++;
                    }
                }
                else if (t.name == "I17")
                {
                    if (!YawIgual(t, YawI17Enfrente))
                    {
                        Vector3 antesP = t.position;
                        Vector3 antesR = t.eulerAngles;
                        Bounds b = Limites(t);
                        t.RotateAround(b.center, Vector3.up, 180f);
                        EditorUtility.SetDirty(t);
                        Registrar("enfrente girada 180", t, antesP, antesR);
                        tocados++;
                    }
                }
            }
            Physics.SyncTransforms();
            Transform armero = Armero();
            if (armero == null)
            {
                throw new System.Exception("T-1522: no se encontró GTA_T407/Mercado/Vendedor");
            }
            float x = XArmero();
            if (Mathf.Abs(armero.position.x - x) > 0.001f)
            {
                Vector3 antesP = armero.position;
                Vector3 antesR = armero.eulerAngles;
                armero.position = new Vector3(x, antesP.y, antesP.z);
                armero.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up); // mira a la puerta (+X)
                EditorUtility.SetDirty(armero);
                Registrar("armero movido a la pared", armero, antesP, antesR);
                tocados++;
            }
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[T-1522] listo: " + tocados + " objetos tocados. Armero x=" + armero.position.x.ToString("F3"));
        }

        /// <summary>X del armero: pegado al frente de los racks de la pared (o al muro), nunca más al frente de donde estaba ni pasando las vitrinas.</summary>
        public static float XArmero()
        {
            float limite = CaraMuro;
            float tope = XMaxArmero;
            foreach (Transform t in Mostradores())
            {
                Bounds b = Limites(t);
                if (t.name == "I18" && b.min.z < 26.35f && b.max.z > 25.65f)
                {
                    limite = Mathf.Max(limite, b.max.x);
                }
                else if (t.name == "I17" && b.min.z < 26.35f && b.max.z > 25.65f)
                {
                    tope = Mathf.Min(tope, b.min.x - Holgura);
                }
            }
            return Mathf.Min(limite + Holgura, tope);
        }

        public static IEnumerable<Transform> Mostradores()
        {
            GameObject r = GameObject.Find(RaizTienda);
            if (r == null)
            {
                yield break;
            }
            foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
            {
                if ((t.name == "I17" || t.name == "I18") && t.GetComponentInChildren<Renderer>(true) != null)
                {
                    yield return t;
                }
            }
        }

        public static Transform Armero()
        {
            GameObject g = GameObject.Find("GTA_T407");
            Transform t = g == null ? null : g.transform.Find("Mercado/Vendedor");
            return t;
        }

        public static bool YawIgual(Transform t, float yaw) => Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.y, yaw)) < 1f;

        public static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(t.position, Vector3.zero);
            for (int i = 0; i < rs.Length; i++)
            {
                if (i == 0) { b = rs[i].bounds; } else { b.Encapsulate(rs[i].bounds); }
            }
            return b;
        }

        private static float Dot(Vector3 frente, Vector3 hacia) => Vector3.Dot(frente, Vector3.ProjectOnPlane(hacia, Vector3.up).normalized);

        private static void Log(string etiqueta, Transform t)
        {
            Debug.Log("[T-1522] " + etiqueta + ": " + (t == null ? "no encontrado" : t.name + " pos=" + t.position.ToString("F3") + " rot=" + t.eulerAngles.ToString("F1")));
        }

        private static void Registrar(string que, Transform t, Vector3 antesP, Vector3 antesR)
        {
            Debug.Log("[T-1522] " + que + ": " + t.name + " pos " + antesP.ToString("F4") + " -> " + t.position.ToString("F4") + " rot " + antesR.ToString("F1") + " -> " + t.eulerAngles.ToString("F1"));
        }
    }
}
