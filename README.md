# Carrito de Compras - Backend

API REST desarrollada en .NET 10 / ASP.NET Core Web API para un sistema de carrito de compras, con autenticación JWT, gestión de productos, carrito y órdenes con control de stock y descuentos automáticos.

## Stack técnico

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core (proveedor PostgreSQL)
- PostgreSQL
- JWT (JSON Web Tokens) para autenticación
- BCrypt para hashing de contraseñas
- Swagger / OpenAPI para documentación de endpoints

## Arquitectura

El proyecto sigue una arquitectura en capas, separada en 3 proyectos dentro de la solución:

```
Back/
├── CarritoCompras.slnx
├── CarritoCompras.Api/              # Controllers, configuración, Program.cs
├── CarritoCompras.Domain/           # Entities, DTOs, interfaces, contratos
└── CarritoCompras.Infrastructure/   # DbContext, repositorios, servicios, migraciones
```

`Domain` no depende de ningún otro proyecto (contiene solo el modelo de negocio). `Infrastructure` implementa el acceso a datos y la lógica de negocio, dependiendo de `Domain`. `Api` expone todo por HTTP y depende de ambos.

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download) o superior
- [PostgreSQL](https://www.postgresql.org/download/) corriendo localmente (puerto por defecto 5432)
- (Opcional) [dotnet-ef tool](https://learn.microsoft.com/ef/core/cli/dotnet) si quieres generar migraciones manualmente:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Configuración

El proyecto usa `appsettings.Development.json` para las credenciales de conexión y JWT en entorno local (con datos de prueba, no sensibles). Verifica/ajusta este archivo en `CarritoCompras.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=carrito_compras;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "ClaveSuperSecretaParaFirmarTokens_CambiarEnProduccion_2026",
    "Issuer": "CarritoComprasApi",
    "Audience": "CarritoComprasClient",
    "ExpirationMinutes": 60
  }
}
```

Ajusta `Username` y `Password` según tu instalación local de PostgreSQL.

## Instalación y ejecución

```bash
# 1. Clonar el repositorio
git clone <url-del-repo>
cd Back

# 2. Restaurar dependencias
dotnet restore

# 3. Compilar (opcional, para verificar que todo esté bien)
dotnet build

# 4. Ejecutar la API
cd CarritoCompras.Api
dotnet run
```

La base de datos y las tablas se crean **automáticamente** al arrancar la aplicación (el proyecto aplica las migraciones pendientes y siembra datos de prueba en el primer arranque, sin necesidad de correr comandos adicionales).

La API quedará disponible en la URL que indique la consola (por ejemplo `http://localhost:5028`), y Swagger en:

```
http://localhost:5028/swagger
```

## Usuarios semilla

| Usuario | Contraseña | Rol |
|---|---|---|
| `Fernando Mendoza` | `Admin*123` | Admin |
| `Irving Zambrano` | `Cliente*123` | Customer |

## Productos semilla

| Código | Nombre | Categoría | Precio | Stock |
|---|---|---|---|---|
| P001 | Laptop Lenovo ThinkPad | Tecnología | $850.00 | 15 |
| P002 | Mouse Inalámbrico | Tecnología | $18.50 | 100 |
| P003 | Teclado Mecánico | Tecnología | $45.00 | 40 |
| P004 | Monitor 24 pulgadas | Tecnología | $120.00 | 25 |
| P005 | Silla Ergonómica | Oficina | $135.00 | 10 |

## Endpoints principales

| Método | Endpoint | Descripción | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Login, devuelve token JWT | No |
| GET | `/api/products` | Lista productos con búsqueda, filtros y paginación | Sí |
| GET | `/api/products/{id}` | Detalle de un producto | Sí |
| GET | `/api/cart` | Obtiene el carrito del usuario autenticado | Sí |
| POST | `/api/cart/items` | Agrega producto al carrito (valida stock) | Sí |
| PUT | `/api/cart/items/{productId}` | Actualiza cantidad de un item | Sí |
| DELETE | `/api/cart/items/{productId}` | Elimina un item del carrito | Sí |
| DELETE | `/api/cart` | Vacía el carrito completo | Sí |
| POST | `/api/orders` | Finaliza la compra, genera orden y descuenta stock | Sí |
| GET | `/api/orders` | Historial de compras del usuario | Sí |
| GET | `/api/orders/{id}` | Detalle de una compra específica | Sí |

Todos los endpoints protegidos requieren el header:

```
Authorization: Bearer {token}
```

## CORS

El backend permite peticiones desde `http://localhost:4200` (el frontend Angular en desarrollo). Si el frontend corre en otro puerto/origen, ajusta la política de CORS en `Program.cs`.

## Decisiones técnicas relevantes

- **Arquitectura en capas** (Api / Domain / Infrastructure) para separar responsabilidades: el dominio no conoce detalles de persistencia ni de HTTP.
- **Patrón Repository + Service**: los repositorios encapsulan el acceso a datos vía EF Core; los servicios contienen la lógica de negocio (validación de stock, cálculo de descuentos, etc.).
- **`decimal` para todos los valores monetarios**, nunca `float`/`double`, para evitar errores de redondeo.
- **`OrderItem` guarda el precio unitario al momento de la compra** (no una referencia al precio actual del producto), para que el historial no cambie si el precio del producto se actualiza después.
- **Transacciones de base de datos en el checkout**: la creación de la orden, el descuento de stock y el vaciado del carrito ocurren dentro de una única transacción (`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`), garantizando consistencia si algo falla a mitad de camino.
- **Validación de stock en dos momentos**: una validación rápida antes de abrir la transacción (para responder rápido con un mensaje claro), y la validación definitiva dentro de la transacción (por si el stock cambió entre que el usuario cargó la pantalla y confirmó la compra).
- **JWT con BCrypt** para autenticación stateless; el rol del usuario (`Admin`/`Customer`) viaja como claim dentro del token.
- **Descuento automático del 10%** cuando el subtotal del carrito u orden supera los $100, calculado en tiempo real (no se persiste como campo fijo del carrito, solo en la orden final).

## Notas

- El proyecto fue desarrollado con .NET 10 SDK; se fijaron versiones explícitas de algunos paquetes (`Microsoft.EntityFrameworkCore`, `Microsoft.OpenApi`) para resolver conflictos de resolución de dependencias transitivas propios de esta versión reciente del SDK.
- Las credenciales en `appsettings.Development.json` son de desarrollo/prueba únicamente. En un entorno productivo real, se sobreescribirían mediante variables de entorno.
