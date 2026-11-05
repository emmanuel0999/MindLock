using System.Collections.Generic;

namespace Mindlock.Datos
{
    /// <summary>
    /// CONFIGURACIÓN NORMAL: los niveles 1 a 15 son los originales y del 16 al 30 los
    /// acertijos son difíciles y cada uno más complejo que el anterior. Se eligen por id
    /// entre los originales y los del modo desafío, así no se repite ningún enunciado.
    /// </summary>
    public static partial class CatalogoAcertijos
    {
        public const int DificilesDesdeNivel = 16;

        /// <summary>Acertijo de cada nivel del 16 al 30, de menor a mayor complejidad.</summary>
        public static readonly int[] IdsNivelesDificiles =
        {
            219,   // 16 · Patrones: doble del anterior más uno (95, 191)
            217,   // 17 · Palabras: el que lo hace no lo quiere (ataúd)
            218,   // 18 · Lógica: reloj que se atrasa 2 minutos por hora
            225,   // 19 · Patrones: diferencias que son cuadrados
            214,   // 20 · Palabras: clave que avanza una letra (LOGICA)
            28,    // 21 · Lógica: el caracol en el pozo
            222,   // 22 · Patrones: letras hacia atrás con saltos que crecen
            227,   // 23 · Palabras: lo que el pobre tiene y el rico necesita
            228,   // 24 · Lógica: partidos de un torneo de 147 jugadores
            25,    // 25 · Patrones: la serie que describe al número anterior
            212,   // 26 · Lógica: probabilidad de dos hijos varones
            220,   // 27 · Lógica: cuántos hombres salen para llegar al 98 %
            210,   // 28 · Patrones: números ordenados alfabéticamente
            230,   // 29 · Lógica: medir 4 litros con bidones de 5 y 3
            209,   // 30 · Lógica: el problema de las edades
        };

        static List<Acertijo> AcertijosNormales()
        {
            var lista = AcertijosOriginales().GetRange(0, DificilesDesdeNivel - 1);
            var disponibles = new Dictionary<int, Acertijo>();
            foreach (var a in AcertijosOriginales()) disponibles[a.Id] = a;
            foreach (var a in AcertijosDesafio()) disponibles[a.Id] = a;
            foreach (int id in IdsNivelesDificiles) lista.Add(disponibles[id]);
            return lista;
        }
    }
}
