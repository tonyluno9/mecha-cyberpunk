using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    public class DiagPiesT749
    {
        static string Nombres(Transform t, int prof) { string s = t.name; if (prof > 0) foreach (Transform h in t) s += "(" + Nombres(h, prof - 1) + ")"; return s; }

        [UnityTest]
        public IEnumerator Pies()
        {
            yield return null;
            {
                GameObject a = (GameObject)Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(MedidaPoseClipsPJ01.RutaPJ01));
                Transform pie = MedidaPoseClipsPJ01.Buscar(a.transform, "L_Foot"), pieD = MedidaPoseClipsPJ01.Buscar(a.transform, "R_Foot");
                Vector3 vi = pie.GetChild(0).position - pie.position, vd = pieD.GetChild(0).position - pieD.position; vi.y = 0; vd.y = 0;
                MedidaPoseClipsPJ01.Anotar("PIES A (PJ01.fbx en T) yaw izq " + (Mathf.Atan2(vi.x, vi.z) * Mathf.Rad2Deg).ToString("F1") + " der " + (Mathf.Atan2(vd.x, vd.z) * Mathf.Rad2Deg).ToString("F1") + " rootRot " + a.transform.rotation.eulerAngles);
                var m = new PoseMedida(a.transform, n => MedidaPoseClipsPJ01.Buscar(a.transform, n));
                MedidaPoseClipsPJ01.Anotar("PIES A bind pistola yaw " + m.Giro.ToString("F1") + " cab " + m.Cabeceo.ToString("F1") + " ladeo " + m.Ladeo.ToString("F1"));
                Object.DestroyImmediate(a);
            }
            foreach (string clip in new[] { "PJ01_Pistola_Apuntar", "PJ01_Pistola_Idle", "PJ01_Pistola_Disparo", "PJ01_Pistola_ApuntarLateral_Derecha", "PJ01_Pistola_Caminar" })
            {
                var b = new MedidaPoseClipsPJ01.CuerpoBlender(clip, Vector3.zero);
                MedidaPoseClipsPJ01.Anotar("PIES hijos de L_Foot: " + Nombres(b.Hueso("L_Foot"), 2));
                foreach (float t in new[] { 0.1f, 0.5f })
                {
                    b.Posar(t);
                    Transform pie = b.Hueso("L_Foot"), pieD = b.Hueso("R_Foot");
                    Transform dedo = pie.childCount > 0 ? pie.GetChild(0) : null, dedoD = pieD.childCount > 0 ? pieD.GetChild(0) : null;
                    float Yaw(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                    Vector3 vi = dedo.position - pie.position, vd = dedoD.position - pieD.position; vi.y = 0; vd.y = 0;
                    var m = b.Medir();
                    MedidaPoseClipsPJ01.Anotar("PIES " + clip + " t=" + t + ": pies yaw izq " + Yaw(vi).ToString("F1") + " der " + Yaw(vd).ToString("F1") + " | pistola yaw " + m.Giro.ToString("F1"));
                }
                b.Destruir();
            }
        }
    }
}
