using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1632 (T18): capturas en Ciudad_Prologo de los NPCs sentados (parque y otro punto, cámara a ~3 m) y de uno huyendo tras un disparo. Explicit.</summary>
    public class NpcSentadoT1632Captura
    {
        private const string Carpeta = "docs/capturas/_tareas/T18";

        [UnityTest, Explicit("Genera docs/capturas/_tareas/T18/*.png (requiere haber corrido NpcsSentadosT1632.Aplicar)")]
        public IEnumerator CiudadPrologo_SentadosYHuida()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 40; i++)
            {
                yield return null;
            }
            Directory.CreateDirectory(Carpeta);
            NpcSentado[] todos = Object.FindObjectsByType<NpcSentado>(FindObjectsSortMode.None).OrderBy(n => n.name).ToArray();
            Assert.Greater(todos.Length, 0, "no hay NpcSentado en la escena: corre NpcsSentadosT1632.Aplicar");
            var sb = new StringBuilder();
            sb.AppendLine("NpcSentado: " + todos.Length);
            foreach (NpcSentado n in todos)
            {
                sb.AppendLine(n.name + " fase=" + n.FaseActual + " dif_cadera=" + n.DiferenciaCadera.ToString("0.000"));
            }
            File.WriteAllText(Path.Combine(Carpeta, "resultado.txt"), sb.ToString());

            NpcSentado parque = todos.FirstOrDefault(n => n.name.Contains("BancaSentable")) ?? todos[0];
            NpcSentado otro = todos.Where(n => !n.name.Contains("BancaSentable")).OrderByDescending(n => Vector3.Distance(n.transform.position, parque.transform.position)).FirstOrDefault()
                ?? todos[todos.Length - 1];
            GameObject camObjeto = new GameObject("Cam_T1632");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            Foto(cam, parque, "parque.png");
            Foto(cam, otro, "otro-punto.png");

            Vector3 detras = parque.transform.position - parque.FrenteAsiento * 10f;
            EventosRuido.PublicarDisparoConRadio(detras + Vector3.up, 25f);
            yield return new WaitForSeconds(2.2f);
            Foto(cam, parque, "tras-disparo.png");
            File.AppendAllText(Path.Combine(Carpeta, "resultado.txt"), "tras el disparo: " + parque.name + " fase=" + parque.FaseActual + "\n");
            Object.Destroy(camObjeto);
        }

        private static void Foto(Camera cam, NpcSentado npc, string nombre)
        {
            Vector3 asiento = npc.PosicionAsiento;
            Vector3 foco = npc.isActiveAndEnabled ? npc.transform.position + Vector3.up * 0.8f : asiento;
            cam.transform.position = asiento + npc.FrenteAsiento * 2.6f + Vector3.up * 1.4f + Vector3.Cross(Vector3.up, npc.FrenteAsiento) * 0.8f;
            cam.transform.LookAt(foco);
            CapturaPrueba.Guardar(cam, "_tareas/T18/" + nombre);
        }
    }
}
