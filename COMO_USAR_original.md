# Mindlock – Interfaz completa en Unity (UI Toolkit)

Esta carpeta trae **toda la interfaz** de Mindlock lista para usar en Unity: las 9 pantallas
del diseño, la navegación entre ellas, los íconos y datos de prueba para poder jugar
sin la base de datos.

- **Versión de Unity:** 2022.3 LTS o Unity 6 (necesita Painter2D, disponible desde 2022.1).
- **Sistema de UI:** UI Toolkit (UXML + USS + C#).
- **Imágenes:** no hacen falta. Todos los íconos (candados, estrellas, bombilla, llama, trofeo…)
  se dibujan con código, así que se ven nítidos en cualquier pantalla y cambian de color con el tema.

> Esta versión **reemplaza** los archivos anteriores (`PerfilScreen.*`, `PerfilScreenController.cs`,
> el `MindlockTheme.uss` viejo y las imágenes `.png`). No los copies al proyecto:
> `PerfilScreenController.cs` tiene un error que impide compilar.

---

## 1. Instalar (5 minutos)

1. Copia la carpeta **`Assets/Mindlock`** dentro de la carpeta `Assets` de tu proyecto de Unity.
2. Crea el Panel Settings:
   - Clic derecho en `Assets/Mindlock` → **Create → UI Toolkit → Panel Settings Asset** → nómbralo `MindlockPanelSettings`.
   - En el Inspector:
     - **Theme Style Sheet:** `Assets/Mindlock/Resources/Mindlock/Estilos/MindlockTheme.tss`
     - **Scale Mode:** Scale With Screen Size
     - **Reference Resolution:** **390 × 844** (el tamaño del diseño; así los px del USS coinciden con el mockup)
     - **Screen Match Mode:** Match Width Or Height → **Match = 0.5**
3. En la escena: **GameObject → UI Toolkit → UI Document**. Renómbralo `UI`.
   - **Panel Settings:** `MindlockPanelSettings`
   - **Source Asset:** déjalo **vacío** (el script carga las pantallas solo).
4. En ese mismo GameObject: **Add Component → UIManager**.
5. Pulsa **Play**.

Para verlo como en un teléfono: pestaña **Game** → resolución 1080 × 1920 (Portrait),
o la pestaña **Simulator** con un iPhone/Android (muestra el notch y el área segura).

> Si olvidas el paso 2, el `UIManager` crea un Panel Settings automático y avisa en la consola,
> pero es mejor tener el asset.

---

## 2. Qué vas a ver al darle Play

El `UIManager` trae **datos de demostración** (casilla *Usar Datos De Demo* en el Inspector):

| Perfil | Situación |
|---|---|
| **Ana** | 14 niveles completados, nivel 15 listo para jugar, racha de 5 días, acertijo de hoy pendiente |
| **Carlos** | 6 niveles completados, sin racha |

Recorrido para probar todo:

1. **Perfil** → toca *Ana* (o crea un perfil nuevo con *+ Crear perfil*; prueba dejarlo vacío para ver el error).
2. **Mapa** → el nivel 15 late en turquesa. Toca un nivel completado o el 15 para ver la hoja de detalle. Toca un candado gris (se sacude).
3. **Juego** → responde mal a propósito (sacudida + "Casi… inténtalo de nuevo"), pide una pista (modal con confirmación y descuento de puntos), pausa.
4. **Resultado** → candado que se abre, estrellas una por una, desglose del puntaje, "Nivel 16 desbloqueado".
5. **Acertijo diario** (tarjeta ámbar del mapa) → resuélvelo y verás la racha subir a 6, el calendario semanal y la cuenta regresiva.
6. **Logros** y **Opciones** (barra inferior). En Opciones cambia a **tema Claro** o **texto Grande** y todo se actualiza al instante.

Respuestas útiles para probar: nivel 1 → `10`, nivel 2 → `reloj`, nivel 15 → *Solo manzanas*,
nivel 16 → `36`. Las respuestas de texto ignoran mayúsculas, acentos y artículos ("El reloj" = "reloj").

---

## 3. Qué hay en cada carpeta

```
Assets/Mindlock/
├── Resources/Mindlock/
│   ├── Pantallas/               ← estructura de cada pantalla (UXML)
│   │   ├── Raiz.uxml            contenedor + barra inferior + aviso emergente
│   │   ├── Perfil.uxml          1. Inicio y perfil
│   │   ├── Mapa.uxml            2. Mapa de 30 niveles
│   │   ├── Juego.uxml           3, 4, 5 y 7. Acertijo + pista + error + pausa + modo diario
│   │   ├── Resultado.uxml       6. Nivel completado
│   │   ├── Diario.uxml          7. Acertijo diario
│   │   ├── Logros.uxml          8. Logros y puntaje
│   │   └── Opciones.uxml        9. Opciones
│   └── Estilos/                 ← apariencia (USS)
│       ├── MindlockTheme.tss    tema que se asigna al Panel Settings (importa todo lo demás)
│       ├── MindlockTheme.uss    colores, modo claro, tamaños de texto, botones, modales…
│       └── <Pantalla>.uss       estilos propios de cada pantalla
└── Scripts/
    ├── Datos/                   ← CAPA DE DATOS
    │   ├── Modelos.cs           Jugador, Nivel, Acertijo, Pista, Logro, Progreso…
    │   ├── Interfaces.cs        contratos que usa la UI (IPerfilService, IProgresoService…)
    │   ├── CatalogoAcertijos.cs 30 acertijos + 7 diarios + 9 logros (contenido de prueba)
    │   └── MemoriaJuegoService.cs  implementación en memoria (temporal, sin SQLite)
    ├── Logica/
    │   └── CalculadoraPuntaje.cs   puntaje, descuentos y estrellas
    ├── Nucleo/
    │   └── Servicios.cs         Servicios, Sesion, ParametrosJuego, Preferencias (PlayerPrefs)
    └── UI/                      ← CAPA DE PRESENTACIÓN
        ├── UIManager.cs         único MonoBehaviour: navegación, tema, área segura, avisos
        ├── Pantalla.cs          clase base de las pantallas
        ├── Iconos.cs            23 íconos vectoriales
        ├── UIUtil.cs            modales, sacudida, interruptores, área segura…
        ├── Formato.cs           fechas, puntos y tiempos en español
        └── Pantallas/           un controlador por pantalla
```

Las carpetas `Resources` son necesarias: `UIManager` carga los UXML y el tema por nombre.

---

## 4. Cómo modificar

| Quiero cambiar… | Dónde |
|---|---|
| Colores, modo claro, tamaños de letra | Variables al inicio de `MindlockTheme.uss` (`--color-acento`, `--fuente-md`…) |
| Posición o textos fijos de una pantalla | Su `.uxml` (doble clic lo abre en el **UI Builder**) |
| Apariencia de una pantalla | Su `.uss` |
| Acertijos, pistas y respuestas | `CatalogoAcertijos.cs` |
| Reglas de puntaje y estrellas | `CalculadoraPuntaje.cs` (base − tiempo extra − intentos − pistas; 3★ ≥ 85 %, 2★ ≥ 60 %) |
| Logros | `CatalogoAcertijos.Logros()` y el método `Progreso()` de `MemoriaJuegoService.cs` |

Usar un ícono en UXML: `<ui:VisualElement class="ico ico-trofeo ico-lg color-acento" />`
(lista completa en `Iconos.Disponibles`; añade `ico--relleno` para la versión rellena).

**Fuente propia (opcional):** descarga Poppins o Inter (`.ttf`), clic derecho → *Create → Text Core → Font Asset*,
y en `MindlockTheme.uss`, dentro de `.app`, agrega
`-unity-font-definition: url("project://database/Assets/Ruta/Poppins-Regular SDF.asset");`

---

## 5. Conectar la base de datos SQLite

La interfaz **nunca** toca la base de datos directamente; solo usa las interfaces de `Interfaces.cs`.
Cuando la capa de datos esté lista:

1. Crear `SqliteJuegoService` que implemente `IPerfilService, IProgresoService, IAcertijoService, IDiarioService, ILogroService`
   (puede copiar la lógica de `MemoriaJuegoService` y cambiar las listas por consultas).
2. Registrarlo antes de que arranque la UI, por ejemplo en un script con orden de ejecución anterior:
   ```csharp
   var db = new SqliteJuegoService(Application.persistentDataPath + "/mindlock.db");
   Servicios.Configurar(db, db, db, db, db);
   ```
3. Desmarcar *Usar Datos De Demo* en el `UIManager`.

No hay que modificar ningún UXML, USS ni pantalla.

> Importante: hoy `UsarPista` y `RegistrarNivel` modifican el mismo objeto `Jugador` que tiene la sesión.
> Con SQLite, después de guardar, actualiza también `Sesion.Jugador` (o vuelve a leerlo) para que el mapa
> muestre el puntaje y las pistas al día.

---

## 6. Reglas implementadas

- 30 niveles: Fácil 1–8 (500 pts), Media 9–16 (800), Difícil 17–24 (1 000, límite 3:00), Experto 25–30 (1 500, límite 4:00).
- Pistas: hasta 3 por acertijo, con penalización creciente. El jugador empieza con **3 pistas** en total;
  gana +1 al resolver el diario y +1 al sacar 3 estrellas por primera vez en un nivel (máximo 9).
- Acertijo diario: uno por fecha, +150 pts de bonus, racha que sube si jugó ayer y se reinicia si faltó un día.
- Nombre de perfil obligatorio, máximo 30 caracteres y sin repetir.
- Todo funciona sin conexión; sin anuncios ni compras.

---

## 7. Pendiente / limitaciones conocidas

- **Datos en memoria:** el progreso se pierde al cerrar el juego hasta conectar SQLite (las *Opciones* sí se guardan con PlayerPrefs).
- **Sonido:** los interruptores de música y efectos guardan la preferencia, pero aún no hay audios.
- **Botón atrás de Android:** funciona si el proyecto usa el *Input Manager* clásico (o "Both").
- **Tema automático:** usa claro de 7:00 a 19:00 y oscuro el resto del día.
- El código se verificó compilándolo contra la API de UI Toolkit y probando la lógica (puntaje, racha,
  logros, respuestas de los 30 acertijos), pero la primera prueba visual real es en tu Unity:
  si algo se ve distinto, revisa la consola; cada error indica el archivo y el elemento.

---

## Problemas comunes

| Síntoma | Solución |
|---|---|
| Pantalla en blanco | El UI Document no tiene Panel Settings, o falta el componente `UIManager`. |
| Se ve con el estilo gris de Unity | El Panel Settings no usa `MindlockTheme.tss` como Theme Style Sheet. |
| `No se encontró Mindlock/Pantallas/...` | La carpeta `Resources` se movió o se renombró: debe quedar `…/Resources/Mindlock/Pantallas/`. |
| Todo se ve muy pequeño o muy grande | Reference Resolution debe ser 390 × 844 y Scale Mode *Scale With Screen Size*. |
| Error de compilación con `PerfilScreenController` | Borra los archivos viejos `PerfilScreen*` del proyecto. |
| Los íconos no aparecen | Unity anterior a 2022.1 (no tiene Painter2D). Actualiza a 2022.3 LTS o Unity 6. |
