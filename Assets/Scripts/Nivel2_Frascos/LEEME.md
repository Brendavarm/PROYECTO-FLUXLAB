# Nivel 2 — Pasillo de Frascos

Trabajo de **Nicol (rama `nikki`)**. Es la mesa del nivel 1 (Aprendiz) con más dificultad.

## Dónde está cada cosa

```text
Nivel2_Frascos/
├── LEEME.md
├── Juego/      MesaDeFrascos (flujo), ConfigFrascos (Inspector), RecetaFrascos (reglas, estrellas), VidasFrascos
├── Pantalla/   EscenaFrascos (HUD, receta, bandeja, ventanas, mensajes), BienvenidaFrascos (bienvenida,
│               conteo, aviso espejo, mensaje final), ResultadoFrascos (pantalla de resultados),
│               MaderaColgante (maderas que caen y se balancean)
├── Caldero/    FondoNivel2 (medidas del fondo), FondoVivo (vida del fondo), CalderoVivo, CuerpoCaldero, FrascoVertido, ParticulasUI
├── Mishi/      GatoMishi
├── Magnus/     PosesMagnus, AspectoPoses, ProcesoImagenMagnus, MagnusAnimado  (biblioteca de animaciones)
│   └── Galeria/  GaleriaMagnus (escena de prueba)
├── Imagenes/   ImagenesFrascos (frascos sin fondo)
├── Editor/     RegistrarEscenaFrascos, ImportarImagenesNivel2
└── Resources/Nivel2/   sonidos del nivel
```

Escenas: `Assets/Scenes/Nivel2_Frascos.unity` (el nivel) y `Assets/Scenes/Nivel2_GaleriaMagnus.unity` (para ver las animaciones de Magnus).

Imágenes:
- Fondo: `Assets/Resources/FluxLab/NIVEL2-IMAGENES/FondoN2.jpg`
- Frascos: `Assets/Resources/FluxLab/NIVEL2-IMAGENES/FRAZCOS/`
- Figuras y burbujas: `Assets/Resources/FluxLab/NIVEL2-IMAGENES/figuras/`. **Solo en este nivel** reemplazan a `gem_*.png` y `burbuja_*.png` (las de Brenda no se tocan): CirculoRojo = Fuego, TrianguloAzul = Gota, EstrellaAmarillo = Polvo, CuadradoVerde = Hierba, PentagonoLila = Cristal, LunaCobre = Lágrima; Burbuja reemplaza a las burbujas y BurbujasGrupo sube de vez en cuando por el fondo. Si falta una imagen, se usa la original. Cruz.Plata, GemaEsmeralda y los octágonos están guardados para más adelante.
- Poses de Magnus: `Assets/StreamingAssets/Personaje/Imagenes-animacion/` (con gafas = `...G.jpg`). Magnus de pie es `Magnuss.jpg` / `MagnussG.jpg` (ya no se usa `magnus.png`).

## Qué cambia respecto al nivel 1

| Nivel 1 (Aprendiz) | Nivel 2 (Frascos) |
|---|---|
| La receta se ve completa | Los ingredientes se encienden **de a uno** |
| Sin repetidos | Puede repetir ingredientes |
| El error se sabe al final | Se revisa **en el momento**; cada error rompe un **frasco de vida** |
| Bandeja fija | La bandeja **se baraja** cada ronda (las teclas 1-6 siguen la posición) |
| — | La **última ronda es espejo**: se pone al revés |
| 3 ingredientes, 3 rondas | 4 ingredientes, 4 rondas |

**Pasar de nivel:** se necesitan `estrellasParaAvanzar` estrellas (2 por defecto) y no romper todos los frascos.

## Fondo

- `FondoN2.jpg`, con una viñeta suave que oscurece las orillas.
- **Todas las medidas del fondo están en `Caldero/FondoNivel2.cs`**: dónde está pintado el caldero (para recortarlo y animarlo), la boca (líquido y frascos que caen) y dónde se para Magnus. Si se cambia el fondo, solo se ajustan esos números.

