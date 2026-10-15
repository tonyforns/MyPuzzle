# Plan de implementación

Cada fase entrega una mecánica que se puede probar sola y deja la anterior intacta. El orden importa: primero el modelo de la partida, después la vista, y al final la interfaz. La escena no decide reglas. Solo muestra el estado y le manda órdenes.

## Base escalable

Estas decisiones se toman en la fase 0 y no se renegocian en las fases siguientes.

**El estado es datos, no objetos de la escena.** Una partida es una grilla, un conjunto de grupos y la pose de cada grupo. Los `MonoBehaviour` copian ese estado a transforms y meshes. Borrar las piezas de la escena y volver a crearlas desde el estado tiene que reconstruir la misma mesa. Eso deja abierto guardar la partida, deshacer, repetir una semilla y probar las reglas sin abrir el Editor.

**La identidad es la celda.** Una pieza es `(columna, fila)`. Un lado es esa celda más una dirección (norte, este, sur, oeste), guardado una sola vez. El encaje pregunta “¿esta celda es vecina de esta otra?”, no “¿estos dos meshes se parecen?”. Cambiar la curva de la pestaña no cambia la regla de unión.

**La pose admite giro desde el primer día.** Cada grupo guarda posición y cuartos de giro (0, 1, 2 o 3). En la primera versión el giro queda en 0 y nadie lo modifica. Cuando se agregue la rotación, se suma una orden y una comparación en el encaje. No hace falta un modelo nuevo de grupos.

**Una sola clase responde “¿quién es vecino de quién?”.** `GridTopology` resuelve vecino, lado opuesto y si un lado es borde. El generador, el encaje y la vista la usan. Hoy la grilla es rectangular. Otro recorte, más adelante, reemplaza esa clase y el resto sigue igual.

**Generar es una función de la semilla.** Misma semilla, mismas columnas, mismas filas: mismos encastres y mismas posiciones iniciales. Las pruebas repiten un caso concreto en lugar de perseguir un azar distinto en cada Play.

**Tres assemblies.**

| Assembly | Puede usar | Contiene |
| --- | --- | --- |
| `MyPuzzle.Core` | C# puro, sin `UnityEngine` | Grilla, generación, estado, dispersión, encaje |
| `MyPuzzle.View` | Unity y Core | Meshes, arrastre, cámara, mesa, HUD |
| `MyPuzzle.Tests` | Core | Pruebas de las reglas, sin escena |

## Fases

### 0. Proyecto y contrato de datos

Crear el proyecto Unity 2D, la escena vacía `Table` y los tres assemblies. Definir los tipos que el resto va a compartir:

- `CellId`, `Direction`, `EdgeKind` (`Edge`, `Tab`, `Blank`)
- `PuzzleDefinition`: columnas, filas, tamaño de celda, semilla, un registro por lado interno
- `GroupState`: piezas miembro, pose (posición y cuartos de giro), ancla
- `PuzzleState`: definición, grupos, fase (`Armando` o `Completo`)
- `GridTopology`
- `PuzzleConfig`: columnas, filas, tamaño de celda, umbral de encaje y márgenes. Vive en Core, sin Unity
- `PuzzleSettings` como `ScriptableObject` en View: guarda un `PuzzleConfig` por preset (4×3, 6×4, 8×6 y 10×8)

Listo cuando un preset se lee desde un asset y un estado vacío de 4×3 se construye en una prueba, con 12 grupos de una pieza y giro 0.

### 1. Generar la grilla

`PuzzleGenerator.Create(columnas, filas, semilla)` devuelve una `PuzzleDefinition`.

Reglas que la prueba debe fijar:

- Cada lado interno tiene un solo dueño y un solo encastre.
- La vecina ve el complemento: pestaña contra hueco.
- El marco es recto y no tiene encastre.
- Dos semillas distintas pueden diferir. La misma semilla no difiere.

En esta fase el contorno de cada celda es el rectángulo de la celda. La curva de pestaña entra en la fase 7, sobre el mismo registro de lados. El encaje y el arrastre no esperan a la curva.

Listo cuando una grilla 4×3 y una 10×8 pasan las reglas de arriba sin crear GameObjects.

### 2. Ver la imagen reconstruida

`PieceMeshBuilder` toma el contorno de una celda y arma un mesh 2D con UVs sobre la textura completa. `PuzzleSession` instancia una pieza por celda y las coloca en la solución: la foto se ve entera, partida por las juntas.

Textura de prueba incluida en el proyecto, de relación de aspecto conocida. Todavía no hay archivo elegido por el jugador ni piezas sueltas.

Listo cuando, en Play, la imagen de prueba se reconoce completa, sin huecos entre celdas ni estiramiento.

### 3. Grupos como única forma de mover

Aunque cada pieza esté sola, ya pertenece a un grupo. La posición de una pieza en el mundo sale de la pose del grupo más su offset de grilla. No se calcula al revés, leyendo transforms sueltos.

`PuzzleSession` expone una orden `MoverGrupo(id, destino)`. La vista aplica el resultado.

Listo cuando un botón temporal de la escena desplaza un grupo de una pieza y, en una prueba, fusionar dos grupos a mano deja un solo grupo cuya pose coloca ambas celdas en su offset correcto.

