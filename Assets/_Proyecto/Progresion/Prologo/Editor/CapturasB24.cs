using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Ronda B24 (23-nov): ayudas para comprobar en Play con `unity command eval` (T52, T53, T60...).
    /// </summary>
    public static class CapturasB24
    {
        private const string RutaSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls_E19_PandilleroSkulls_a_2.prefab";

        /// <summary>Pone <paramref name="cuantos"/> SKULLS armados a <paramref name="distancia"/> m delante del jugador y los vuelve hostiles.</summary>
        public static string PonerSkulls(int cuantos, float distancia, bool hostiles)
        {
            IJugador j = Registro<IJugador>.Actual;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkulls);
            if (j == null || j.Raiz == null || prefab == null)
            {
                return "sin jugador o sin prefab";
            }
            Vector3 adelante = Vector3.ProjectOnPlane(j.Raiz.forward, Vector3.up).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            var sb = new StringBuilder();
            for (int i = 0; i < cuantos; i++)
            {
                Vector3 donde = j.Raiz.position + adelante * distancia + lado * ((i - (cuantos - 1) * 0.5f) * 2f) + Vector3.up * 0.2f;
                GameObject o = Object.Instantiate(prefab, donde, Quaternion.LookRotation(-adelante, Vector3.up));
                o.name = "SkullPruebaB24_" + i;
                PandilleroAPie p = o.GetComponentInChildren<PandilleroAPie>(true);
                if (p != null && hostiles)
                {
                    p.VolverseHostil();
                }
                sb.Append(o.name).Append(' ').Append(donde.ToString("F1")).Append("; ");
            }
            return sb.ToString();
        }

        /// <summary>Grupo de <paramref name="cuantos"/> SKULLS de calle (PandillaAPie, como en ProvocacionCalleT1528Pruebas) a <paramref name="distancia"/> m delante del jugador.</summary>
        public static string GrupoSkulls(int cuantos, float distancia)
        {
            IJugador j = Registro<IJugador>.Actual;
            if (j == null || j.Raiz == null)
            {
                return "sin jugador";
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkulls);
            Vector3 adelante = Vector3.ProjectOnPlane(j.Raiz.forward, Vector3.up).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            GameObject grupo = new GameObject("GrupoSkullsB24");
            grupo.SetActive(false);
            grupo.transform.position = j.Raiz.position + adelante * distancia;
            for (int i = 0; i < cuantos; i++)
            {
                GameObject p = Object.Instantiate(prefab, grupo.transform);
                p.transform.position = grupo.transform.position + lado * ((i - (cuantos - 1) * 0.5f) * 2f) + Vector3.up * 0.1f;
                p.transform.rotation = Quaternion.LookRotation(-adelante, Vector3.up);
                SerializedObject soP = new SerializedObject(p.GetComponentInChildren<PandilleroAPie>(true));
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
            return "grupo en " + grupo.transform.position.ToString("F1");
        }

        /// <summary>Vuelve hostiles a los del grupo de prueba.</summary>
        public static string Provocar()
        {
            int n = 0;
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                if (p.transform.parent != null && p.transform.parent.name == "GrupoSkullsB24")
                {
                    p.VolverseHostil();
                    n++;
                }
            }
            return "provocados " + n;
        }

        /// <summary>Estado de cada pandillero a menos de <paramref name="radio"/> m: arma, fase, munición, clip actual y velocidad.</summary>
        public static string Estado(float radio)
        {
            IJugador j = Registro<IJugador>.Actual;
            Vector3 centro = j != null && j.Raiz != null ? j.Raiz.position : Vector3.zero;
            var sb = new StringBuilder();
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(p.transform.position, centro);
                if (d > radio)
                {
                    continue;
                }
                sb.Append(p.name).Append(" d=").Append(d.ToString("F1"));
                if (p.ReglaArma != null)
                {
                    sb.Append(" arma=").Append(p.ReglaArma.Tipo).Append(' ').Append(p.ReglaArma.Fase).Append(" mun=").Append(p.ReglaArma.Municion);
                }
                Animator a = p.GetComponentInChildren<Animator>();
                if (a != null)
                {
                    sb.Append(" vel=").Append(a.GetFloat("Velocidad").ToString("F2"));
                    for (int capa = 0; capa < a.layerCount; capa++)
                    {
                        AnimatorClipInfo[] clips = a.GetCurrentAnimatorClipInfo(capa);
                        if (clips.Length > 0)
                        {
                            sb.Append(" c").Append(capa).Append('=').Append(clips[0].clip.name);
                        }
                    }
                    sb.Append(" ctrl=").Append(a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "-");
                }
                sb.Append(" | ");
            }
            return sb.Length == 0 ? "nadie cerca" : sb.ToString();
        }

        /// <summary>Pone la cámara de la vista del jugador mirando hacia el punto (para capturar algo concreto).</summary>
        public static string MirarA(float x, float z)
        {
            IJugador j = Registro<IJugador>.Actual;
            if (j == null || j.Raiz == null)
            {
                return "sin jugador";
            }
            Vector3 hacia = new Vector3(x, j.Raiz.position.y, z) - j.Raiz.position;
            float yaw = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            return Capturas20nB.Poner(j.Raiz.position.x, j.Raiz.position.y, j.Raiz.position.z, yaw, 8f);
        }
    }
}