**Fondo vivo** (`Caldero/FondoVivo.cs`): burbujas mágicas que suben y a veces revientan, faroles con resplandor que titila, la luna que brilla con estrellitas en la ventana, polvo mágico dorado y destellos en los frascos de las repisas. Además, `Caldero/FrascosDelFondo.cs` recorta varios frascos pintados (sus posiciones están en `FondoNivel2.FrascosPintados`) y los hace reaccionar a cómo juegas: mientras se juega solo se mecen sutil (a veces uno tintinea o le sube una burbujita); **al fallar** varios de las repisas se caen en cadena y se rompen y luego reaparece por magia; **al acertar una receta** ¡todos festejan! (saltan, algunos dan una vuelta o vuelan en loop, brillan y sueltan destellos). Se apaga con `frascosReaccionan`. Todo se apaga con `fondoVivo` y las burbujas se regulan con `burbujasFondo` en el Inspector.

## Magnus

**Biblioteca de animaciones** (`Magnus/MagnusAnimado.cs`): Quieto, Observar, Remover, Reír, Sorprenderse, Asustarse, Saltar, Bailar, Cantar, Meditar, Pensar, Enojarse, Triste, Lanzar, Volar, y tres finales: Alegre (baila y salta), Contento (se ríe) y Ánimo (triste con corazones).

**En el juego solo se usan:**
- **Riendo:** ingrediente correcto y receta completa.
- **Sorprendido:** cuando se equivoca.
- **Al terminar** (mensaje final y pantalla de resultados): **Alegre** si fue perfecto (3 estrellas), **Contento** si pasó, **Ánimo** si no pasó.

Las demás quedan listas. Para verlas: abrir `Nivel2_GaleriaMagnus.unity` y dar Play (un botón por animación y controles de pelo, túnica y gafas; no guarda nada).

**Movimiento natural:** las poses cambian con un fundido corto; posición, giro y tamaño siguen a su objetivo con resortes (aceleran, se pasan un poquito y se acomodan); hay anticipación (se agacha antes de saltar o sorprenderse) y rebote al caer; al bailar se da vuelta suave cada dos pasos; en reposo respira y pasa el peso de un pie al otro.

**Personalización:** las poses se tiñen con el pelo y la túnica que el jugador eligió en "Personalizar" (`AjustesJuego.Pelo` y `ColorTunica`). La zona de la cabeza de cada pose está en `PosesMagnus.cs`.

**Gafas (para Brenda):** "Personalizar" todavía no tiene opción de gafas. El código ya está listo: si se guarda en PlayerPrefs la clave **`flux.gafas`** (1 = con gafas, 0 = sin gafas), Magnus usa las poses con o sin gafas. Si la clave no existe, lleva gafas.

**Fondo gris de las poses:** `ProcesoImagenMagnus.cs` lo borra al cargarlas, usando el borde blanco tipo sticker que rodea a Magnus. Las 12 poses con gafas quedan limpias. En 3 sin gafas (Saltando, Sorprendido, Volando) se pierde un poco de pelo, porque ahí el pelo no tiene borde que lo separe del fondo. Exportarlas en PNG transparente lo arreglaría.

## Entrada al nivel

