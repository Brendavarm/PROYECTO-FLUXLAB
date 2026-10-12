# Mishi, el gato animado: cómo llevarla a tu nivel

Mishi es el gato del laboratorio. Pasea, salta entre repisas, maúlla, duerme (y ronronea) y lanza una bola de luz mágica.
Se anima sola: solo tienes que **traer sus archivos** a tu rama y **crearla en tu nivel con una línea**.

> Este kit lo hizo Sasha (rama `sasha`). Si algo no funciona, escríbele antes de tocar el código de Mishi.

---

## Qué trae el kit

| Qué | Dónde queda en tu proyecto |
|---|---|
| `MishiAnimado.cs` y `BolaMagica.cs` | `Assets/Scripts/Gatitos/` |
| Los 37 dibujos del gato | `Assets/Resources/FluxLab/Mishi/` |
| Sonidos del gato: `gato_1.mp3`, `gato_2.mp3`, `gato_3.mp3` y `ronroneo.mp3` | `Assets/Resources/FluxLab/Audio/` |
| El importador de imágenes (las deja nítidas) | `Assets/Editor/MishiImportador.cs` |

Lo único que necesita del juego de Brenda es `AjustesJuego` y el sonido `hechizo.mp3`. Los dos ya están en `main`.

---

## Paso 1. Prepara tu rama

1. Abre la terminal en la carpeta del proyecto.
2. Comprueba que estás en **tu** rama (no en `main`):
   ```
   git branch --show-current
   ```
3. Guarda lo que tengas a medias. Lo ideal es tener todo subido (commit) antes de traer el kit:
   ```
   git status
   ```
   Si salen archivos tuyos en rojo o en verde, haz primero tu commit.

## Paso 2. Trae los archivos del kit

Este comando copia **solo** los archivos de Mishi desde la rama `sasha`. No mezcla nada de otros niveles ni toca tu código:

```
git fetch origin
git checkout origin/sasha -- Assets/Scripts/Gatitos Assets/Scripts/Gatitos.meta Assets/Resources/FluxLab/Mishi Assets/Resources/FluxLab/Mishi.meta "Assets/Resources/FluxLab/Audio/gato_*" "Assets/Resources/FluxLab/Audio/ronroneo*" Assets/Editor/MishiImportador.cs Assets/Editor/MishiImportador.cs.meta
```

Revisa qué llegó:
```
git status
```
Deben aparecer como archivos nuevos solo las rutas de la tabla de arriba. Si todo se ve bien, súbelo a tu rama:
```
git commit -m "Traigo el kit de Mishi"
git push origin TU-RAMA
```
(cambia `TU-RAMA` por el nombre de tu rama).

## Paso 3. Abre Unity

1. Vuelve a Unity y espera a que termine de importar (la barra de abajo a la derecha).
2. Mira la pestaña **Console**: no debe haber errores rojos.
3. Si los dibujos de Mishi salen en blanco o borrosos: en la ventana *Project*, haz clic derecho sobre
   `Assets/Resources/FluxLab/Mishi` y elige **Reimport**.

## Paso 4. Ponla en tu nivel

Mishi se dibuja dentro de un **Canvas** (el lienzo de tu pantalla). Necesitas una sola línea, en el código donde armas tu pantalla,
**después** de crear tu lienzo y tu fondo:

```csharp
var mishi = MishiAnimado.Crear(miLienzo, misPlataformas);
```

- `miLienzo` es el `Transform` de tu Canvas (o de un panel dentro de él).
- `misPlataformas` son los sitios donde puede pararse Mishi **en tu fondo**.

### Cómo definir las plataformas

Cada plataforma es una repisa, mesa o estante. Se define con tres números: de dónde a dónde (`izq` y `der`) y a qué altura (`y`).
Las medidas son del **lienzo de 1920 x 1080**, con el centro en (0, 0): a la derecha y arriba son positivos.

```csharp
var misPlataformas = new[]
{
    new MishiAnimado.Plataforma(380f, 530f, 254f),     // una repisa: de x=380 a x=530, a altura 254
    new MishiAnimado.Plataforma(-250f, 640f, -170f)    // la mesa
};
```

