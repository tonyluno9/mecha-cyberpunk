using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba de la zona del interior de Alan (solo para escenas y pruebas de Tony). Trigger que cubre el
    /// interior: detecta al jugador con GetComponentInParent&lt;IJugador&gt;() y avisa una sola vez por entrada y por
    /// salida, aunque entren varios colliders del jugador.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZonaLugarDePrueba : MonoBehaviour
    {
        [SerializeField] private LugarDePrueba lugar;

        private int collidersDentro;

        private void Awake()
        {
            if (lugar == null)
            {
                Debug.LogError("[Personaje] Falta el lugar en ZonaLugarDePrueba de " + name, this);
                enabled = false;
                return;
            }
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnDisable()
        {
            if (collidersDentro > 0)
            {
                collidersDentro = 0;
                Salir();
            }
        }

        private void OnTriggerEnter(Collider otro)
        {
            if (otro.GetComponentInParent<IJugador>() == null) return;
            collidersDentro++;
            if (collidersDentro != 1) return;
            lugar.PonerJugadorDentro(true);
            EventosLugares.PublicarEntro(lugar.Id);
        }

        private void OnTriggerExit(Collider otro)
        {
            if (otro.GetComponentInParent<IJugador>() == null || collidersDentro == 0) return;
            collidersDentro--;
            if (collidersDentro == 0) Salir();
        }

        private void Salir()
        {
            lugar.PonerJugadorDentro(false);
            EventosLugares.PublicarSalio(lugar.Id);
        }
    }
}
