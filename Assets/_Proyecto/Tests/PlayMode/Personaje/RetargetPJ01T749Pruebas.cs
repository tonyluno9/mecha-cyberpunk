using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-749: el clip PJ01_* (Humanoid, sobre el cuerpo de PJ01) deja la pistola y las manos como el mismo clip sin retargeting (la copia Generic de ClipsReferencia, lo que se ve en Blender).
    /// Se mide en el espacio del cuerpo, a varios tiempos de cada clip: giro y cabeceo del cañón, ladeo de la corredera y distancia de la mano izquierda a la empuñadura.
    /// Las medidas van a Logs/t749-medidas.txt.
    /// </summary>
    public class RetargetPJ01T749Pruebas
    {
        private static readonly float[] Fracciones = { 0f, 0.25f, 0.5f, 0.75f };

        private static void Comparar(string nombre, out float peorCanon, out float peorManos, out float peorLadeo, out float peorHueso)
        {
            var humano = new MedidaPoseClipsPJ01.CuerpoHumanoide(MedidaPoseClipsPJ01.ClipHumanoide(nombre), new Vector3(-5f, 0f, 0f));
            var blender = new MedidaPoseClipsPJ01.CuerpoBlender(nombre, new Vector3(5f, 0f, 0f));
            peorCanon = 0f; peorManos = 0f; peorLadeo = 0f; peorHueso = 0f;
            try
            {
                foreach (float f in Fracciones)
                {
                    float t = Mathf.Min(f * blender.Largo, blender.Largo - 0.001f);
                    humano.Posar(t);
                    blender.Posar(t);
                    PoseMedida h = humano.Medir(), b = blender.Medir();
                    float canon = Vector3.Angle(h.CanonDir, b.CanonDir);
                    float manos = Mathf.Abs(h.IzquierdaALaEmpunadura - b.IzquierdaALaEmpunadura) * 100f;
                    float ladeo = Mathf.Abs(h.Ladeo - b.Ladeo);
                    float huesos = MedidaPoseClipsPJ01.CompararHuesos(humano.Raiz.transform, blender.Marco.transform, humano.Hueso, blender.Hueso, out string texto);
                    peorCanon = Mathf.Max(peorCanon, canon); peorManos = Mathf.Max(peorManos, manos); peorLadeo = Mathf.Max(peorLadeo, ladeo); peorHueso = Mathf.Max(peorHueso, huesos);
                    float rumboU = PoseMedida.Rumbo(humano.Raiz.transform, humano.Hueso("L_Thigh").position, humano.Hueso("R_Thigh").position);
                    float rumboB = PoseMedida.Rumbo(blender.Marco.transform, blender.Hueso("L_Thigh").position, blender.Hueso("R_Thigh").position);
                    float pechoU = PoseMedida.Rumbo(humano.Raiz.transform, humano.Hueso("L_Upperarm").position, humano.Hueso("R_Upperarm").position);
                    float pechoB = PoseMedida.Rumbo(blender.Marco.transform, blender.Hueso("L_Upperarm").position, blender.Hueso("R_Upperarm").position);
                    Quaternion qb = Quaternion.Inverse(blender.Marco.transform.rotation) * blender.Hueso("R_Hand").rotation * Quaternion.Euler(AgarresArmas.GiroPJ01);
                    MedidaPoseClipsPJ01.Anotar("  rumbo pelvis U=" + rumboU.ToString("F0") + " B=" + rumboB.ToString("F0") + " | hombros U=" + pechoU.ToString("F0") + " B=" + pechoB.ToString("F0") +
                        " | canon B con los ejes del hueso R_Hand de Blender: giro " + (Mathf.Atan2((qb * Vector3.forward).x, (qb * Vector3.forward).z) * Mathf.Rad2Deg).ToString("F0"));
                    MedidaPoseClipsPJ01.Anotar(nombre + " t=" + t.ToString("F2") + " | Unity (Humanoid): " + h + " | Blender: " + b +
                        " | canon aparta " + canon.ToString("F2") + " deg, manos aparta " + manos.ToString("F2") + " cm, articulaciones max " + huesos.ToString("F1") + " cm: " + texto);
                }
            }
            finally
            {
                humano.Destruir();
                blender.Destruir();
            }
        }

        [UnityTest]
        public IEnumerator Retarget_ApuntarDisparoYLaterales_IgualQueBlender([ValueSource(nameof(ClipsDeApuntar))] string nombre)
        {
            yield return null;
            Comparar(nombre, out float canon, out float manos, out float ladeo, out float hueso);
            Assert.Less(canon, 2f, nombre + ": el canon de la pistola a menos de 2 grados de Blender");
            Assert.Less(ladeo, 2f, nombre + ": la corredera igual que en Blender");
            Assert.Less(manos, 2f, nombre + ": la mano izquierda a menos de 2 cm de donde esta en Blender");
        }

        public static string[] ClipsDeApuntar => new[]
        {
            "PJ01_Pistola_Apuntar", "PJ01_Pistola_Disparo", "PJ01_Pistola_ApuntarLateral_Derecha", "PJ01_Pistola_ApuntarLateral_Izquierda", "PJ01_Pistola_ApuntarAtras",
            "PJ01_Pistola_Idle", "PJ01_Pistola_Caminar", "PJ01_Pistola_ApuntarCaminar_Derecha", "PJ01_Pistola_ApuntarCaminar_Izquierda", "PJ01_Pistola_Correr",
        };
    }
}
