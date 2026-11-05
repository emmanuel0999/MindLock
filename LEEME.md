# Mindlock – Juego de acertijos (Unity 6000.0.84f1)

Proyecto de Software II – Unicaribe.
Emmanuel Rodriguez · Ezequiel Javier Herrera · Dionicio Moreta Andujar

Juego de un solo jugador, sin conexión y sin compras: 30 niveles de lógica, palabras y
patrones, pistas con penalización, puntaje y estrellas, logros, acertijo diario con racha,
anuncios propios del juego y guardado del progreso en el dispositivo. Se adapta a cualquier
tamaño de pantalla, en vertical u horizontal.

---

¿Eres desarrollador del equipo? Lee `PREGUNTAS_DESARROLLADORES.docx`.

---

## Abrir y jugar

1. En **Unity Hub → Add → Add project from disk** elige la carpeta `Mindlock`.
   (Versión del editor: **6000.0.84f1**.)
2. Abre la escena **`Assets/Scenes/Mindlock.unity`** y pulsa **Play**.
3. Prueba cualquier tamaño en la pestaña **Game** (por ejemplo 1080 × 1920, 1920 × 1080 o
   2048 × 1536) o en la pestaña **Simulator**.

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

El progreso se guarda en `Application.persistentDataPath/mindlock_datos.json`
(en Windows: `C:\Users\<usuario>\AppData\LocalLow\Mindlock\`) con las
**9 tablas del diccionario de datos** (`jugador`, `nivel`, `acertijo`, `pista`, `progreso`,
`intento`, `logro`, `jugador_logro`, `acertijo_diario`), con los mismos nombres de campo
y fechas en ISO 8601. Cada respuesta enviada (correcta o incorrecta) queda en `intento`,
y cada fecha tiene su acertijo fijo en `acertijo_diario`.

Se escribe primero en un temporal y se conserva una copia `.bak`: si el archivo se daña,
el juego recupera la copia.

Antes la carpeta era `LocalLow\Unicaribe\Mindlock` o `LocalLow\Grupo3\Mindlock`. La primera vez que se abre esta versión,
el juego mueve el progreso a la carpeta nueva y borra la vieja (sin pisar un progreso que ya
exista). Las opciones de PlayerPrefs no se mueven: en Windows vuelven una vez a sus valores
por defecto.

Unity guarda sus registros (Player.log) en `LocalLow\Mindlock\Mindlock`; el progreso va un nivel
más arriba (`ArchivoJuegoService.Carpeta()`).

**¿Por qué no SQLite todavía?** Unity no trae el motor nativo de SQLite para Android/iOS;
hay que añadir un plugin (por ejemplo `sqlite-net` con sus binarios). Como la interfaz
solo usa las interfaces de `Interfaces.cs`, pasar a SQLite es crear una clase
`SqliteJuegoService` con las mismas tablas y registrarla con `Servicios.Configurar(...)`;
no hay que tocar pantallas ni estilos.

Para probar la interfaz con perfiles de ejemplo (Ana y Carlos), marca **Usar Datos De Demo**
en el componente `UIManager` del objeto `UI` (en ese modo no se guarda nada).

## Idiomas (inglés y español)

El juego empieza en **inglés** y se cambia en **Settings > LANGUAGE** (Opciones > Idioma). La
elección se guarda en el dispositivo y, al cambiarla, todas las pantallas se vuelven a crear.

- Los textos se escriben en español en los UXML y en el código; la traducción está en
  `Scripts/Nucleo/TextosIngles.cs` (la clave es el texto en español).
- En el código se usa `Idioma.T("Texto")` o `Idioma.F("Te quedan {0} vidas", n)`.
- Los acertijos, sus pistas y los logros en inglés están en
  `Scripts/Datos/CatalogoAcertijos.Ingles.cs`, por id. Los juegos de palabras se adaptaron al
  inglés con otro acertijo equivalente (por ejemplo, el anagrama de ROMA es el de LISTEN).
- Las respuestas se aceptan en los dos idiomas, y en inglés también se ignoran «the», «a» y «an».
- **Mindlock > Probar lógica y guardado** avisa si falta una traducción o si una respuesta en
  inglés no coincide con sus opciones.

## Vidas y monedas

Los valores están en la clase `Economia` de `Scripts/Datos/Modelos.cs`.

- **Vidas:** cada perfil tiene 4. Una respuesta incorrecta o un tiempo agotado (Difícil y Experto)
  resta 1; el acertijo diario no gasta vidas. Se recupera 1 cada 50 minutos, aunque el juego esté
  cerrado. Con 0 vidas no se puede jugar un nivel: se abre una ventana para ver 2 anuncios
  (+1 vida), pagar 100 monedas (llena las 4) o esperar.
- **Monedas:** 10 por cada estrella nueva en un nivel, 50 por el acertijo diario, 50 por cada
  logro y 25 por anuncio. En el resultado se pueden duplicar con un anuncio. Se gastan en llenar
  las vidas (100) o comprar una pista (50). La tienda se abre al tocar las monedas del mapa.
- **Límite:** 10 anuncios con premio al día por perfil.
- **Guardado:** la tabla `jugador` tiene ahora `vidas`, `monedas`, `recarga_vida_desde`,
  `anuncios_para_vida`, `anuncios_hoy` y `fecha_anuncios`. Los archivos de antes empiezan con
  4 vidas y 0 monedas. Reiniciar el progreso de un perfil también reinicia sus vidas y monedas.

## Anuncios (Unity LevelPlay con Unity Ads y AdMob)

El juego tiene instalado **Ads Mediation (Unity LevelPlay) 9.6.0** con el SDK de LevelPlay 9.7.0,
los adaptadores de **Unity Ads** y **Google AdMob**, y el **Mobile Dependency Resolver** que baja
las librerías de Android e iOS. Hay tres tipos de anuncio:

- **Banner** abajo de todo, en todas las pantallas menos mientras se resuelve un acertijo.
- **Pantalla completa** al salir del resultado: como máximo cada 3 niveles y cada 2 minutos,
  nunca antes del nivel 3.
- **Con recompensa**: +1 vida (2 anuncios), +1 pista o +25 monedas, y duplicar las monedas de un
  nivel. El reloj del acertijo se detiene mientras dura.

**Qué se muestra en cada caso**

- **Android o iOS con las claves puestas** (`Scripts/Nucleo/ClavesAnuncios.cs`): anuncios reales
  de LevelPlay, que elige el que pague más entre Unity Ads y AdMob (`Scripts/UI/AnunciosLevelPlay.cs`).
- **Editor, web, Windows o sin claves**: anuncios propios del juego, que promocionan sus funciones
  y no generan dinero (`Scripts/UI/AnunciosDemo.cs`).
- Para apagar todos los anuncios, desmarca **Mostrar Anuncios** en el componente `UIManager`.

**Pasos para ganar dinero (los hace el dueño de la cuenta)**

1. En el panel de LevelPlay (platform.ironsrc.com), crea la app de Android (y la de iOS si la
   publican). Copia la **App key** y crea tres *ad units*: banner, intersticial y con recompensa.
2. Pega la App key y los tres ids en `Scripts/Nucleo/ClavesAnuncios.cs` (hay una sección para iOS
   y otra para Android).
3. En LevelPlay, en *SDK Networks*, activa **Unity Ads** (con el Game ID de tu proyecto en
   Unity Ads) y **AdMob** (con la app y los anuncios creados en tu cuenta de AdMob).
4. En Unity, en **Ads Mediation > Network Settings**, cambia los App ID de AdMob. Ahora tienen los
   **App ID de prueba públicos de Google**: sirven para compilar, pero no generan dinero.
5. Antes de compilar para Android: **Assets > Mobile Dependency Resolver > Android Resolver >
   Force Resolve**.
6. Para probar sin riesgo, registra tu teléfono como dispositivo de prueba en LevelPlay y pon
   `HerramientaDePrueba = true` en `ClavesAnuncios.cs` (abre el Test Suite al iniciar). Nunca
   toques tus propios anuncios reales: las redes pueden cerrar la cuenta.
7. Antes de publicar: política de privacidad, aviso de consentimiento para usuarios de Europa
   (`LevelPlay.SetConsent`), archivo `app-ads.txt` en tu sitio web, y si el juego es para niños,
   las reglas de Google Play para familias.

Las reglas de frecuencia están en `Scripts/Nucleo/Anuncios.cs`.

## Pantallas de cualquier tamaño

La interfaz se escala para medir siempre al menos 390 × 760 (`MindlockPanelSettings.asset`,
modo *Expand*). En pantallas anchas la app queda como una columna centrada de hasta 600 de
ancho (`.app` en `Raiz.uss`). El teléfono puede girar, la ventana de Windows se puede agrandar
y la versión web ocupa toda la ventana del navegador.

## Lo que se agregó a los recursos originales

- **Guardado real en el dispositivo** (antes los datos se perdían al cerrar).
- **Historial de intentos** (tabla `intento`) y **acertijo diario fijo por fecha**.
- **Sonido**: efectos (toque, acierto, error, pista, candado, estrellas, logro, tiempo
  agotado) y música ambiental en bucle, generados por código. Respetan los interruptores
  de Música y Efectos de Opciones.
- **Paletas de color** en Opciones → Colores: *Clásica*, *Océano* y *Aurora*, cada una con
  versión oscura y clara.
- **14 acertijos diarios nuevos** (ahora son 21, uno distinto cada día durante 3 semanas).
- Escena, Panel Settings y ajustes para cualquier pantalla ya configurados; 60 FPS.
- **Anuncios** con Unity LevelPlay (Unity Ads y AdMob), **vidas**, **monedas** y **tienda**, y **diseño adaptable**.
- Pruebas automáticas de lógica y guardado, y menús para compilar.

## Niveles (configuración normal)

Los niveles 1 a 15 son los originales. Del 16 al 30 los acertijos son difíciles y cada uno es
más complejo que el anterior: los elige `IdsNivelesDificiles` en
`Scripts/Datos/CatalogoAcertijos.Progresivo.cs`, de entre los originales y los del modo desafío.
Las pistas restan lo de siempre: Fácil 50 / 75 / 100, Media 75 / 100 / 150, Difícil
100 / 150 / 200, Experto 150 / 200 / 300 y diario 30 / 50 / 75.

## Modo desafío (desactivado)

Sirvió para probar un escenario muy difícil: del nivel 5 al 30 usa los acertijos de
`Scripts/Datos/CatalogoAcertijos.Desafio.cs` y las pistas restan más (Fácil 100 / 150 / 200,
Media 150 / 250 / 350, Difícil 250 / 350 / 450, Experto 350 / 500 / 650, diario 60 / 100 / 150).
Para activarlo, marca **Modo Desafio** en el componente `UIManager` del objeto `UI`.

Los 30 acertijos originales siguen intactos en `CatalogoAcertijos.AcertijosOriginales()` (y hay
una copia del archivo en `Respaldo/`). El progreso guardado se conserva al cambiar de
configuración; solo cambia qué acertijo tiene cada nivel.

## Respuestas para probar

Nivel 1 → `10` · Nivel 2 → `reloj` · Nivel 3 → *Misi es un animal* · Nivel 9 → `9` ·
Nivel 15 → *Solo manzanas* · Nivel 16 → `95`, `191` · Nivel 30 → `28`.
Las respuestas de texto ignoran mayúsculas, acentos y artículos («El reloj» = «reloj»).

## Reglas

- Fácil 1–8 (500 pts), Media 9–15 (800), Difícil 16–24 (1 000, límite 3:00), Experto 25–30 (1 500, límite 4:00).
- Puntaje = base − tiempo extra − intentos fallidos − pistas (mínimo 10 % de la base).
  3★ con ≥ 85 %, 2★ con ≥ 60 %, si no 1★.
- Empiezas con 3 pistas; ganas +1 con el acertijo diario y +1 al sacar 3★ por primera vez (máximo 9).
  También se consiguen con un anuncio o por 50 monedas.
- 4 vidas: cada fallo resta una y se recupera una cada 50 minutos (ver «Vidas y monedas»).
- Acertijo diario: +150 pts de bonus; la racha sube si jugaste ayer y se reinicia si faltaste un día.
