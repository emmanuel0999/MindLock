# Mindlock – Juego de acertijos (Unity 6000.0.84f1)

Proyecto de Software II – Unicaribe.
Emmanuel Rodriguez · Ezequiel Javier Herrera · Dionicio Moreta Andujar

Juego de un solo jugador, sin conexión y sin compras: 30 niveles de lógica, palabras y
patrones, pistas con penalización, puntaje y estrellas, logros, acertijo diario con racha
y guardado del progreso en el dispositivo.

---

¿Eres desarrollador del equipo? Lee `PREGUNTAS_DESARROLLADORES.docx`.

---

## Abrir y jugar

1. En **Unity Hub → Add → Add project from disk** elige la carpeta `Mindlock`.
   (Versión del editor: **6000.0.84f1**.)
2. Abre la escena **`Assets/Scenes/Mindlock.unity`** y pulsa **Play**.
3. Para verlo como teléfono: pestaña **Game** → resolución vertical (por ejemplo 1080 × 1920)
   o la pestaña **Simulator**.

La escena, el Panel Settings y los ajustes del jugador ya están creados. Si algo se borra,
usa el menú **Mindlock → Configurar proyecto** y se vuelve a generar todo.

## Menú «Mindlock» del editor

| Opción | Qué hace |
|---|---|
| Configurar proyecto | Crea/actualiza el Panel Settings, la escena principal y los ajustes (vertical, nombre, identificador de la app). |
| Abrir carpeta de datos guardados | Muestra el archivo `mindlock_datos.json` con el progreso. |
| Borrar datos guardados | Empieza de cero (borra perfiles y progreso). |
| Probar lógica y guardado | Revisa las respuestas de los 51 acertijos, el puntaje y que el guardado funcione. |
| Compilar → Windows / Android / Web | Genera el juego en la carpeta `Builds/`. |

## Cómo está organizado (arquitectura por capas del informe)

```
Assets/Mindlock/
├── Resources/Mindlock/Pantallas/   UXML de las 9 pantallas (interfaz)
├── Resources/Mindlock/Estilos/     USS: tema, modo claro, paletas, tamaños de texto
├── Scripts/UI/                     CAPA DE PRESENTACIÓN: UIManager + una clase por pantalla
├── Scripts/Logica/                 CAPA DE LÓGICA: CalculadoraPuntaje
├── Scripts/Nucleo/                 Servicios, Sesión, Preferencias y Sonidos
├── Scripts/Datos/                  CAPA DE DATOS
│   ├── Modelos.cs                  Jugador, Nivel, Acertijo, Pista, Progreso, Intento, Logro…
│   ├── Interfaces.cs               contratos que usa la interfaz
│   ├── CatalogoAcertijos.cs        contenido empaquetado: 30 niveles, 21 diarios, 9 logros
│   ├── MemoriaJuegoService.cs      reglas del juego (progreso, racha, logros, intentos)
│   ├── ArchivoJuegoService.cs      guarda todo en el dispositivo después de cada cambio
│   └── BaseDatosLocal.cs           las 9 tablas del diccionario de datos
└── Editor/                         configuración, pruebas y compilación
```

La interfaz solo habla con la lógica a través de `Servicios`, y la lógica solo con la capa de datos.

## Guardado (R-03 y R-05)

El progreso se guarda en `Application.persistentDataPath/mindlock_datos.json` con las
**9 tablas del diccionario de datos** (`jugador`, `nivel`, `acertijo`, `pista`, `progreso`,
`intento`, `logro`, `jugador_logro`, `acertijo_diario`), con los mismos nombres de campo
y fechas en ISO 8601. Cada respuesta enviada (correcta o incorrecta) queda en `intento`,
y cada fecha tiene su acertijo fijo en `acertijo_diario`.

Se escribe primero en un temporal y se conserva una copia `.bak`: si el archivo se daña,
el juego recupera la copia.

**¿Por qué no SQLite todavía?** Unity no trae el motor nativo de SQLite para Android/iOS;
hay que añadir un plugin (por ejemplo `sqlite-net` con sus binarios). Como la interfaz
solo usa las interfaces de `Interfaces.cs`, pasar a SQLite es crear una clase
`SqliteJuegoService` con las mismas tablas y registrarla con `Servicios.Configurar(...)`;
no hay que tocar pantallas ni estilos.

Para probar la interfaz con perfiles de ejemplo (Ana y Carlos), marca **Usar Datos De Demo**
en el componente `UIManager` del objeto `UI` (en ese modo no se guarda nada).

## Lo que se agregó a los recursos originales

- **Guardado real en el dispositivo** (antes los datos se perdían al cerrar).
- **Historial de intentos** (tabla `intento`) y **acertijo diario fijo por fecha**.
- **Sonido**: efectos (toque, acierto, error, pista, candado, estrellas, logro, tiempo
  agotado) y música ambiental en bucle, generados por código. Respetan los interruptores
  de Música y Efectos de Opciones.
- **Paletas de color** en Opciones → Colores: *Clásica*, *Océano* y *Aurora*, cada una con
  versión oscura y clara.
- **14 acertijos diarios nuevos** (ahora son 21, uno distinto cada día durante 3 semanas).
- Escena, Panel Settings y ajustes de teléfono vertical ya configurados; 60 FPS.
- Pruebas automáticas de lógica y guardado, y menús para compilar.

## Modo desafío (activado)

Del nivel 5 al 30 se usan acertijos mucho más difíciles (`Scripts/Datos/CatalogoAcertijos.Desafio.cs`)
y las pistas restan más puntos:

| Dificultad | Pistas normal | Pistas desafío |
|---|---|---|
| Fácil | 50 / 75 / 100 | 100 / 150 / 200 |
| Media | 75 / 100 / 150 | 150 / 250 / 350 |
| Difícil | 100 / 150 / 200 | 250 / 350 / 450 |
| Experto | 150 / 200 / 300 | 350 / 500 / 650 |
| Acertijo diario | 30 / 50 / 75 | 60 / 100 / 150 |

**Para volver a la configuración original:** selecciona el objeto `UI` de la escena y, en el
componente `UIManager`, desmarca **Modo Desafio**. Los acertijos originales siguen intactos en
`CatalogoAcertijos.AcertijosOriginales()` (y hay una copia del archivo en `Respaldo/`).
El progreso guardado se conserva al cambiar de modo; solo cambia qué acertijo tiene cada nivel.

## Respuestas para probar

Nivel 1 → `10` · Nivel 2 → `reloj` · Nivel 3 → *Misi es un animal* · Nivel 9 → `9` ·
Nivel 15 → *Solo manzanas* · Nivel 16 → `36`.
Las respuestas de texto ignoran mayúsculas, acentos y artículos («El reloj» = «reloj»).

## Reglas

- Fácil 1–8 (500 pts), Media 9–16 (800), Difícil 17–24 (1 000, límite 3:00), Experto 25–30 (1 500, límite 4:00).
- Puntaje = base − tiempo extra − intentos fallidos − pistas (mínimo 10 % de la base).
  3★ con ≥ 85 %, 2★ con ≥ 60 %, si no 1★.
- Empiezas con 3 pistas; ganas +1 con el acertijo diario y +1 al sacar 3★ por primera vez (máximo 9).
- Acertijo diario: +150 pts de bonus; la racha sube si jugaste ayer y se reinicia si faltaste un día.
