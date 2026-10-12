# Mishi, el gato animado (kit para los niveles)

Mishi es el gato del laboratorio. Pasea, salta entre repisas, maúlla, duerme (y ronronea), y lanza una bola de luz mágica.
Se anima sola: no hace falta tocar nada más que crearla en tu nivel.

## Qué hay que copiar a tu proyecto

| Qué | Dónde |
|---|---|
| `MishiAnimado.cs` y `BolaMagica.cs` | `Assets/Scripts/Gatitos/` |
| Los 37 dibujos del gato | `Assets/Resources/FluxLab/Mishi/` (la carpeta entera, con sus `.meta`) |
| Sonidos: `gato_1.mp3`, `gato_2.mp3`, `gato_3.mp3` y `ronroneo.mp3` | `Assets/Resources/FluxLab/Audio/` |
| El importador de imágenes | `Assets/Editor/MishiImportador.cs` |

Lo único que usa del resto del juego es `AjustesJuego` (para saber si los efectos de sonido están activados) y el sonido
`hechizo.mp3`. Los dos ya existen en el juego de Brenda.

## Cómo ponerla en tu nivel

Una sola línea, cuando armes tu pantalla (necesitas el `Transform` de tu lienzo/Canvas):

```csharp
var mishi = MishiAnimado.Crear(miLienzo, misPlataformas);
```

`misPlataformas` son los sitios donde puede pararse Mishi en TU fondo. Cada uno tiene: de dónde a dónde (`izq`, `der`)
y a qué altura (`y`), en coordenadas del lienzo de 1920x1080 (el centro es 0,0). La primera es donde empieza.

```csharp
var misPlataformas = new[]
{
    new MishiAnimado.Plataforma(380f, 530f, 254f),     // una repisa
    new MishiAnimado.Plataforma(-250f, 640f, -170f)    // la mesa
};
```

Si no pasas plataformas, usa una sola en el centro.

## Qué puedes pedirle desde tu código

| Llamada | Qué hace |
|---|---|
| `mishi.Celebrar()` | Salta de alegría (por ejemplo, cuando el jugador acierta) |
| `mishi.Maullar()` | Maúlla |
| `mishi.Lanzar()` | Lanza la bola mágica ya |
| `mishi.Distraer()` | Sube a una repisa y lanza la bola en alto (para distraer) |
| `mishi.DistraerVarias(3)` | Lo mismo, pero con varias bolas seguidas |
| `mishi.Tranquila = true` | Solo descansa, sin maullar ni travesuras (`false` la despierta) |
| `mishi.AlAterrizar = (x, y) => ...` | Aviso opcional cada vez que aterriza; si devuelves `true`, ella maúlla del susto |

## Para probar en el editor

Con el juego en Play: F1 lanza la bola, F2 maúlla, F3 duerme, F4 viaja a otra plataforma, F5 salta.
(Solo funcionan dentro del editor de Unity, no en el juego final.)

## Ajustes que puedes cambiar

En `MishiAnimado.cs`: `escala` (tamaño), `velocidad`, `alturaSalto`, `volumenRonroneo` y `volumenHechizo`.
