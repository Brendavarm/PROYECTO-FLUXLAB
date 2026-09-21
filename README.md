# Laboratorio de Alquimia — FLUXLAB

Bienvenido al repositorio oficial de **Laboratorio de Alquimia**, un videojuego 2D de estimulación cognitiva desarrollado en **Unity** por el equipo **FLUXLAB**.

---

## Descripción

**Laboratorio de Alquimia** es un videojuego de puzles enfocado en el entrenamiento de la **memoria de trabajo** y la **atención selectiva**.

El jugador asume el papel de **Magnus**, un joven aprendiz de alquimia que debe preparar diferentes pociones siguiendo correctamente las recetas que aparecen durante unos segundos.

La principal mecánica consiste en **memorizar una secuencia de ingredientes y reproducirla en el orden correcto**, mientras el entorno presenta diferentes elementos que pueden distraer la atención del jugador.

---

## Historia

En la **Gran Academia de Alquimia Astral**, los aprendices deben superar el legendario **Rito de la Memoria Fluida**.

Magnus debe demostrar sus habilidades preparando pociones y elixires mientras intenta mantener la concentración en un laboratorio lleno de elementos mágicos y distracciones.

Durante su aventura estará acompañado por **Mishi**, el misterioso gato del laboratorio.

---

## Gameplay

* **Género:** Puzle / Estimulación cognitiva
* **Formato:** 2D
* **Vista:** Pantalla fija
* **Mecánica principal:** Memorización y secuenciación
* **Interacción:** Mouse, pantalla táctil y teclado
* **Motor:** Unity

### Mecánica principal

1. El jugador observa una receta durante unos segundos.
2. La receta desaparece.
3. Debe recordar los ingredientes.
4. Selecciona o arrastra los ingredientes hacia el caldero.
5. El juego comprueba si la secuencia es correcta.
6. La dificultad aumenta progresivamente mediante nuevas recetas y distractores.

La mecánica utiliza una **doble codificación visual mediante formas y colores** para facilitar la identificación de los ingredientes.

---

## Ingredientes

Los ingredientes utilizan diferentes formas geométricas y colores para facilitar su identificación:

* 🔴 **Círculo Rojo** — Esencia de Fuego
* 🔷 **Triángulo Azul** — Gota Mística
* ⭐ **Estrella Amarilla** — Polvo Astral
* 🟩 **Cuadrado Verde** — Hierba Esmeralda
* ⬢ **Pentágono Morado** — Cristal Arcano
* 🌙 **Luna Naranja** — Lágrima Solar

---

## Personajes

### Magnus

Joven aprendiz de alquimia y protagonista del juego. Es curioso, creativo, perseverante y busca demostrar sus habilidades dentro de la Academia de Alquimia Astral.

### Mishi

Gato mágico que acompaña al jugador y forma parte de las distracciones presentes en el laboratorio.

---

## Estilo visual

El proyecto busca una estética de **fantasía acogedora**, utilizando un estilo **Cartoon 2D** con:

* Colores vivos.
* Formas claras y reconocibles.
* Fondos cálidos.
* Elementos mágicos.
* Frascos y objetos de alquimia.
* Partículas y efectos visuales.

La intención es crear un ambiente agradable que transmita concentración, descubrimiento y satisfacción.

---

## Estructura del proyecto

Los recursos del juego se organizan dentro de la carpeta `Assets/` de Unity:

```text
Assets/
│
├── Sprites/
│   ├── UI/
│   │   └── # Botones, marcos y elementos de interfaz
│   │
│   ├── Ingredientes/
│   │   └── # Formas geométricas y colores
│   │
│   ├── Escenario/
│   │   └── # Laboratorio, caldero y estanterías
│   │
│   └── Personajes/
│       └── # Magnus y Mishi
│
├── Audio/
│   ├── SFX/
│   │   └── # Interacciones, aciertos, errores y efectos
│   │
│   └── BGM/
│       └── # Música ambiental
│
└── Fonts/
    └── # Tipografías del proyecto
```

La organización de los recursos busca mantener separados los elementos gráficos, personajes, interfaz, audio y tipografías para facilitar el desarrollo del proyecto.

---

## Herramientas

* **Unity** — Desarrollo del videojuego.
* **C#** — Programación.
* **Git / GitHub** — Control de versiones y gestión del repositorio.
* **Herramientas de diseño gráfico** — Creación y edición de recursos visuales.
* **Herramientas de audio** — Edición y preparación de sonidos y música.

---

## Estado del proyecto

**En desarrollo**

Actualmente el proyecto se encuentra en etapa de desarrollo del prototipo, trabajando en la construcción de los recursos visuales, organización de `Assets/`, mecánicas principales y estructura del videojuego.