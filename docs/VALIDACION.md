# Validación de GolBet

Validación realizada el 29 de septiembre de 2026 en el PC de desarrollo, con .NET SDK 8.0.425 y SQL Server LocalDB.

## Comprobaciones automáticas

La suite incluye 27 casos, sin pruebas omitidas. Usa el servidor MVC real mediante `WebApplicationFactory` y una base SQL independiente para cada ejecución de la suite.

Se comprueban:

- Apertura de inicio, cartelera, equipos y formularios.
- Migraciones aplicadas, datos iniciales e idempotencia del seeder.
- Configuración de AutoMapper y datos del detalle.
- Filtros combinados y respuestas 400/404 para solicitudes inválidas.
- Creación, edición y desactivación lógica de equipos y partidos.
- Fechas UTC/Colombia y conservación de la auditoría.
- Rechazo de nombres duplicados, equipos inactivos o inexistentes, fechas pasadas y cuotas inválidas.
- Restricciones SQL sobre nombres y claves foráneas.
- Aceptación de `2,50` y `2.50`, guardando ambos como 2.50.
- Persistencia de errores y opciones de equipos al devolver un formulario inválido.
- Redirección después de guardar, mensajes de éxito y tokens antiforgery.
- Rechazo de intentos de editar un identificador distinto al de la URL.
- Edición de equipos que conservan un escudo local del seeder.

Para repetirlas, detén la app y ejecuta `Validar-GolBet.cmd`. Se genera `artifacts/test-results/golbet.trx`. El archivo no se versiona porque corresponde a cada ejecución local.

## Ejecución y revisión visual

Se verificó la compilación con el SDK de .NET 8 y también con MSBuild de Visual Studio 18 Insiders. El lanzador `Iniciar-GolBet.cmd` inicia el sitio en `http://localhost:5229`, con persistencia en `GolBetDB_DisenoSoft`.

Se revisaron la cartelera, los equipos y los formularios en el navegador. La validación del cliente muestra los campos obligatorios y rechaza cuotas de 1,00. Se revisó el formulario en un ancho de 390 píxeles. La comprobación final del aspecto usa la captura del profesor como referencia.

## Advertencia que sigue visible

NuGet avisa sobre **AutoMapper 13.0.1** con el código `NU1903`. Se conservó esa versión porque es la indicada expresamente en el módulo 4. El aviso corresponde a recursión sin límite en grafos anidados; puede consultarse en [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x).

Todos los mapas del proyecto tienen `MaxDepth(4)` y los DTOs de entrada son planos. Esto limita la exposición en los mapas actuales, pero no convierte el paquete en una versión corregida ni elimina la advertencia. El aviso no se ha silenciado. Una actualización de AutoMapper debe revisar también las condiciones de licencia y los cambios de API de la versión elegida.

## Lo que no cubre esta entrega

No hay pruebas de autenticación, saldo o resolución de apuestas porque esas funciones no se implementan en los módulos 1–6. Tampoco se ha hecho un despliegue público. La validación local no equivale a una revisión para producción.
