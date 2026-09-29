# GolBet

Aplicación del curso de Desarrollo de Software, con los módulos 1 al 6. Permite consultar la cartelera de fútbol, filtrar partidos y crear, editar o desactivar equipos y encuentros.

El frontend sigue las vistas de los documentos y la captura del profesor: Bootstrap 5, barra verde, fondo blanco, escudos cuadrados y tablas sencillas. Los escudos son imágenes de ejemplo guardadas en el proyecto; no necesitan internet.

## Abrir la aplicación en este PC

Haz doble clic en **Iniciar-GolBet.cmd**. El programa compila la aplicación, prepara la base de datos si hace falta y abre [http://localhost:5229](http://localhost:5229).

Para cerrarla, ejecuta **Detener-GolBet.cmd**. Cerrar la pestaña del navegador no detiene el servidor.

La primera ejecución crea `GolBetDB_DisenoSoft` en SQL Server LocalDB y agrega ocho equipos y seis partidos: cuatro programados, uno en juego y uno finalizado con marcador 2–1. Las siguientes ejecuciones conservan los datos que hayas creado o editado.

## Trabajar desde Visual Studio

1. Ejecuta **Detener-GolBet.cmd** si la aplicación ya está abierta desde el lanzador, para liberar el puerto.
2. Haz doble clic en **Abrir-VisualStudio.cmd**. Abre `GolBet.sln` con el entorno de .NET preparado.
3. En el Explorador de soluciones, selecciona **GolBet.Web** como proyecto de inicio.
4. Selecciona el perfil **GolBet local** y presiona **F5** o **Ctrl+F5**.
5. Si el navegador no se abre solo, entra a [http://localhost:5229](http://localhost:5229).

También puedes abrir `GolBet.sln` directamente. Se conserva `GolBet.slnx` para versiones recientes de Visual Studio. El perfil `GolBet local` usa el SDK que está en `.tools/dotnet`; los perfiles `http` y `https` sirven cuando ASP.NET Core 8 está instalado en Windows. El perfil HTTPS requiere un certificado de desarrollo de confianza.

En este PC se encontró Visual Studio 18 Insiders, SQL Server LocalDB y un SDK global de .NET 10. Para ejecutar el proyecto con la versión del curso, se preparó un SDK de .NET 8 dentro de `.tools`. Esa carpeta no se sube a Git. La solución no utiliza las cargas opcionales de Android o MAUI.

## Si clonas el repositorio en otro equipo

Necesitas Windows, Visual Studio con la carga **ASP.NET y desarrollo web**, SQL Server LocalDB y acceso a internet para descargar las dependencias la primera vez.

Ejecuta **Preparar-Entorno.cmd** y luego **Iniciar-GolBet.cmd**. El preparador descarga el SDK oficial de .NET 8 dentro del repositorio y restaura NuGet. No cambia el SDK global del equipo.

Si usas otra instancia de SQL Server, cambia `ConnectionStrings:DefaultConnection` en `GolBet.Web/appsettings.json`. La cadena actual usa autenticación de Windows, sin contraseñas. No subas credenciales a Git.

## Qué contiene cada proyecto

| Proyecto | Responsabilidad |
| --- | --- |
| `GolBet.Entities` | Equipos, partidos, apuestas, enumeraciones y campos de auditoría. |
| `GolBet.Repositories` | Entity Framework, migraciones, consultas y carga inicial de datos. |
| `GolBet.Services` | DTOs, AutoMapper y reglas de negocio. |
| `GolBet.Web` | Controladores, vistas Razor y archivos del frontend. |
| `GolBet.Tests` | Pruebas de integración sobre SQL Server. |

Una petición sigue este recorrido: **vista → controlador → servicio → repositorio → SQL Server**. Al consultar, los servicios devuelven DTOs; las vistas no reciben entidades de Entity Framework.

## Probar los módulos

Desde la pantalla de Equipos puedes crear un club, editar su ciudad y desactivarlo si no tiene partidos asociados. En Partidos puedes combinar los filtros de estado y equipo, entrar al detalle y programar un encuentro nuevo.

Los formularios aceptan cuotas como `2,50` o `2.50`, sin separadores de miles. Las fechas se capturan y muestran en hora de Colombia; en la base de datos se guardan en UTC.

Para ejecutar las pruebas automáticas, detén primero la app y abre **Validar-GolBet.cmd**. Los resultados quedan en `artifacts/test-results/golbet.trx`. Las pruebas usan una base temporal llamada `GolBetTests_` seguida de un identificador; no borran ni alteran `GolBetDB_DisenoSoft`.

## Alcance de esta entrega

Se implementaron únicamente los módulos 1–6, según lo acordado. Registro, inicio de sesión, roles, apuestas de usuarios, saldo y resolución de apuestas pertenecen a módulos posteriores. La entidad `Bet` existe porque forma parte del modelo del módulo 2; el botón Apostar permanece deshabilitado.

La gestión todavía no tiene autenticación, como indica el módulo 6. Los lanzadores escuchan en `localhost` para trabajar y hacer la demostración en el propio PC.

Consulta [el detalle por módulo](docs/MODULOS.md) y [las comprobaciones realizadas](docs/VALIDACION.md). Ahí también se explica la advertencia de NuGet de AutoMapper 13.0.1, que se conservó para seguir la versión indicada por el curso.
