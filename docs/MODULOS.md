# Desarrollo de los módulos

La referencia de esta entrega es la carpeta **Módulos GolBet APP**: guía introductoria, modelo relacional y documentos de los módulos 1 a 6. La guía menciona otras funciones, pero no forman parte del alcance acordado.

## Módulo 1 — Solución y presentación

Se mantienen los cuatro proyectos y la dirección de sus referencias. `GolBet.sln` permite abrirlos en Visual Studio; `GolBet.slnx` conserva el formato original del repositorio. La aplicación tiene página de bienvenida, navegación a Partidos y Equipos, y pie de página con el año actual.

El diseño sigue el material del profesor. Se añadieron lanzadores para iniciar, detener y validar la aplicación, preparar otro PC y abrir Visual Studio con el SDK correcto.

## Módulo 2 — Entidades y base de datos

`Team`, `Match` y `Bet` heredan de `AuditableEntity`. La migración original crea las tablas, las relaciones, el índice único del nombre del equipo y la precisión decimal de las cuotas. Las dos relaciones de un partido con equipos y la relación de apuestas con partidos restringen el borrado físico.

El contexto asigna `CreatedDate` al crear y `ModifiedDate` al actualizar. La fecha de creación no se sobrescribe en la base. La auditoría funciona tanto al guardar de forma síncrona como asíncrona.

## Módulo 3 — Repositorios y datos iniciales

`GenericRepository<T>` reúne la lectura, creación, actualización y desactivación. `MatchRepository` carga los equipos y apuestas, filtra la cartelera y consulta las relaciones necesarias para las validaciones.

El seeder aplica las migraciones y carga ocho equipos y seis partidos. Usa una transacción para no dejar solo una parte de los datos si falla el guardado. Si ya hay equipos, respeta los datos existentes y no vuelve a sembrar.

## Módulo 4 — Servicios y cartelera

Los servicios convierten las entidades a DTOs mediante AutoMapper. La cartelera muestra escudos, equipos, fecha, estado, cuotas y marcador cuando corresponde. El listado de equipos completa el reto de este módulo usando el repositorio genérico.

Los escudos de ejemplo son SVG locales con las mismas siglas y colores del material. Las fechas se muestran en español y en hora de Colombia.

## Módulo 5 — Detalle y filtros

Cada tarjeta permite abrir el detalle. Se muestran las tres cuotas con los nombres de los equipos y el total de apuestas asociadas. Los identificadores inexistentes o inactivos devuelven 404.

Se puede filtrar por estado, por equipo o por ambos. Los filtros quedan en la URL y se conservan al recargar. Una búsqueda sin resultados muestra un mensaje en lugar de una zona vacía.

## Módulo 6 — Equipos y partidos

Ambos módulos tienen creación, listado, edición y desactivación. Los formularios reutilizan parciales, validan en navegador y servidor, incluyen protección antiforgery y redirigen después de guardar. Los mensajes de éxito usan `TempData`.

Se completaron las acciones de partidos y la vista de edición que faltaban. También se corrigieron el manejo de decimales y la validación del identificador de la URL al editar.

## Decisiones donde el material deja opciones abiertas

- Desactivar conserva la fila y su auditoría. Un equipo con partidos asociados no puede desactivarse; así se protege su historial.
- Los partidos iniciados o finalizados se pueden consultar, pero no se editan ni se desactivan. Un partido con apuestas tampoco se desactiva.
- Los equipos seleccionados deben existir y estar activos. No basta con enviar un número en el formulario.
- El nombre del equipo se compara sin distinguir mayúsculas ni acentos, incluidos los equipos inactivos. SQL Server mantiene además su índice único.
- Se aceptan punto y coma decimal, con un máximo de dos decimales. No se aceptan separadores de miles.
- Los escudos nuevos deben usar HTTPS. Los escudos locales del seeder se pueden conservar al editar.

Los documentos proponen commits y tags al finalizar cada clase. No se inventaron commits históricos para simular ese recorrido: esta entrega reúne la finalización y validación de los seis módulos en un cambio real sobre el repositorio existente.
