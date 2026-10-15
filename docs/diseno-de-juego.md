# Diseño de juego

## Visión

MyPuzzle es una mesa digital donde se arma un rompecabezas a partir de cualquier imagen. El jugador no dibuja las piezas ni define los encastres: aporta la foto y el sistema genera el puzzle completo, con el mismo aspecto y la misma lógica que un rompecabezas de cartón.

La cámara mira la mesa desde arriba. Las piezas tienen grosor, sombra suave y borde, y se pueden mover con el puntero. Lo que ya está encajado viaja junto, como un bloque de cartón.

## Fantasía del jugador

> Pongo una foto mía sobre la mesa y el juego la parte en piezas de verdad. Las revuelvo, las voy juntando y, cuando dos encajan, se quedan unidas.

## Loop de una partida

1. **Elegir imagen.** Archivo local (PNG o JPG). El juego muestra una vista previa y rechaza archivos que no pueda leer.
2. **Elegir dificultad.** Presets fijos (por ejemplo 4×3, 6×4, 8×6, 10×8) o filas y columnas libres dentro de un máximo.
3. **Generar.** El sistema calcula la grilla, asigna encastres y recorta la textura. Las piezas aparecen boca arriba, mezcladas en la zona de piezas, sin solaparse de forma que queden ilegibles.
4. **Armar.** El jugador arrastra piezas sueltas o grupos ya unidos hacia el área de armado.
5. **Encajar.** Si una pieza (o un grupo) queda a poca distancia de su vecina correcta y con la misma rotación, se sueldan. El jugador siente un pequeño ajuste de posición, no un teletransporte brusco desde lejos.
6. **Completar.** Cuando queda un único grupo que cubre toda la grilla, la partida termina. La imagen completa queda centrada en la mesa y se ofrece volver a mezclar o elegir otra foto.

## La mesa

La escena tiene dos zonas sobre el mismo plano:

- **Área de armado.** Rectángulo del tamaño final del puzzle, con un marco tenue o la silueta vacía. Sirve de referencia de escala y de lugar donde apoyar el conjunto.
- **Zona de piezas.** Alrededor del marco, o a un lado si la relación de aspecto de la imagen es muy ancha. Ahí caen las piezas al empezar.

La imagen de referencia es opcional y atenuada dentro del marco. Se puede ocultar para jugar a ciegas, como con la caja del puzzle dada vuelta.

No hay gravedad que haga caer las piezas fuera de la mesa. El arrastre las mantiene en el plano. Un límite invisible evita que se pierdan fuera de la cámara.

## Las piezas

Cada pieza es un trozo de la imagen con forma de rompecabezas clásico:

- Las piezas de **esquina** tienen dos lados rectos.
- Las piezas de **borde** tienen un lado recto.
- Las piezas **interiores** tienen los cuatro lados con encastre.
- Cada lado interno de la grilla es una **pestaña** en una pieza y un **hueco** en la vecina. El generador elige el sentido al azar, con la única regla de que ambos lados coincidan.
- El contorno usa la curva típica de cuello y cabeza, para que el encaje se lea a simple vista y no sea un cuadrado con un semicírculo.

Todas las piezas de una partida comparten el mismo tamaño de celda. La imagen se ajusta al marco conservando su relación de aspecto: si sobra espacio en la mesa, queda margen, no se estira la foto.

Cada pieza muestra:

- La porción de imagen que le corresponde, incluida la parte que entra en las pestañas.
- Un canto de cartón (un grosor corto y un color de borde) para separarlas cuando están apiladas visualmente.
- Una sombra suave que indica cuál está por encima mientras se arrastra.

## Dificultad

La dificultad es el tamaño de la grilla. Más celdas implican piezas más chicas y más encastres parecidos.

| Preset | Grilla | Piezas | Uso |
| --- | --- | --- | --- |
| Fácil | 4×3 | 12 | Probar el encaje y los grupos |
| Medio | 6×4 | 24 | Partida corta |
| Difícil | 8×6 | 48 | Partida larga |
| Experto | 10×8 | 80 | Límite recomendado de la primera versión |

La primera versión no rota las piezas: salen ya orientadas, como muchos puzzles de mesa que se arman mirando la imagen. La rotación en pasos de 90° queda prevista en el modelo de pieza para una versión posterior, porque cambia la regla de encaje (hay que coincidir también el giro).

## Controles

| Acción | Mouse | Tacto |
| --- | --- | --- |
| Tomar una pieza o un grupo | Clic sostenido | Dedo sostenido |
| Mover | Arrastrar | Arrastrar |
| Soltar | Soltar el botón | Levantar el dedo |
| Traer al frente | Al tomar, la pieza o el grupo queda por encima del resto | Igual |
| Ver referencia | Botón en la interfaz | Igual |
| Nueva imagen / nueva mezcla | Botones en la interfaz | Igual |

Al tomar un grupo, se mueve entero. No se puede separar una pieza ya encajada en la primera versión.

## Encaje

Dos piezas se unen cuando se cumplen las tres condiciones a la vez:

1. Comparten un lado en la grilla (son vecinas reales, no solo parecidas).
2. La distancia entre sus anclas es menor que un umbral corto, del orden de una fracción del tamaño de celda.
3. La orientación coincide. En la primera versión esto es automático, porque no hay rotación.

Al unirse:

- Pasan a pertenecer al mismo grupo.
- Se alinean a la posición exacta de la grilla relativa del grupo.
- El grupo se puede seguir arrastrando como una sola pieza.
- Un grupo también puede encajar con otro grupo si alguna pareja de bordes cumple las mismas reglas. Así se arman bloques grandes y luego se juntan entre sí.

El encaje es definitivo en la primera versión. No hay “desarmar”.

## Fin de partida

La partida está completa cuando el grupo único contiene todas las celdas. En ese momento:

- Se centra el puzzle en el marco.
- Se desactiva el arrastre.
- Se muestra la imagen completa sin los cantos de las piezas, con una transición corta.
- Quedan disponibles “Mezclar de nuevo” (misma imagen y misma grilla, encastres nuevos) y “Otra imagen”.

## Fuera de la primera versión

Estas ideas encajan con la fantasía de la mesa, pero no forman parte del primer entregable:

- Rotar piezas.
- Varias imágenes o un álbum guardado.
- Guardar la partida a medias.
- Multijugador en la misma mesa.
- Piezas boca abajo.
- Animación de barajado física (las piezas pueden aparecer ya separadas).
- Recorte de formas que no sean la grilla rectangular clásica.
- Compras, cuentas o clasificación en línea.
