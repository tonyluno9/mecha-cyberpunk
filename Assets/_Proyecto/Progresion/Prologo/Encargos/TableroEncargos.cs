using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Encargos
{
    /// <summary>El tablero de encargos (T-426): se usa con E y ofrece, cancela o muestra la espera del encargo que toca (lo decide <see cref="DirectorEncargos"/>).</summary>
    public class TableroEncargos : MonoBehaviour, IInteractuable
    {
        [SerializeField] private DirectorEncargos director;
        private bool registrado;

        public Vector3 Posicion => transform.position;
        public string Texto => director != null ? director.TextoDelTablero : DirectorEncargos.TextoTablero;
        public bool PuedeUsar => director != null && director.PuedeUsarTablero;

        public void Usar() => director?.UsarTablero();

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
        }
    }
}
