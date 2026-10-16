using UnityEngine;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Herramienta de prueba (T-081): promedia <c>Time.unscaledDeltaTime</c> durante N fotogramas en Play.
    /// Con el editor vivo: <c>Iniciar(300)</c> con eval, esperar unos segundos y leer <c>Resultado</c>.
    /// </summary>
    public class MedidorFotogramas : MonoBehaviour
    {
        private const string NombreObjeto = "MedidorFotogramas";

        private static string resultado = "sin medir";

        private int pendientes;
        private int total;
        private double suma;

        public static string Resultado => resultado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            resultado = "sin medir";
        }

        public static void Iniciar(int fotogramas)
        {
            resultado = "midiendo";
            GameObject objeto = new GameObject(NombreObjeto);
            MedidorFotogramas medidor = objeto.AddComponent<MedidorFotogramas>();
            medidor.pendientes = fotogramas;
            medidor.total = fotogramas;
        }

        private void Update()
        {
            suma += Time.unscaledDeltaTime;
            pendientes--;
            if (pendientes <= 0)
            {
                resultado = "promedio=" + (suma / total * 1000.0).ToString("F3") + " ms en " + total + " fotogramas";
                Destroy(gameObject);
            }
        }
    }
}
