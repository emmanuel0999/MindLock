using System.Collections.Generic;

namespace Mindlock.Datos
{
    /// <summary>
    /// MODO DESAFÍO: acertijos mucho más difíciles del nivel 5 al 30.
    /// Los niveles 1 a 4 y los acertijos diarios no cambian.
    /// Los ids empiezan en 200 para que el historial de intentos no se mezcle
    /// con los acertijos originales.
    /// </summary>
    public static partial class CatalogoAcertijos
    {
        public const int DesafioDesdeNivel = 5;

        static List<Acertijo> AcertijosDesafio()
        {
            return new List<Acertijo>
            {
                // ---------- FÁCIL (5-8) ----------
                Secuencia(205, T, "Completa la serie:\n2, 12, 36, 80, 150, ?", "252", new[] { "216", "225", "252", "260" },
                    "Cada número depende de su posición (1, 2, 3...).", "Cada término combina el cuadrado y el cubo de su posición.", "Posición 6: 6³ + 6² = ?"),
                Texto(206, P, "¿Qué palabra se escribe incorrectamente en todos los diccionarios?", "incorrectamente",
                    "No es una errata de imprenta.", "La respuesta está escondida en la pregunta.", "Lee la pregunta palabra por palabra."),
                Opcion(207, L, "A dice: «B miente». B dice: «C miente». C dice: «A y B mienten». Cada uno siempre miente o siempre dice la verdad. ¿Quién dice la verdad?", "Solo B",
                    new[] { "Solo A", "Solo B", "Solo C", "A y C" },
                    "Supón que C dice la verdad y busca una contradicción.", "C no puede decir la verdad, así que A o B dicen la verdad.", "Si C miente, lo que dice B es cierto."),
                Secuencia(208, T, "Completa las dos que faltan:\nU, D, T, C, C, S, S, ?, ?", "O|N", new[] { "O", "N", "D", "E", "V" },
                    "No es el abecedario.", "Piensa en números escritos con letras.", "Uno, Dos, Tres, Cuatro..."),

                // ---------- MEDIA (9-16) ----------
                Texto(209, L, "Tengo el doble de la edad que tú tenías cuando yo tenía la edad que tú tienes. Cuando tú tengas la edad que yo tengo, sumaremos 63 años. ¿Cuántos años tengo?", "28;veintiocho;28 años;veintiocho años",
                    "Llama x a mi edad e y a la tuya.", "La primera frase da 3x = 4y.", "La segunda frase da 3x - y = 63."),
                Secuencia(210, T, "Completa los dos que faltan:\n5, 4, 2, 9, 8, 6, 7, ?, ?", "3|1", new[] { "0", "1", "3", "10" },
                    "No es una operación matemática.", "Escribe cada número con letras.", "Están en orden alfabético: cinco, cuatro, dos..."),
                Texto(211, P, "¿Qué número del 1 al 20, escrito con letras, tiene todas sus letras en orden alfabético?", "dos;2",
                    "Escribe los números con letras.", "Es un número muy pequeño.", "Sus letras son d, o, s."),
                Opcion(212, L, "Una familia tiene dos hijos y al menos uno es varón. ¿Qué probabilidad hay de que los dos sean varones?", "1/3",
                    new[] { "1/2", "1/3", "1/4", "2/3" },
                    "Enumera todas las combinaciones posibles de dos hijos.", "Son VV, VM, MV y MM, todas igual de probables.", "Descarta MM: quedan tres casos."),
                Secuencia(213, T, "Completa la serie:\n1, 4, 27, 256, ?", "3125", new[] { "625", "1024", "3025", "3125" },
                    "Cada número depende de su posición.", "La posición se eleva a sí misma.", "5 elevado a 5 = ?"),
                Opcion(214, P, "Si CASA se escribe DBTB, ¿cómo se escribe LOGICA?", "MPHJDB",
                    new[] { "MPHJDB", "KNFHBZ", "MPHJCB", "NQIKEC" },
                    "Compara letra por letra CASA y DBTB.", "Cada letra avanza una posición en el abecedario.", "L pasa a M, O pasa a P, G pasa a H..."),
                Opcion(215, L, "Tienes 9 monedas iguales, pero una pesa un poco más. Con una balanza de dos platillos, ¿cuántas pesadas necesitas como mínimo para encontrarla con seguridad?", "2",
                    new[] { "1", "2", "3", "4" },
                    "Cada pesada tiene tres resultados: izquierda, derecha o equilibrio.", "Divide las monedas en tres grupos de 3.", "Con dos pesadas distingues 3 x 3 = 9 casos."),
                Secuencia(216, T, "Completa la serie:\n2, 6, 30, 210, 2310, ?", "30030", new[] { "23100", "25410", "30030", "32310" },
                    "Divide cada número entre el anterior.", "Los factores son 3, 5, 7, 11...", "Son números primos: 2310 x 13 = ?"),

                // ---------- DIFÍCIL (17-24) ----------
                Texto(217, P, "El que lo hace no lo quiere, el que lo compra no lo usa y el que lo usa no lo ve. ¿Qué es?", "ataúd;féretro;cajón;urna",
                    "Nadie lo quiere para sí.", "Tiene que ver con un final.", "Se usa en un funeral."),
                Texto(218, L, "Un reloj se atrasa 2 minutos cada hora. Lo pones en hora a las 12:00 del mediodía. ¿Qué hora marcará cuando realmente sea medianoche? (formato HH:MM)", "11:36;11 36",
                    "¿Cuántas horas pasan del mediodía a la medianoche?", "En 12 horas se atrasa 12 x 2 minutos.", "Se atrasa 24 minutos: réstalos a las 12:00."),
                Secuencia(219, T, "Completa los dos que faltan:\n2, 5, 11, 23, 47, ?, ?", "95|191", new[] { "94", "95", "96", "189", "191" },
                    "Compara cada número con el doble del anterior.", "Cada término es el doble del anterior más uno.", "47 x 2 + 1 = 95"),
                Opcion(220, L, "En una sala hay 100 personas y el 99 % son hombres. ¿Cuántos hombres deben salir para que los hombres sean el 98 %?", "50",
                    new[] { "1", "2", "49", "50" },
                    "La cantidad de mujeres no cambia.", "Hay 1 mujer, y debe ser el 2 % del total.", "1 es el 2 % de 50."),
                Opcion(221, L, "Juan es hijo único. Señalando un retrato dice: «El padre de ese hombre es el hijo de mi padre». ¿Quién está en el retrato?", "El hijo de Juan",
                    new[] { "Juan mismo", "El padre de Juan", "El hijo de Juan", "El nieto de Juan" },
                    "¿Quién es «el hijo de mi padre» si Juan no tiene hermanos?", "«El hijo de mi padre» es el propio Juan.", "Entonces el padre del hombre del retrato es Juan."),
                Secuencia(222, T, "¿Qué letra sigue? (abecedario sin la Ñ)\nZ, X, U, Q, L, ?", "F", new[] { "E", "F", "G", "H" },
                    "Recorre el abecedario hacia atrás.", "Los saltos crecen: 2, 3, 4, 5...", "Desde L retrocede 6 letras."),
                Texto(223, L, "¿Cuántas veces al día se superponen exactamente la aguja de las horas y la de los minutos?", "22;veintidós;22 veces;veintidós veces",
                    "No son 24.", "En 12 horas se superponen 11 veces.", "11 x 2 = ?"),
                Texto(224, P, "Cuanto más grande es, menos se ve. ¿Qué es?", "oscuridad;obscuridad",
                    "No es un objeto.", "Aparece cuando se va la luz.", "Es lo contrario de la claridad."),

                // ---------- EXPERTO (25-30) ----------
                Secuencia(225, T, "Completa la serie:\n1, 2, 6, 15, 31, ?", "56", new[] { "47", "52", "56", "63" },
                    "Mira las diferencias entre los números.", "Las diferencias son cuadrados: 1, 4, 9, 16...", "31 + 25 = ?"),
                Opcion(226, L, "Fuera de un cuarto cerrado hay 3 interruptores; solo uno enciende la bombilla de dentro. Puedes tocar los interruptores lo que quieras, pero solo puedes entrar una vez. ¿Cómo sabes cuál es?",
                    "Enciendo uno un rato, lo apago, enciendo otro y entro a tocar la bombilla",
                    new[]
                    {
                        "Enciendo los tres y entro",
                        "Enciendo uno un rato, lo apago, enciendo otro y entro a tocar la bombilla",
                        "Enciendo uno, entro y si no se encendió adivino entre los otros dos",
                        "No se puede saber entrando una sola vez"
                    },
                    "La bombilla da más información que solo su luz.", "Una bombilla encendida se calienta.", "Usa el calor para identificar el primer interruptor."),
                Texto(227, P, "Lo que el pobre tiene, el rico necesita y si lo comes te mueres. ¿Qué es?", "nada",
                    "No es un objeto.", "Piensa en lo que NO tiene alguien.", "Si no comes ___, te mueres de hambre."),
                Texto(228, L, "En un torneo de tenis por eliminación directa participan 147 jugadores. ¿Cuántos partidos se juegan hasta tener un campeón?", "146;146 partidos;ciento cuarenta y seis",
                    "Cada partido elimina exactamente a un jugador.", "Al final solo queda uno sin eliminar.", "147 - 1 = ?"),
                Secuencia(229, T, "Ordena de menor a mayor:\n7/10, 2/3, 5/8, 9/13", "5/8|2/3|9/13|7/10", new[] { "7/10", "2/3", "5/8", "9/13" },
                    "Convierte cada fracción a decimal.", "5/8 = 0.625 y 7/10 = 0.7.", "2/3 es casi 0.667 y 9/13 es casi 0.692."),
                Opcion(230, L, "Tienes un bidón de 5 litros y otro de 3, sin marcas. Contando cada vez que llenas, vacías o pasas agua de uno a otro, ¿cuál es el mínimo de pasos para tener exactamente 4 litros?", "6",
                    new[] { "4", "5", "6", "8" },
                    "Empieza llenando el bidón de 5 litros.", "Llena el de 3 con el de 5: quedan 2 litros en el grande.", "Vacía el de 3, pasa los 2 litros, llena el de 5 y completa el de 3."),
            };
        }

        static int[] PenalizacionesDesafio(Dificultad d)
        {
            switch (d)
            {
                case Dificultad.Facil: return new[] { 100, 150, 200 };
                case Dificultad.Media: return new[] { 150, 250, 350 };
                case Dificultad.Dificil: return new[] { 250, 350, 450 };
                default: return new[] { 350, 500, 650 };
            }
        }

        static readonly int[] PenalizacionesDiarioDesafio = { 60, 100, 150 };
    }
}
