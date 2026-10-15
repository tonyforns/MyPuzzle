# MyPuzzle

Juego de rompecabezas en Unity. El jugador elige una imagen y el sistema la convierte en piezas con encastres, dispersas sobre una mesa. Armar el puzzle se siente como hacerlo con cartón real: arrastrar, juntar grupos y encajar cada pieza en su lugar.

## Estado del proyecto

El proyecto de Unity 6000.3.13f1 ya está creado con la plantilla 2D (URP). Un doble clic saca solo esa pieza del bloque. El resto sigue unido.

## Cómo se juega

1. El jugador elige una imagen desde el disco.
2. Elige la dificultad: cantidad de filas y columnas, o un preset de piezas.
3. El sistema recorta la imagen en piezas con pestañas y huecos, y las reparte sobre la mesa.
4. El jugador arrastra las piezas. Cuando dos piezas vecinas quedan cerca y en la orientación correcta, se encajan y pasan a moverse juntas.
5. El puzzle termina cuando todas las piezas forman un solo grupo.

## Documentación

| Documento | Contenido |
| --- | --- |
| [Diseño de juego](docs/diseno-de-juego.md) | Visión, loop, piezas, mesa, controles y alcance de la primera versión |
| [Arquitectura](docs/arquitectura.md) | Módulos de Unity, generación del corte, grupos, encaje y carpetas |
| [Plan de implementación](docs/plan-de-implementacion.md) | Mecánicas, en orden, y la base que las sostiene |
| [Plan de desarrollo](docs/plan-de-desarrollo.md) | Tareas, archivos y cierre de cada fase para implementar |

## Stack previsto

- Unity (2D, cámara cenital sobre la mesa)
- C# con componentes (`MonoBehaviour`) para la interacción y `ScriptableObject` para la configuración de dificultad
- Input System de Unity para mouse y tacto
