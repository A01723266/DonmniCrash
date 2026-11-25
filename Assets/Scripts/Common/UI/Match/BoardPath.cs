using System.Collections.Generic;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Traza el historial público en casillas de media ficha. El origen es el primer escudo.</summary>
    internal sealed class BoardPath
    {
        static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.up, Vector2Int.down };
        readonly HashSet<Vector2Int> occupied = new();
        Vector2Int sword, previous;
        uint hash = 2166136261u;
        int run;

        public (Vector2 center, Vector2Int direction) Append(int tileId)
        {
            // Una mezcla entera estable reproduce las mismas esquinas al recibir un historial completo o por partes.
            hash = unchecked((hash ^ (uint)tileId) * 16777619u);
            uint sample = unchecked((hash ^ (hash >> 16)) * 2246822519u);
            var direction = occupied.Count == 0 ? Vector2Int.right : Choose(sample ^ (sample >> 13));
            var shield = occupied.Count == 0 ? Vector2Int.zero : sword + direction;
            sword = shield + direction;
            occupied.Add(shield);
            occupied.Add(sword);
            run = previous == direction ? run + 1 : 1;
            previous = direction;
            return (((Vector2)shield + sword) * .5f, direction);
        }

        Vector2Int Choose(uint sample)
        {
            // La derecha siempre conduce a una columna nueva. En vertical no se permite volver sobre la cadena.
            int total = 0;
            System.Span<int> weights = stackalloc int[3];
            for (int i = 0; i < Directions.Length; i++)
            {
                var direction = Directions[i];
                bool free = !occupied.Contains(sword + direction) && !occupied.Contains(sword + 2 * direction);
                int weight = direction == previous && run >= 2 ? 1 : 4;
                weights[i] = free ? weight : 0;
                total += weights[i];
            }

            int pick = (int)(sample % (uint)total);
            for (int i = 0; i < Directions.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0) return Directions[i];
            }
            throw new System.InvalidOperationException("Invalid board direction weights.");
        }
    }
}