- **Estilo:** maderas y pergamino tallados de la Mesa del Aprendiz / Jardín de Luna (`Resources/FluxLab/Nivel3/UI`: `boton_ancho`, `placa_titulo`, `placa_ronda`, `boton_cuadrado`, `pergamino`, `bandeja`) y la letra `LilitaOne`. Solo se usan las imágenes; se eligen en `EscenaFrascos` (`Madera`, `Pergamino`, `Placa`, `PlacaRonda`, `Redondo`). Si faltan, vuelve a las del menú (`StreamingAssets/Menu/boton.png` y `pergamino.png`).
- **Bandeja:** cada tabla de color se recorta de `bandeja.png` y viaja con su figura cuando la bandeja se baraja (`LugarEnBandeja`, `TablaDeColor`).
- **Mishi:** `GatoMishi` llama al gato animado de Sasha (`Gatitos/MishiAnimado`, sin cambiar su código): entra saltando, maúlla con una notita de pergamino y destellos dorados, y mientras espera hace travesuras (pasea, lanza su bola mágica o salta). Si le haces clic, maúlla. Si faltan los dibujos de Sasha, usa la imagen quieta de antes.
- **Bienvenida** (solo la primera vez), **conteo** 3, 2, 1 y **aviso de ronda espejo** antes de la última ronda.
- Se activan o desactivan en el Inspector: `mostrarBienvenida`, `conteoInicial`, `anunciarEspejo`. El aviso de espejo y el mensaje final duran `segundosMensajesGrandes`.

## Pantalla

- **HUD de madera y pergamino:** letrero colgante, PAUSA, SALIR (pregunta antes), botones de música y sonidos, marcador en pergamino, frascos de vida, repisa con cartas de figuras, VACIAR.
- **Pergamino de la receta:** crece y brilla mientras se dicta; la barra marca el tiempo y luego el avance ("Tu turno · 2 / 4").
- **Mensajes de ánimo:** "¡Bien!", "¡Perfecto!", "¡Oh nooo!" y el mensaje final de motivación (`mensajesAnimo`).

## Resultados

- `Pantalla/ResultadoFrascos.cs`: madera y pergamino, todo animado en orden: el título se desenrolla, los paneles entran desde los lados, las estrellas caen una por una, el puntaje cuenta hacia arriba (sello rojo si es **nuevo récord**), el desempeño aparece fila por fila, sube la poción ganada, aparece Magnus (alegre, contento o dando ánimo) y suben los botones.
- Un clic acelera la secuencia. Si pasó el nivel, aparecen destellos blancos y dorados que titilan y luciérnagas doradas que suben (en vez de confeti), y "SIGUIENTE NIVEL" late.

## Caldero

- **Echar el frasco:** vuela desde la carta, se inclina y cae un chorro del color del ingrediente.
- **Caldero vivo:** líquido en perspectiva con remolino, ondas, hervor, aura de luz, lucecitas en espiral y burbujas grandes.
- **El caldero se mueve:** respira, rebota, se sacude al hervir, se tambalea con un error y baila al acertar.
- **Reacción por ingrediente** y **combinaciones** (*¡Hervor doble!*, *¡Vapor!*, *¡Lluvia de destellos!*). Para agregar otra: `CalderoVivo.cs` → `Combinar`.

## Frascos

| Ingrediente | Frasco |
|---|---|
| Fuego | FrazcoRojo |
| Gota | FrazcoAzul |
| Polvo | FrazcoAmarillo |
| Hierba | FrazcoVerde |
| Cristal | FrazcoLila |
| Lágrima | FrazcoNaranja |
| Vidas | FrazcoRosa |
| Premio del resultado | `StreamingAssets/Menu/pocion.png` (FrazcoUnicornio solo si falta) |

## Cambiar la dificultad sin código

1. Abrir `Assets/Scenes/Nivel2_Frascos.unity`.
2. En la Jerarquía seleccionar **MesaDeFrascos**.
3. En el Inspector desplegar **Config** y mover los valores.
4. Guardar la escena (Ctrl+S).

## Lo único que se tocó fuera de esta carpeta

- `Scripts/Menu/MapaDeNiveles.cs` → `Entrar()`: el nivel 2 carga `Nivel2_Frascos`.
- `Scripts/Mesa/MesaDeAlquimia.cs` → `IrAlSiguiente()`: desde el nivel 1, "Siguiente nivel" carga `Nivel2_Frascos`.