La **primera** de la lista es donde empieza Mishi. Si no pasas ninguna, usa una sola plataforma en el centro.

### Cómo sacar los números de tu fondo

Abre tu imagen de fondo en un editor y mira en qué píxel está la superficie donde quieres que pise Mishi.
Con una imagen de ancho `W` y alto `H` (en 16:9):

```
x del lienzo = (x del píxel − W / 2) × 1920 / W
y del lienzo = (H / 2 − y del píxel) × 1080 / H
```

Haz eso para el borde izquierdo y derecho de la superficie (`izq` y `der`) y para su altura (`y`).

### Si tu Canvas no es de 1920 x 1080

Las plataformas se miden con esa resolución de referencia (la que usa el juego de Brenda). Si tu Canvas usa otra,
convierte los números en proporción.

### Para que quede en el orden correcto

Mishi se crea **encima de todo lo que ya existe** en ese lienzo. Si quieres que quede detrás de tus paneles o botones:
```csharp
mishi.transform.SetSiblingIndex(0);   // 0 = al fondo, delante del fondo si este se creó antes
```

## Paso 5. Pruébala

1. Pulsa **Play** y entra a tu nivel.
2. Mishi debe aparecer en la primera plataforma y empezar a moverse.
3. Atajos solo para probar (funcionan dentro del editor de Unity, no en el juego final):

   | Tecla | Qué hace |
   |---|---|
   | F1 | Lanza la bola mágica |
   | F2 | Maúlla |
   | F3 | Se duerme |
   | F4 | Viaja a otra plataforma |
   | F5 | Salta |

---

## Qué le puedes pedir desde tu código

| Llamada | Qué hace |
|---|---|
| `mishi.Celebrar()` | Salta de alegría (por ejemplo, cuando el jugador acierta) |
| `mishi.Maullar()` | Maúlla |
| `mishi.Lanzar()` | Lanza la bola mágica ya |
| `mishi.Distraer()` | Sube a una repisa y lanza la bola en alto (para distraer al jugador) |
| `mishi.DistraerVarias(3)` | Lo mismo, con varias bolas seguidas |
| `mishi.Tranquila = true` | Solo pasea, duerme y descansa, **sin ruido ni travesuras** (`false` la despierta) |
| `mishi.AlAterrizar = (x, y) => ...` | Aviso opcional cada vez que aterriza; si devuelves `true`, ella maúlla del susto |
| `mishi.X` y `mishi.PisoY` | Dónde está parada ahora |

Con `Tranquila = true` sigue recorriendo tu escenario sin hacer ruido. Es lo ideal para que el nivel no se sienta cargado;
puedes despertarla un rato (por ejemplo, un turno de 10 segundos) y volver a calmarla.

## Ajustes

En `MishiAnimado.cs` hay variables públicas que puedes cambiar: `escala` (tamaño), `velocidad`, `alturaSalto`,
`volumenRonroneo` y `volumenHechizo`.

---

## Problemas comunes

| Qué pasa | Qué hacer |
|---|---|
| `git checkout` dice `pathspec ... did not match` | Primero ejecuta `git fetch origin`. Comprueba que escribiste las rutas tal cual. |
| Mishi sale como un cuadro blanco | `Reimport` de la carpeta `Resources/FluxLab/Mishi` (Paso 3). |
| No aparece Mishi | Revisa la Console. Comprueba que `miLienzo` no sea `null` y que el Canvas esté activo. |
| Está flotando o hundida en la repisa | Ajusta la `y` de esa plataforma (sube o baja el número). |
| Se sale de la repisa | Ajusta `izq` y `der` de esa plataforma. |
| Error `AjustesJuego` no existe | Tu rama no tiene el código de Brenda. Trae primero los cambios de `main`. |
| Se oye demasiado | `mishi.Tranquila = true`, o baja `volumenRonroneo` y `volumenHechizo`. |

## Qué NO traer

Este kit es **solo Mishi**. No necesitas nada de `Assets/Scripts/Nivel3/`: eso es el nivel 3 de Sasha (con su grillo, su mariposa y sus frascos).