La fusión de prueba no es el encaje del jugador. Solo demuestra que el modelo de bloque ya sirve.

### 4. Arrastrar

`DragController` traduce mouse y tacto a tres órdenes: tomar el grupo bajo el punto, moverlo, soltarlo. Al tomar, ese grupo pasa al frente. El puntero se proyecta sobre el plano de la mesa. Un límite mantiene el grupo dentro de la cámara.

El controlador no une piezas ni conoce filas y columnas.

Listo cuando, en Play, se puede tomar cualquier pieza de la solución, moverla y soltarla, y el estado del grupo coincide con lo que se ve.

### 5. Repartir en la mesa

`TableLayout` es una función pura: definición, semilla, rectángulo del marco y rectángulo de la zona de piezas → posición inicial de cada grupo.

Respeta separación mínima y margen al marco. Si no entra, reduce la separación y reintenta. Si aun así no entra, aplica un desplazamiento chico y sigue. Las 80 piezas del preset experto tienen que aparecer dentro de la vista.

La sesión arranca llamando a este reparto en lugar de colocar la solución. Un atajo de desarrollo (`Solved` en `PuzzleSettings`) salta el reparto para poder probar el encaje con las piezas ya en su sitio.

Listo cuando Play muestra las piezas sueltas, reconocibles, y la misma semilla las deja en los mismos lugares.

### 6. Encajar y terminar

`SnapSystem` es una función pura. Recibe el estado y el grupo recién soltado, y devuelve el estado siguiente.

Pasos:

1. Recorrer los lados del grupo que todavía no tienen a su vecina adentro.
2. Buscar esa vecina en otro grupo.
3. Comparar la distancia entre la pose real y la pose que tendrían si ya estuvieran unidas. El umbral es una fracción del tamaño de celda.
4. Comparar los cuartos de giro. Hoy ambos valen 0, así que la comparación ya queda escrita.
5. Fusionar y alinear el grupo soltado. Repetir por si un solo gesto cierra más de un lado, incluida una esquina.
6. Si queda un solo grupo con todas las celdas, pasar a `Completo` y rechazar nuevas órdenes de movimiento.

El encaje es definitivo.

Listo cuando, con el atajo de solución, separar dos vecinas y acercarlas las vuelve a unir; cuando dos bloques se unen entre sí; y cuando completar la grilla deja de aceptar arrastre. Las pruebas cubren “vecina lejos no une”, “vecina cerca sí une” y “pieza parecida que no es vecina no une”.

### 7. Que se vea como cartón

Recién acá el contorno deja de ser un rectángulo.

- Curva única de cuello y cabeza, escalada a la celda. `Tab` la suma hacia afuera y `Blank` la resta.
- Canto de cartón y sombra suave, ambos solo visuales. El raycast del arrastre pega en la cara.
- Marco del área de armado.
- Referencia atenuada de la imagen dentro del marco, con un interruptor.

La prueba de la fase 1 sigue en verde: la curva no cambia dueños ni encastres. Se agrega una comprobación de que el contorno cierra y de que la pestaña de un lado coincide en espejo con el hueco del otro.

Listo cuando una partida de 4×3 se lee como rompecabezas y el encaje de la fase 6 sigue funcionando igual.

### 8. Loop de partida

`ImageImportService` abre un PNG o JPG local, valida que se pueda leer y ajusta el rectángulo al marco sin deformar. `PuzzleHud` ofrece la vista previa, los presets, mezclar de nuevo (nueva semilla, misma imagen y misma grilla) y elegir otra imagen.

Al completar, el puzzle se centra en el marco y una transición corta muestra la foto continua. Siguen disponibles mezclar y cambiar la imagen.

Listo cuando una foto elegida en el disco recorre el loop entero del diseño: generar, mezclar, arrastrar, encajar, terminar y volver a empezar.

## Qué queda preparado y sin construir

| Capacidad | Dónde ya tiene lugar | Cuándo se construye |
| --- | --- | --- |
| Rotar de a 90° | `cuartos de giro` en la pose y en la comparación del encaje | Después del loop, como una orden nueva del arrastre |
| Guardar y cargar | `PuzzleDefinition` + `PuzzleState` serializables | Cuando el loop de una sesión esté cerrado |
| Desarmar una unión | La fusión es una operación del estado | Si más adelante se permite, se agrega la operación inversa |
| Otro tipo de recorte | `GridTopology` concentrada en una clase | Cuando exista un segundo recorte real |
| Varias fotos, álbum, red | Fuera de Core | No entra en este plan |

## Orden de trabajo

```
0 Contrato de datos
    → 1 Generador rectangular
        → 2 Imagen reconstruida
            → 3 Grupos
                → 4 Arrastre
                    → 5 Reparto
                        → 6 Encaje y fin
                            → 7 Curva, canto, marco, referencia
                                → 8 Elegir imagen y loop
```

La fase 2 es el primer momento en Unity Play. Las fases 0, 1, 3 (la parte de fusión) y 6 tienen pruebas en `MyPuzzle.Tests` que no necesitan la mesa. Una fase no arranca hasta que la anterior cumple su condición de lista.
